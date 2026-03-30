using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Reusable navigation bar for chart controls (PluginChartTime, PluginChartLog, etc.).
    /// </summary>
    /// <remarks>
    /// This control provides a standardized interface for navigating data points and adjusting view limits.
    ///
    /// Design contract:
    /// <list type="bullet">
    ///   <item>Directional buttons (First, PrevPage, Prev, Next, NextPage, Last).</item>
    ///   <item>Data toggle (ShowAll) and Axis limits (Decrease, Reset, Increase).</item>
    ///   <item>Navigation is disabled when ShowAllData is active.</item>
    ///   <item>Axis limits are constrained between <see cref="AxisLimitMinimum"/> (5) and <see cref="AxisLimitMaximum"/>.</item>
    /// </list>
    /// </remarks>
    public partial class PluginChartNavBar : UserControl
    {
        /// <summary>The absolute minimum number of data points to display to ensure readability.</summary>
        public const int AxisLimitMinimum = 5;

        private readonly PluginChartNavBarDataContext _ctx;

        #region CanGoNext

        /// <summary>
        /// Determines if forward navigation (Next/NextPage/Last) is available.
        /// Overridden to false if <see cref="ShowAllData"/> is enabled.
        /// </summary>
        public bool CanGoNext
        {
            get => (bool)GetValue(CanGoNextProperty);
            set => SetValue(CanGoNextProperty, value);
        }

        public static readonly DependencyProperty CanGoNextProperty = DependencyProperty.Register(
            nameof(CanGoNext),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(true, OnCanGoNextChanged)
        );

        private static void OnCanGoNextChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            ctrl._ctx.CanGoNext = !ctrl.ShowAllData && (bool)e.NewValue;
        }

        #endregion

        #region CanGoPrev

        /// <summary>
        /// Determines if backward navigation (Prev/PrevPage/First) is available.
        /// Overridden to false if <see cref="ShowAllData"/> is enabled.
        /// </summary>
        public bool CanGoPrev
        {
            get => (bool)GetValue(CanGoPrevProperty);
            set => SetValue(CanGoPrevProperty, value);
        }

        public static readonly DependencyProperty CanGoPrevProperty = DependencyProperty.Register(
            nameof(CanGoPrev),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(true, OnCanGoPrevChanged)
        );

        private static void OnCanGoPrevChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            ctrl._ctx.CanGoPrev = !ctrl.ShowAllData && (bool)e.NewValue;
        }

        #endregion

        #region ShowNavBar

        /// <summary>Gets or sets the visibility of the entire navigation bar.</summary>
        public bool ShowNavBar
        {
            get => (bool)GetValue(ShowNavBarProperty);
            set => SetValue(ShowNavBarProperty, value);
        }

        public static readonly DependencyProperty ShowNavBarProperty = DependencyProperty.Register(
            nameof(ShowNavBar),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(true, OnShowNavBarChanged)
        );

        private static void OnShowNavBarChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            ((PluginChartNavBar)d)._ctx.ShowNavBar = (bool)e.NewValue;
        }

        #endregion

        #region ShowAllData

        /// <summary>
        /// Toggles between displaying a limited window of data and the entire dataset.
        /// Disables specific navigation features when set to true.
        /// </summary>
        public bool ShowAllData
        {
            get => (bool)GetValue(ShowAllDataProperty);
            set => SetValue(ShowAllDataProperty, value);
        }

        public static readonly DependencyProperty ShowAllDataProperty = DependencyProperty.Register(
            nameof(ShowAllData),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(false, OnShowAllDataChanged)
        );

        private static void OnShowAllDataChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            bool active = (bool)e.NewValue;

            ctrl._ctx.CanNavigate = !active;
            ctrl._ctx.ShowAllData = active;
            ctrl._ctx.CanGoNext = !active && ctrl.CanGoNext;
            ctrl._ctx.CanGoPrev = !active && ctrl.CanGoPrev;

            bool allowDecrease =
                ctrl.AxisLimitMaximum <= 0 || ctrl.AxisLimitMaximum > AxisLimitMinimum;

            // Re-evaluate limit interaction states based on mode
            if (active)
            {
                ctrl._ctx.CanDecreaseAxisLimit = allowDecrease && ctrl.AxisLimit > AxisLimitMinimum;
                ctrl._ctx.CanIncreaseAxisLimit = CanIncrease(ctrl.AxisLimit, ctrl.AxisLimitMaximum);
                ctrl._ctx.CanResetAxisLimit = ctrl.AxisLimit != ctrl.AxisLimitDefault;
            }
            else
            {
                ctrl._ctx.CanDecreaseAxisLimit =
                    ctrl.AllowDecreaseAxisLimit && ctrl.AxisLimit > AxisLimitMinimum;
                ctrl._ctx.CanIncreaseAxisLimit = CanIncrease(ctrl.AxisLimit, ctrl.AxisLimitMaximum);
                ctrl._ctx.CanResetAxisLimit = ctrl.AxisLimit != ctrl.AxisLimitDefault;
            }

            ctrl._ctx.ShowAllToolTip = active
                ? ResourceProvider.GetString("LOCCommonNavShowAllActive")
                : ResourceProvider.GetString("LOCCommonNavShowAll");
        }

        #endregion

        #region NavLabel

        /// <summary>
        /// Secondary label or badge text shown on the navigation bar.
        /// </summary>
        public string NavLabel
        {
            get => (string)GetValue(NavLabelProperty);
            set => SetValue(NavLabelProperty, value);
        }

        public static readonly DependencyProperty NavLabelProperty = DependencyProperty.Register(
            nameof(NavLabel),
            typeof(string),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnNavLabelChanged)
        );

        private static void OnNavLabelChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            string lbl = e.NewValue as string ?? string.Empty;
            ctrl._ctx.NavLabel = lbl;
            ctrl._ctx.HasNavLabel = !string.IsNullOrEmpty(lbl);
        }

        #endregion

        #region PageSize

        /// <summary>
        /// Defines the step size for Page navigation. Buttons are hidden if value is 0 or less.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize),
            typeof(int),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(0, OnPageSizeChanged)
        );

        private static void OnPageSizeChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            int size = (int)e.NewValue;

            ctrl._ctx.PageSize = size;
            ctrl._ctx.HasPageButtons = size > 0;
            ctrl._ctx.PagePrevToolTip = BuildPageToolTip("LOCCommonNavPagePrev", size);
            ctrl._ctx.PageNextToolTip = BuildPageToolTip("LOCCommonNavPageNext", size);
        }

        private static string BuildPageToolTip(string locKey, int size)
        {
            string baseText = ResourceProvider.GetString(locKey);
            return size > 0 ? string.Format("{0} ({1})", baseText, size) : baseText;
        }

        #endregion

        #region AxisLimit

        /// <summary>
        /// The number of items to display on the X-axis.
        /// </summary>
        public int AxisLimit
        {
            get => (int)GetValue(AxisLimitProperty);
            set => SetValue(AxisLimitProperty, value);
        }

        public static readonly DependencyProperty AxisLimitProperty = DependencyProperty.Register(
            nameof(AxisLimit),
            typeof(int),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnAxisLimitChanged
            )
        );

        private static void OnAxisLimitChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            int limit = (int)e.NewValue;

            bool allowDecrease =
                ctrl.AxisLimitMaximum <= 0 || ctrl.AxisLimitMaximum > AxisLimitMinimum;

            ctrl._ctx.AxisLimit = limit;
            ctrl._ctx.CanDecreaseAxisLimit =
                !ctrl.ShowAllData && allowDecrease && limit > AxisLimitMinimum;
            ctrl._ctx.CanIncreaseAxisLimit =
                !ctrl.ShowAllData && CanIncrease(limit, ctrl.AxisLimitMaximum);
            ctrl._ctx.CanResetAxisLimit = !ctrl.ShowAllData && limit != ctrl.AxisLimitDefault;
            ctrl._ctx.AxisLimitDecreaseToolTip = BuildAxisLimitToolTip(
                "LOCCommonNavAxisLimitDecrease",
                limit
            );
            ctrl._ctx.AxisLimitIncreaseToolTip = BuildAxisLimitToolTip(
                "LOCCommonNavAxisLimitIncrease",
                limit
            );
        }

        private static string BuildAxisLimitToolTip(string locKey, int currentLimit)
        {
            string baseText = ResourceProvider.GetString(locKey);
            return currentLimit > 0 ? string.Format("{0} ({1})", baseText, currentLimit) : baseText;
        }

        #endregion

        #region AxisLimitMaximum

        /// <summary>
        /// Defines the upper boundary for <see cref="AxisLimit"/>.
        /// Set to 0 to disable the upper limit.
        /// </summary>
        public int AxisLimitMaximum
        {
            get => (int)GetValue(AxisLimitMaximumProperty);
            set => SetValue(AxisLimitMaximumProperty, value);
        }

        public static readonly DependencyProperty AxisLimitMaximumProperty =
            DependencyProperty.Register(
                nameof(AxisLimitMaximum),
                typeof(int),
                typeof(PluginChartNavBar),
                new FrameworkPropertyMetadata(0, OnAxisLimitMaximumChanged)
            );

        private static void OnAxisLimitMaximumChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            int max = (int)e.NewValue;

            bool allowDecrease = max <= 0 || max > AxisLimitMinimum;
            ctrl._ctx.CanDecreaseAxisLimit =
                !ctrl.ShowAllData && allowDecrease && ctrl.AxisLimit > AxisLimitMinimum;
            ctrl._ctx.CanIncreaseAxisLimit = !ctrl.ShowAllData && CanIncrease(ctrl.AxisLimit, max);
            ctrl._ctx.AxisLimitIncreaseToolTip = BuildAxisLimitToolTip(
                "LOCCommonNavAxisLimitIncrease",
                ctrl.AxisLimit
            );
        }

        private static bool CanIncrease(int current, int max)
        {
            return max <= 0 || current < max;
        }

        #endregion

        #region AxisLimitDefault

        /// <summary>
        /// The value restored when using the Reset functionality.
        /// </summary>
        public int AxisLimitDefault
        {
            get => (int)GetValue(AxisLimitDefaultProperty);
            set => SetValue(AxisLimitDefaultProperty, value);
        }

        public static readonly DependencyProperty AxisLimitDefaultProperty =
            DependencyProperty.Register(
                nameof(AxisLimitDefault),
                typeof(int),
                typeof(PluginChartNavBar),
                new FrameworkPropertyMetadata(0, OnAxisLimitDefaultChanged)
            );

        private static void OnAxisLimitDefaultChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            int newDefault = (int)e.NewValue;
            ctrl._ctx.CanResetAxisLimit = !ctrl.ShowAllData && ctrl.AxisLimit != newDefault;
            ctrl._ctx.AxisLimitResetToolTip = BuildAxisLimitToolTip(
                "LOCCommonNavAxisLimitReset",
                newDefault
            );
        }

        #endregion

        #region AllowDecreaseAxisLimit

        /// <summary>
        /// External gate to prevent axis reduction even if current limit is above minimum.
        /// </summary>
        public bool AllowDecreaseAxisLimit
        {
            get => (bool)GetValue(AllowDecreaseAxisLimitProperty);
            set => SetValue(AllowDecreaseAxisLimitProperty, value);
        }

        public static readonly DependencyProperty AllowDecreaseAxisLimitProperty =
            DependencyProperty.Register(
                nameof(AllowDecreaseAxisLimit),
                typeof(bool),
                typeof(PluginChartNavBar),
                new FrameworkPropertyMetadata(true, OnAllowDecreaseAxisLimitChanged)
            );

        private static void OnAllowDecreaseAxisLimitChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            var ctrl = (PluginChartNavBar)d;
            bool allowed = (bool)e.NewValue;
            ctrl._ctx.CanDecreaseAxisLimit =
                !ctrl.ShowAllData && allowed && ctrl.AxisLimit > AxisLimitMinimum;
        }

        #endregion

        #region CustomToggle

        /// <summary>
        /// State of the optional custom toggle button rendered at the far right of the nav bar.
        /// When false (default), the button is hidden entirely.
        /// </summary>
        public bool CustomToggle
        {
            get => (bool)GetValue(CustomToggleProperty);
            set => SetValue(CustomToggleProperty, value);
        }

        public static readonly DependencyProperty CustomToggleProperty = DependencyProperty.Register(
            nameof(CustomToggle),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(false, OnCustomToggleChanged)
        );

        private static void OnCustomToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            bool active = (bool)e.NewValue;
            ctrl._ctx.CustomToggle = active;
            ctrl._ctx.CustomToggleToolTip = active
                ? (ctrl._ctx.CustomToggleActiveToolTip ?? ctrl._ctx.CustomToggleToolTip)
                : (ctrl._ctx.CustomToggleInactiveToolTip ?? ctrl._ctx.CustomToggleToolTip);
        }

        #endregion

        #region CustomToggleVisible

        /// <summary>
        /// Controls whether the custom toggle button is rendered.
        /// Set to true from the consumer to opt-in to the feature.
        /// </summary>
        public bool CustomToggleVisible
        {
            get => (bool)GetValue(CustomToggleVisibleProperty);
            set => SetValue(CustomToggleVisibleProperty, value);
        }

        public static readonly DependencyProperty CustomToggleVisibleProperty = DependencyProperty.Register(
            nameof(CustomToggleVisible),
            typeof(bool),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(false, OnCustomToggleVisibleChanged)
        );

        private static void OnCustomToggleVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PluginChartNavBar)d)._ctx.CustomToggleVisible = (bool)e.NewValue;
        }

        #endregion

        #region CustomToggleInactiveIcon / CustomToggleActiveIcon

        /// <summary>IcoFont glyph shown when the toggle is inactive.</summary>
        public string CustomToggleInactiveIcon
        {
            get => (string)GetValue(CustomToggleInactiveIconProperty);
            set => SetValue(CustomToggleInactiveIconProperty, value);
        }

        public static readonly DependencyProperty CustomToggleInactiveIconProperty = DependencyProperty.Register(
            nameof(CustomToggleInactiveIcon),
            typeof(string),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnCustomToggleIconChanged)
        );

        /// <summary>IcoFont glyph shown when the toggle is active.</summary>
        public string CustomToggleActiveIcon
        {
            get => (string)GetValue(CustomToggleActiveIconProperty);
            set => SetValue(CustomToggleActiveIconProperty, value);
        }

        public static readonly DependencyProperty CustomToggleActiveIconProperty = DependencyProperty.Register(
            nameof(CustomToggleActiveIcon),
            typeof(string),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnCustomToggleIconChanged)
        );

        private static void OnCustomToggleIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            // Update displayed icon based on current toggle state
            ctrl._ctx.CustomToggleIcon = ctrl.CustomToggle
                ? (ctrl.CustomToggleActiveIcon ?? ctrl.CustomToggleInactiveIcon)
                : ctrl.CustomToggleInactiveIcon;
        }

        #endregion

        #region CustomToggleInactiveToolTip / CustomToggleActiveToolTip

        /// <summary>Tooltip text when the toggle is inactive.</summary>
        public string CustomToggleInactiveToolTip
        {
            get => (string)GetValue(CustomToggleInactiveToolTipProperty);
            set => SetValue(CustomToggleInactiveToolTipProperty, value);
        }

        public static readonly DependencyProperty CustomToggleInactiveToolTipProperty = DependencyProperty.Register(
            nameof(CustomToggleInactiveToolTip),
            typeof(string),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnCustomToggleToolTipChanged)
        );

        /// <summary>Tooltip text when the toggle is active.</summary>
        public string CustomToggleActiveToolTip
        {
            get => (string)GetValue(CustomToggleActiveToolTipProperty);
            set => SetValue(CustomToggleActiveToolTipProperty, value);
        }

        public static readonly DependencyProperty CustomToggleActiveToolTipProperty = DependencyProperty.Register(
            nameof(CustomToggleActiveToolTip),
            typeof(string),
            typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnCustomToggleToolTipChanged)
        );

        private static void OnCustomToggleToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            ctrl._ctx.CustomToggleInactiveToolTip = ctrl.CustomToggleInactiveToolTip;
            ctrl._ctx.CustomToggleActiveToolTip = ctrl.CustomToggleActiveToolTip;
            ctrl._ctx.CustomToggleToolTip = ctrl.CustomToggle
                ? ctrl.CustomToggleActiveToolTip
                : ctrl.CustomToggleInactiveToolTip;
        }

        #endregion

        // Routed Events
        public static readonly RoutedEvent FirstClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(FirstClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent PagePrevClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(PagePrevClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent PrevClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(PrevClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent ShowAllToggledEvent = EventManager.RegisterRoutedEvent(
            nameof(ShowAllToggled),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent NextClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(NextClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent PageNextClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(PageNextClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent LastClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(LastClicked),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent AxisLimitDecreasedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(AxisLimitDecreased),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PluginChartNavBar)
            );
        public static readonly RoutedEvent AxisLimitResetEvent = EventManager.RegisterRoutedEvent(
            nameof(AxisLimitReset),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PluginChartNavBar)
        );
        public static readonly RoutedEvent AxisLimitIncreasedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(AxisLimitIncreased),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PluginChartNavBar)
            );

        public static readonly RoutedEvent CustomToggledEvent = 
            EventManager.RegisterRoutedEvent(
                nameof(CustomToggled),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PluginChartNavBar)
            );

        public event RoutedEventHandler FirstClicked
        {
            add => AddHandler(FirstClickedEvent, value);
            remove => RemoveHandler(FirstClickedEvent, value);
        }
        public event RoutedEventHandler PagePrevClicked
        {
            add => AddHandler(PagePrevClickedEvent, value);
            remove => RemoveHandler(PagePrevClickedEvent, value);
        }
        public event RoutedEventHandler PrevClicked
        {
            add => AddHandler(PrevClickedEvent, value);
            remove => RemoveHandler(PrevClickedEvent, value);
        }
        public event RoutedEventHandler ShowAllToggled
        {
            add => AddHandler(ShowAllToggledEvent, value);
            remove => RemoveHandler(ShowAllToggledEvent, value);
        }
        public event RoutedEventHandler NextClicked
        {
            add => AddHandler(NextClickedEvent, value);
            remove => RemoveHandler(NextClickedEvent, value);
        }
        public event RoutedEventHandler PageNextClicked
        {
            add => AddHandler(PageNextClickedEvent, value);
            remove => RemoveHandler(PageNextClickedEvent, value);
        }
        public event RoutedEventHandler LastClicked
        {
            add => AddHandler(LastClickedEvent, value);
            remove => RemoveHandler(LastClickedEvent, value);
        }
        public event RoutedEventHandler AxisLimitDecreased
        {
            add => AddHandler(AxisLimitDecreasedEvent, value);
            remove => RemoveHandler(AxisLimitDecreasedEvent, value);
        }
        public event RoutedEventHandler AxisLimitReset
        {
            add => AddHandler(AxisLimitResetEvent, value);
            remove => RemoveHandler(AxisLimitResetEvent, value);
        }
        public event RoutedEventHandler AxisLimitIncreased
        {
            add => AddHandler(AxisLimitIncreasedEvent, value);
            remove => RemoveHandler(AxisLimitIncreasedEvent, value);
        }

        public event RoutedEventHandler CustomToggled
        {
            add => AddHandler(CustomToggledEvent, value);
            remove => RemoveHandler(CustomToggledEvent, value);
        }

        public PluginChartNavBar()
        {
            _ctx = new PluginChartNavBarDataContext();

            _ctx.CmdFirst = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(FirstClickedEvent))
            );
            _ctx.CmdPagePrev = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(PagePrevClickedEvent))
            );
            _ctx.CmdPrev = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(PrevClickedEvent))
            );
            _ctx.CmdToggleShowAll = new RelayCommand(() =>
            {
                ShowAllData = !ShowAllData;
                RaiseEvent(new RoutedEventArgs(ShowAllToggledEvent));
            });
            _ctx.CmdNext = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(NextClickedEvent))
            );
            _ctx.CmdPageNext = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(PageNextClickedEvent))
            );
            _ctx.CmdLast = new RelayCommand(() =>
                RaiseEvent(new RoutedEventArgs(LastClickedEvent))
            );

            _ctx.CmdAxisLimitDecrease = new RelayCommand(() =>
            {
                int next = Math.Max(AxisLimitMinimum, AxisLimit - 1);
                if (next == AxisLimit)
                    return;
                AxisLimit = next;
                RaiseEvent(new RoutedEventArgs(AxisLimitDecreasedEvent));
            });

            _ctx.CmdAxisLimitReset = new RelayCommand(() =>
            {
                if (AxisLimit == AxisLimitDefault)
                    return;
                AxisLimit = AxisLimitDefault;
                RaiseEvent(new RoutedEventArgs(AxisLimitResetEvent));
            });

            _ctx.CmdAxisLimitIncrease = new RelayCommand(() =>
            {
                int next = AxisLimit + 1;
                if (AxisLimitMaximum > 0 && next > AxisLimitMaximum)
                    next = AxisLimitMaximum;
                if (next == AxisLimit)
                    return;
                AxisLimit = next;
                RaiseEvent(new RoutedEventArgs(AxisLimitIncreasedEvent));
            });

            _ctx.CmdCustomToggle = new RelayCommand(() =>
            {
                CustomToggle = !CustomToggle;
                // Sync icon & tooltip after state flip
                _ctx.CustomToggle = CustomToggle;
                _ctx.CustomToggleIcon = CustomToggle
                    ? (CustomToggleActiveIcon ?? CustomToggleInactiveIcon)
                    : CustomToggleInactiveIcon;
                _ctx.CustomToggleToolTip = CustomToggle
                    ? CustomToggleActiveToolTip
                    : CustomToggleInactiveToolTip;
                RaiseEvent(new RoutedEventArgs(CustomToggledEvent));
            });


            InitializeComponent();

            // Set DataContext to root container to avoid breaking external bindings on the UserControl itself.
            PART_Root.DataContext = _ctx;

            // Load initial localization
            _ctx.ShowAllToolTip = ResourceProvider.GetString("LOCCommonNavShowAll");
            _ctx.PagePrevToolTip = ResourceProvider.GetString("LOCCommonNavPagePrev");
            _ctx.PageNextToolTip = ResourceProvider.GetString("LOCCommonNavPageNext");
            _ctx.AxisLimitDecreaseToolTip = ResourceProvider.GetString(
                "LOCCommonNavAxisLimitDecrease"
            );
            _ctx.AxisLimitResetToolTip = ResourceProvider.GetString("LOCCommonNavAxisLimitReset");
            _ctx.AxisLimitIncreaseToolTip = ResourceProvider.GetString(
                "LOCCommonNavAxisLimitIncrease"
            );

            _ctx.CanIncreaseAxisLimit = true;
            _ctx.CanResetAxisLimit = true;
        }

        /// <summary>
        /// Formats an array of labels into a range string (e.g., "LabelA – LabelZ").
        /// </summary>
        public static string BuildRangeLabel(string[] labels)
        {
            if (labels == null || labels.Length == 0)
                return string.Empty;

            string first = labels[0];
            string last = labels[labels.Length - 1];

            if (string.Equals(first, last, StringComparison.CurrentCulture))
                return first;

            return string.Format(CultureInfo.CurrentCulture, "{0} \u2013 {1}", first, last);
        }

        // Programmatic API
        public void InvokeFirst() => RaiseEvent(new RoutedEventArgs(FirstClickedEvent));

        public void InvokePagePrev() => RaiseEvent(new RoutedEventArgs(PagePrevClickedEvent));

        public void InvokePrev() => RaiseEvent(new RoutedEventArgs(PrevClickedEvent));

        public void InvokeToggleShowAll()
        {
            ShowAllData = !ShowAllData;
            RaiseEvent(new RoutedEventArgs(ShowAllToggledEvent));
        }

        public void InvokeNext() => RaiseEvent(new RoutedEventArgs(NextClickedEvent));

        public void InvokePageNext() => RaiseEvent(new RoutedEventArgs(PageNextClickedEvent));

        public void InvokeLast() => RaiseEvent(new RoutedEventArgs(LastClickedEvent));

        public void InvokeAxisLimitDecrease() => _ctx.CmdAxisLimitDecrease.Execute(null);

        public void InvokeAxisLimitReset() => _ctx.CmdAxisLimitReset.Execute(null);

        public void InvokeAxisLimitIncrease() => _ctx.CmdAxisLimitIncrease.Execute(null);

        public void InvokeCustomToggle()
        {
            CustomToggle = !CustomToggle;
            RaiseEvent(new RoutedEventArgs(CustomToggledEvent));
        }
    }

    /// <summary>
    /// ViewModel containing the presentation logic and state for <see cref="PluginChartNavBar"/>.
    /// </summary>
    public class PluginChartNavBarDataContext : ObservableObject
    {
        private bool _showNavBar = true;
        public bool ShowNavBar
        {
            get => _showNavBar;
            set => SetValue(ref _showNavBar, value);
        }

        private bool _canNavigate = true;
        public bool CanNavigate
        {
            get => _canNavigate;
            set => SetValue(ref _canNavigate, value);
        }

        private bool _showAllData;
        public bool ShowAllData
        {
            get => _showAllData;
            set => SetValue(ref _showAllData, value);
        }

        private string _navLabel = string.Empty;
        public string NavLabel
        {
            get => _navLabel;
            set => SetValue(ref _navLabel, value);
        }

        private bool _hasNavLabel;
        public bool HasNavLabel
        {
            get => _hasNavLabel;
            set => SetValue(ref _hasNavLabel, value);
        }

        private int _pageSize;
        public int PageSize
        {
            get => _pageSize;
            set => SetValue(ref _pageSize, value);
        }

        private bool _hasPageButtons;
        public bool HasPageButtons
        {
            get => _hasPageButtons;
            set => SetValue(ref _hasPageButtons, value);
        }

        private int _axisLimit;
        public int AxisLimit
        {
            get => _axisLimit;
            set => SetValue(ref _axisLimit, value);
        }

        private bool _canDecreaseAxisLimit;
        public bool CanDecreaseAxisLimit
        {
            get => _canDecreaseAxisLimit;
            set => SetValue(ref _canDecreaseAxisLimit, value);
        }

        private bool _canResetAxisLimit;
        public bool CanResetAxisLimit
        {
            get => _canResetAxisLimit;
            set => SetValue(ref _canResetAxisLimit, value);
        }

        private bool _canIncreaseAxisLimit = true;
        public bool CanIncreaseAxisLimit
        {
            get => _canIncreaseAxisLimit;
            set => SetValue(ref _canIncreaseAxisLimit, value);
        }

        private bool _canGoNext = true;
        public bool CanGoNext
        {
            get => _canGoNext;
            set => SetValue(ref _canGoNext, value);
        }

        private bool _canGoPrev = true;
        public bool CanGoPrev
        {
            get => _canGoPrev;
            set => SetValue(ref _canGoPrev, value);
        }

        private string _showAllToolTip = string.Empty;
        public string ShowAllToolTip
        {
            get => _showAllToolTip;
            set => SetValue(ref _showAllToolTip, value);
        }

        private string _pagePrevToolTip = string.Empty;
        public string PagePrevToolTip
        {
            get => _pagePrevToolTip;
            set => SetValue(ref _pagePrevToolTip, value);
        }

        private string _pageNextToolTip = string.Empty;
        public string PageNextToolTip
        {
            get => _pageNextToolTip;
            set => SetValue(ref _pageNextToolTip, value);
        }

        private string _axisLimitDecreaseToolTip = string.Empty;
        public string AxisLimitDecreaseToolTip
        {
            get => _axisLimitDecreaseToolTip;
            set => SetValue(ref _axisLimitDecreaseToolTip, value);
        }

        private string _axisLimitResetToolTip = string.Empty;
        public string AxisLimitResetToolTip
        {
            get => _axisLimitResetToolTip;
            set => SetValue(ref _axisLimitResetToolTip, value);
        }

        private string _axisLimitIncreaseToolTip = string.Empty;
        public string AxisLimitIncreaseToolTip
        {
            get => _axisLimitIncreaseToolTip;
            set => SetValue(ref _axisLimitIncreaseToolTip, value);
        }

        private bool _customToggle;
        public bool CustomToggle
        {
            get => _customToggle;
            set => SetValue(ref _customToggle, value);
        }

        private bool _customToggleVisible;
        public bool CustomToggleVisible
        {
            get => _customToggleVisible;
            set => SetValue(ref _customToggleVisible, value);
        }

        private string _customToggleIcon = string.Empty;
        public string CustomToggleIcon
        {
            get => _customToggleIcon;
            set => SetValue(ref _customToggleIcon, value);
        }

        private string _customToggleToolTip = string.Empty;
        public string CustomToggleToolTip
        {
            get => _customToggleToolTip;
            set => SetValue(ref _customToggleToolTip, value);
        }

        // Commands
        public RelayCommand CmdFirst { get; set; }
        public RelayCommand CmdPagePrev { get; set; }
        public RelayCommand CmdPrev { get; set; }
        public RelayCommand CmdToggleShowAll { get; set; }
        public RelayCommand CmdNext { get; set; }
        public RelayCommand CmdPageNext { get; set; }
        public RelayCommand CmdLast { get; set; }
        public RelayCommand CmdAxisLimitDecrease { get; set; }
        public RelayCommand CmdAxisLimitReset { get; set; }
        public RelayCommand CmdAxisLimitIncrease { get; set; }

        // Kept as backing store for DP sync only — not bound in XAML
        internal string CustomToggleInactiveToolTip { get; set; } = string.Empty;
        internal string CustomToggleActiveToolTip { get; set; } = string.Empty;

        public RelayCommand CmdCustomToggle { get; set; }
    }
}
