using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Reusable navigation bar for chart controls (PluginChartTime, PluginChartLog, etc.).
    /// </summary>
    /// <remarks>
    /// Design contract:
    /// <list type="bullet">
    ///   <item>Nine buttons: First, PrevPage, Prev, ShowAll toggle, Next, NextPage, Last,
    ///         AxisLimitDecrease, AxisLimitIncrease.</item>
    ///   <item>First / PrevPage / Prev / Next / NextPage / Last / AxisLimitIncrease are disabled
    ///         when <see cref="PluginChartNavBarDataContext.CanNavigate"/> is <c>false</c>
    ///         (i.e. ShowAllData is active — navigation is meaningless over the full dataset).</item>
    ///   <item>AxisLimitDecrease is additionally disabled when <see cref="AxisLimit"/> is already
    ///         at <see cref="AxisLimitMinimum"/> (5).</item>
    ///   <item>The centre button toggles ShowAllData; its visual style reflects the active state.</item>
    ///   <item>An optional NavLabel badge is shown on the right (e.g. "Week 12").</item>
    ///   <item>PageSize controls how many steps PrevPage/NextPage jump.
    ///         When PageSize &lt;= 0, the PrevPage/NextPage buttons are hidden.</item>
    /// </list>
    ///
    /// Integration:
    /// <list type="bullet">
    ///   <item>Consumers subscribe to RoutedEvents for XAML-side wiring.</item>
    ///   <item>Consumers call <c>Invoke*()</c> methods for programmatic triggering.</item>
    ///   <item>The parent resolves <c>PageSize = AxisLimit ?? PluginSettings.ChartXxxCountAbscissa</c>
    ///         and binds it to this DP.</item>
    ///   <item>The parent binds <see cref="AxisLimit"/> two-way and reacts to
    ///         <see cref="AxisLimitDecreased"/> / <see cref="AxisLimitIncreased"/> to refresh the chart.</item>
    /// </list>
    /// </remarks>
    public partial class PluginChartNavBar : UserControl
    {
        // Enforced floor for the X-axis data limit — prevents charts from becoming unreadable.
        private const int AxisLimitMinimum = 5;

        // Internal DataContext kept separate from the control's own DataContext so that
        // external DP bindings on the parent still resolve against the parent's DataContext.
        private readonly PluginChartNavBarDataContext _ctx;

        // ────────────────────────────────────────────────────────────────────
        // Dependency Properties
        // ────────────────────────────────────────────────────────────────────

        #region ShowNavBar

        /// <summary>Controls whether the entire navigation bar is visible.</summary>
        public bool ShowNavBar
        {
            get => (bool)GetValue(ShowNavBarProperty);
            set => SetValue(ShowNavBarProperty, value);
        }

        /// <summary>Backing store for <see cref="ShowNavBar"/>.</summary>
        public static readonly DependencyProperty ShowNavBarProperty = DependencyProperty.Register(
            nameof(ShowNavBar), typeof(bool), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(true, OnShowNavBarChanged));

        private static void OnShowNavBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PluginChartNavBar)d)._ctx.ShowNavBar = (bool)e.NewValue;
        }

        #endregion

        #region ShowAllData

        /// <summary>
        /// Reflects the current ShowAllData toggle state.
        /// Setting this DP from the parent updates the button visual and the
        /// <see cref="PluginChartNavBarDataContext.CanNavigate"/> flag.
        /// </summary>
        public bool ShowAllData
        {
            get => (bool)GetValue(ShowAllDataProperty);
            set => SetValue(ShowAllDataProperty, value);
        }

        /// <summary>Backing store for <see cref="ShowAllData"/>.</summary>
        public static readonly DependencyProperty ShowAllDataProperty = DependencyProperty.Register(
            nameof(ShowAllData), typeof(bool), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(false, OnShowAllDataChanged));

        private static void OnShowAllDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            bool active = (bool)e.NewValue;

            ctrl._ctx.CanNavigate = !active;
            ctrl._ctx.ShowAllData = active;

            // Tooltip text reflects the current state so the user knows what the button will do next.
            ctrl._ctx.ShowAllToolTip = active
                ? ResourceProvider.GetString("LOCCommonNavShowAllActive")
                : ResourceProvider.GetString("LOCCommonNavShowAll");
        }

        #endregion

        #region NavLabel

        /// <summary>
        /// Optional info label shown as a badge on the right of the bar (e.g. "Week 12" or "Jan – Mar 2024").
        /// Setting this to <c>null</c> or empty hides the badge.
        /// </summary>
        public string NavLabel
        {
            get => (string)GetValue(NavLabelProperty);
            set => SetValue(NavLabelProperty, value);
        }

        /// <summary>Backing store for <see cref="NavLabel"/>.</summary>
        public static readonly DependencyProperty NavLabelProperty = DependencyProperty.Register(
            nameof(NavLabel), typeof(string), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnNavLabelChanged));

        private static void OnNavLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            string lbl = e.NewValue as string ?? string.Empty;
            ctrl._ctx.NavLabel = lbl;
            // HasNavLabel drives the badge Visibility converter in XAML.
            ctrl._ctx.HasNavLabel = !string.IsNullOrEmpty(lbl);
        }

        #endregion

        #region PageSize

        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage buttons.
        /// Typically bound to <see cref="AxisLimit"/> (if set) or to
        /// <c>PluginSettings.ChartXxxCountAbscissa</c>.
        /// When &lt;= 0, the PrevPage and NextPage buttons are hidden.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        /// <summary>Backing store for <see cref="PageSize"/>.</summary>
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize), typeof(int), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(0, OnPageSizeChanged));

        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            int size = (int)e.NewValue;

            ctrl._ctx.PageSize = size;
            // HasPageButtons drives PrevPage/NextPage button Visibility in XAML.
            ctrl._ctx.HasPageButtons = size > 0;
            // Rebuild tooltips to include the actual page size for discoverability.
            ctrl._ctx.PagePrevToolTip = BuildPageToolTip("LOCCommonNavPagePrev", size);
            ctrl._ctx.PageNextToolTip = BuildPageToolTip("LOCCommonNavPageNext", size);
        }

        /// <summary>
        /// Returns a localised tooltip string appended with the page size in parentheses
        /// when <paramref name="size"/> is positive, or just the bare localised string otherwise.
        /// </summary>
        /// <param name="locKey">Localisation key for the button label.</param>
        /// <param name="size">Current page size value.</param>
        private static string BuildPageToolTip(string locKey, int size)
        {
            string baseText = ResourceProvider.GetString(locKey);
            return size > 0 ? string.Format("{0} ({1})", baseText, size) : baseText;
        }

        #endregion

        #region AxisLimit

        /// <summary>
        /// Current X-axis data limit exposed to the parent chart control.
        /// Incremented / decremented by the AxisLimitIncrease / AxisLimitDecrease buttons.
        /// Clamped to a minimum of <see cref="AxisLimitMinimum"/> (5) — enforced on decrease.
        /// A value of <c>0</c> carries the "no explicit limit" semantic managed by the parent.
        /// Binding is two-way by default so the parent can both read and initialise the value.
        /// </summary>
        public int AxisLimit
        {
            get => (int)GetValue(AxisLimitProperty);
            set => SetValue(AxisLimitProperty, value);
        }

        /// <summary>Backing store for <see cref="AxisLimit"/>.</summary>
        public static readonly DependencyProperty AxisLimitProperty = DependencyProperty.Register(
            nameof(AxisLimit), typeof(int), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnAxisLimitChanged));

        private static void OnAxisLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            int limit = (int)e.NewValue;

            ctrl._ctx.AxisLimit = limit;
            // Decrease is blocked at the floor so the chart always shows at least AxisLimitMinimum points.
            ctrl._ctx.CanDecreaseAxisLimit = limit > AxisLimitMinimum;
            // Rebuild tooltips to show the resulting value after the click.
            ctrl._ctx.AxisLimitDecreaseToolTip = BuildAxisLimitToolTip("LOCCommonNavAxisLimitDecrease", limit);
            ctrl._ctx.AxisLimitIncreaseToolTip = BuildAxisLimitToolTip("LOCCommonNavAxisLimitIncrease", limit);
        }

        /// <summary>
        /// Returns a localised tooltip string appended with the current limit in parentheses
        /// when <paramref name="currentLimit"/> is positive, or just the bare localised string otherwise.
        /// </summary>
        /// <param name="locKey">Localisation key for the button label.</param>
        /// <param name="currentLimit">Current axis limit value.</param>
        private static string BuildAxisLimitToolTip(string locKey, int currentLimit)
        {
            string baseText = ResourceProvider.GetString(locKey);
            return currentLimit > 0
                ? string.Format("{0} ({1})", baseText, currentLimit)
                : baseText;
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Routed Events — Bubble strategy so the parent UserControl can catch them
        // without needing a direct reference to this control.
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Raised when the user clicks the First button.</summary>
        public static readonly RoutedEvent FirstClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(FirstClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user clicks the PrevPage button.</summary>
        public static readonly RoutedEvent PagePrevClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PagePrevClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user clicks the Prev button.</summary>
        public static readonly RoutedEvent PrevClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PrevClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user toggles the ShowAllData button.</summary>
        public static readonly RoutedEvent ShowAllToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(ShowAllToggled), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user clicks the Next button.</summary>
        public static readonly RoutedEvent NextClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(NextClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user clicks the NextPage button.</summary>
        public static readonly RoutedEvent PageNextClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PageNextClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>Raised when the user clicks the Last button.</summary>
        public static readonly RoutedEvent LastClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(LastClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>
        /// Raised after <see cref="AxisLimit"/> has been decremented by one.
        /// The new value is already reflected on the DP when this event fires.
        /// </summary>
        public static readonly RoutedEvent AxisLimitDecreasedEvent =
            EventManager.RegisterRoutedEvent(nameof(AxisLimitDecreased), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <summary>
        /// Raised after <see cref="AxisLimit"/> has been incremented by one.
        /// The new value is already reflected on the DP when this event fires.
        /// </summary>
        public static readonly RoutedEvent AxisLimitIncreasedEvent =
            EventManager.RegisterRoutedEvent(nameof(AxisLimitIncreased), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        /// <inheritdoc cref="FirstClickedEvent"/>
        public event RoutedEventHandler FirstClicked { add => AddHandler(FirstClickedEvent, value); remove => RemoveHandler(FirstClickedEvent, value); }
        /// <inheritdoc cref="PagePrevClickedEvent"/>
        public event RoutedEventHandler PagePrevClicked { add => AddHandler(PagePrevClickedEvent, value); remove => RemoveHandler(PagePrevClickedEvent, value); }
        /// <inheritdoc cref="PrevClickedEvent"/>
        public event RoutedEventHandler PrevClicked { add => AddHandler(PrevClickedEvent, value); remove => RemoveHandler(PrevClickedEvent, value); }
        /// <inheritdoc cref="ShowAllToggledEvent"/>
        public event RoutedEventHandler ShowAllToggled { add => AddHandler(ShowAllToggledEvent, value); remove => RemoveHandler(ShowAllToggledEvent, value); }
        /// <inheritdoc cref="NextClickedEvent"/>
        public event RoutedEventHandler NextClicked { add => AddHandler(NextClickedEvent, value); remove => RemoveHandler(NextClickedEvent, value); }
        /// <inheritdoc cref="PageNextClickedEvent"/>
        public event RoutedEventHandler PageNextClicked { add => AddHandler(PageNextClickedEvent, value); remove => RemoveHandler(PageNextClickedEvent, value); }
        /// <inheritdoc cref="LastClickedEvent"/>
        public event RoutedEventHandler LastClicked { add => AddHandler(LastClickedEvent, value); remove => RemoveHandler(LastClickedEvent, value); }
        /// <inheritdoc cref="AxisLimitDecreasedEvent"/>
        public event RoutedEventHandler AxisLimitDecreased { add => AddHandler(AxisLimitDecreasedEvent, value); remove => RemoveHandler(AxisLimitDecreasedEvent, value); }
        /// <inheritdoc cref="AxisLimitIncreasedEvent"/>
        public event RoutedEventHandler AxisLimitIncreased { add => AddHandler(AxisLimitIncreasedEvent, value); remove => RemoveHandler(AxisLimitIncreasedEvent, value); }

        // ────────────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Initialises the control and wires all commands to their RoutedEvents.</summary>
        public PluginChartNavBar()
        {
            _ctx = new PluginChartNavBarDataContext();

            _ctx.CmdFirst = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(FirstClickedEvent)));
            _ctx.CmdPagePrev = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(PagePrevClickedEvent)));
            _ctx.CmdPrev = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(PrevClickedEvent)));
            _ctx.CmdToggleShowAll = new RelayCommand(() =>
            {
                ShowAllData = !ShowAllData;
                RaiseEvent(new RoutedEventArgs(ShowAllToggledEvent));
            });
            _ctx.CmdNext = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(NextClickedEvent)));
            _ctx.CmdPageNext = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(PageNextClickedEvent)));
            _ctx.CmdLast = new RelayCommand(() => RaiseEvent(new RoutedEventArgs(LastClickedEvent)));

            _ctx.CmdAxisLimitDecrease = new RelayCommand(() =>
            {
                int next = Math.Max(AxisLimitMinimum, AxisLimit - 1);
                // Guard: no-op when already at the floor (CanDecreaseAxisLimit should have prevented
                // the button from being clickable, but defensive check is cheap).
                if (next == AxisLimit) { return; }
                AxisLimit = next;
                RaiseEvent(new RoutedEventArgs(AxisLimitDecreasedEvent));
            });

            _ctx.CmdAxisLimitIncrease = new RelayCommand(() =>
            {
                AxisLimit = AxisLimit + 1;
                RaiseEvent(new RoutedEventArgs(AxisLimitIncreasedEvent));
            });

            InitializeComponent();

            // Assign DataContext to the visual root, NOT to 'this', so that external DP bindings
            // declared on this control by consumers continue to resolve against the parent's
            // DataContext, while all internal XAML bindings use _ctx.
            PART_Root.DataContext = _ctx;

            // Seed localised tooltips; they will be refreshed whenever the relevant DP changes.
            _ctx.ShowAllToolTip = ResourceProvider.GetString("LOCCommonNavShowAll");
            _ctx.PagePrevToolTip = ResourceProvider.GetString("LOCCommonNavPagePrev");
            _ctx.PageNextToolTip = ResourceProvider.GetString("LOCCommonNavPageNext");
            _ctx.AxisLimitDecreaseToolTip = ResourceProvider.GetString("LOCCommonNavAxisLimitDecrease");
            _ctx.AxisLimitIncreaseToolTip = ResourceProvider.GetString("LOCCommonNavAxisLimitIncrease");
        }

        // ────────────────────────────────────────────────────────────────────
        // Static helpers
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a "first – last" range label from the rendered X-axis labels array.
        /// </summary>
        /// <remarks>
        /// The labels are expected to already be formatted for the current UI culture
        /// (e.g. via a date formatter or a localised week string),
        /// so no date parsing or re-formatting is performed here.
        /// <see cref="CultureInfo.CurrentCulture"/> is used for the composite format so that
        /// punctuation and text direction are handled correctly for all locales.
        /// </remarks>
        /// <param name="labels">The X-axis label array currently assigned to the chart.</param>
        /// <returns>
        /// A single label string when the range contains only one entry;
        /// a "first – last" string when the range spans multiple entries;
        /// <see cref="string.Empty"/> when <paramref name="labels"/> is <c>null</c> or empty.
        /// </returns>
        public static string BuildRangeLabel(string[] labels)
        {
            if (labels == null || labels.Length == 0)
            {
                return string.Empty;
            }

            string first = labels[0];
            string last = labels[labels.Length - 1];

            if (string.Equals(first, last, StringComparison.CurrentCulture))
            {
                return first;
            }

            // CurrentCulture ensures correct punctuation and RTL handling across locales.
            return string.Format(CultureInfo.CurrentCulture, "{0} – {1}", first, last);
        }

        // ────────────────────────────────────────────────────────────────────
        // Public imperative API — callable from parent code-behind without
        // needing to raise UI events directly.
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Programmatically triggers the First action.</summary>
        public void InvokeFirst() => RaiseEvent(new RoutedEventArgs(FirstClickedEvent));

        /// <summary>Programmatically triggers the PrevPage action.</summary>
        public void InvokePagePrev() => RaiseEvent(new RoutedEventArgs(PagePrevClickedEvent));

        /// <summary>Programmatically triggers the Prev action.</summary>
        public void InvokePrev() => RaiseEvent(new RoutedEventArgs(PrevClickedEvent));

        /// <summary>Programmatically toggles the ShowAllData state and raises the corresponding event.</summary>
        public void InvokeToggleShowAll()
        {
            ShowAllData = !ShowAllData;
            RaiseEvent(new RoutedEventArgs(ShowAllToggledEvent));
        }

        /// <summary>Programmatically triggers the Next action.</summary>
        public void InvokeNext() => RaiseEvent(new RoutedEventArgs(NextClickedEvent));

        /// <summary>Programmatically triggers the NextPage action.</summary>
        public void InvokePageNext() => RaiseEvent(new RoutedEventArgs(PageNextClickedEvent));

        /// <summary>Programmatically triggers the Last action.</summary>
        public void InvokeLast() => RaiseEvent(new RoutedEventArgs(LastClickedEvent));

        /// <summary>
        /// Programmatically decrements <see cref="AxisLimit"/> by one (floor: <see cref="AxisLimitMinimum"/>).
        /// No-op when already at the minimum.
        /// </summary>
        public void InvokeAxisLimitDecrease() => _ctx.CmdAxisLimitDecrease.Execute(null);

        /// <summary>Programmatically increments <see cref="AxisLimit"/> by one.</summary>
        public void InvokeAxisLimitIncrease() => _ctx.CmdAxisLimitIncrease.Execute(null);
    }

    // ────────────────────────────────────────────────────────────────────────
    // DataContext — internal ViewModel for PluginChartNavBar.
    // Kept as a separate class (not nested private) so XAML designer tools
    // and d:DataContext declarations can reference it by name.
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Observable ViewModel backing <see cref="PluginChartNavBar"/>.
    /// All properties are bound directly in the control's XAML template.
    /// Commands are assigned by <see cref="PluginChartNavBar"/>'s constructor.
    /// </summary>
    public class PluginChartNavBarDataContext : ObservableObject
    {
        private bool _showNavBar = true;
        /// <summary>Controls overall bar visibility — mirrors <see cref="PluginChartNavBar.ShowNavBar"/>.</summary>
        public bool ShowNavBar { get => _showNavBar; set => SetValue(ref _showNavBar, value); }

        private bool _canNavigate = true;
        /// <summary>
        /// <c>false</c> while ShowAllData is active — disables all directional buttons and
        /// the AxisLimitIncrease button (zooming in is irrelevant when all data is shown).
        /// </summary>
        public bool CanNavigate { get => _canNavigate; set => SetValue(ref _canNavigate, value); }

        private bool _showAllData;
        /// <summary>Mirror of the ShowAllData DP — available for XAML style triggers.</summary>
        public bool ShowAllData { get => _showAllData; set => SetValue(ref _showAllData, value); }

        private string _navLabel = string.Empty;
        /// <summary>Text shown in the range badge on the right of the bar.</summary>
        public string NavLabel { get => _navLabel; set => SetValue(ref _navLabel, value); }

        private bool _hasNavLabel;
        /// <summary><c>true</c> when <see cref="NavLabel"/> is non-empty — drives badge Visibility.</summary>
        public bool HasNavLabel { get => _hasNavLabel; set => SetValue(ref _hasNavLabel, value); }

        private int _pageSize;
        /// <summary>Number of items per page — drives tooltip text and HasPageButtons.</summary>
        public int PageSize { get => _pageSize; set => SetValue(ref _pageSize, value); }

        private bool _hasPageButtons;
        /// <summary><c>true</c> when <see cref="PageSize"/> &gt; 0 — drives PrevPage/NextPage Visibility.</summary>
        public bool HasPageButtons { get => _hasPageButtons; set => SetValue(ref _hasPageButtons, value); }

        private int _axisLimit;
        /// <summary>Current X-axis data limit — mirrors <see cref="PluginChartNavBar.AxisLimit"/>.</summary>
        public int AxisLimit { get => _axisLimit; set => SetValue(ref _axisLimit, value); }

        private bool _canDecreaseAxisLimit;
        /// <summary>
        /// <c>false</c> when <see cref="AxisLimit"/> is already at its minimum (5).
        /// Disables the AxisLimitDecrease button in XAML via IsEnabled binding.
        /// </summary>
        public bool CanDecreaseAxisLimit { get => _canDecreaseAxisLimit; set => SetValue(ref _canDecreaseAxisLimit, value); }

        private string _showAllToolTip = string.Empty;
        /// <summary>Dynamic tooltip for the centre toggle — updated on every ShowAllData change.</summary>
        public string ShowAllToolTip { get => _showAllToolTip; set => SetValue(ref _showAllToolTip, value); }

        private string _pagePrevToolTip = string.Empty;
        /// <summary>Tooltip for the PrevPage button — includes the current page size for discoverability.</summary>
        public string PagePrevToolTip { get => _pagePrevToolTip; set => SetValue(ref _pagePrevToolTip, value); }

        private string _pageNextToolTip = string.Empty;
        /// <summary>Tooltip for the NextPage button — includes the current page size for discoverability.</summary>
        public string PageNextToolTip { get => _pageNextToolTip; set => SetValue(ref _pageNextToolTip, value); }

        private string _axisLimitDecreaseToolTip = string.Empty;
        /// <summary>Tooltip for the AxisLimitDecrease button — includes the current limit for discoverability.</summary>
        public string AxisLimitDecreaseToolTip { get => _axisLimitDecreaseToolTip; set => SetValue(ref _axisLimitDecreaseToolTip, value); }

        private string _axisLimitIncreaseToolTip = string.Empty;
        /// <summary>Tooltip for the AxisLimitIncrease button — includes the current limit for discoverability.</summary>
        public string AxisLimitIncreaseToolTip { get => _axisLimitIncreaseToolTip; set => SetValue(ref _axisLimitIncreaseToolTip, value); }

        // Commands are assigned by PluginChartNavBar's constructor — not initialised here
        // because RelayCommand requires the closure to already reference the control instance.

        /// <summary>Navigates to the first data point.</summary>
        public RelayCommand CmdFirst { get; set; }
        /// <summary>Jumps back by <see cref="PageSize"/> steps.</summary>
        public RelayCommand CmdPagePrev { get; set; }
        /// <summary>Moves back by one step.</summary>
        public RelayCommand CmdPrev { get; set; }
        /// <summary>Toggles the ShowAllData mode on or off.</summary>
        public RelayCommand CmdToggleShowAll { get; set; }
        /// <summary>Moves forward by one step.</summary>
        public RelayCommand CmdNext { get; set; }
        /// <summary>Jumps forward by <see cref="PageSize"/> steps.</summary>
        public RelayCommand CmdPageNext { get; set; }
        /// <summary>Navigates to the last data point.</summary>
        public RelayCommand CmdLast { get; set; }
        /// <summary>Decrements <see cref="AxisLimit"/> by one (floor: 5).</summary>
        public RelayCommand CmdAxisLimitDecrease { get; set; }
        /// <summary>Increments <see cref="AxisLimit"/> by one.</summary>
        public RelayCommand CmdAxisLimitIncrease { get; set; }
    }
}