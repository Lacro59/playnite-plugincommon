using Playnite.SDK;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Reusable navigation bar for chart controls (PluginChartTime, PluginChartLog, etc.).
    ///
    /// Design contract:
    ///   - Seven buttons: First, PrevPage, Prev, ShowAll toggle, Next, NextPage, Last.
    ///   - First / PrevPage / Prev / Next / NextPage / Last are disabled when CanNavigate=False
    ///     (i.e. ShowAllData is active — navigation is meaningless over the full dataset).
    ///   - The centre button toggles ShowAllData; its visual style reflects the active state.
    ///   - An optional NavLabel badge is shown on the right (e.g. "Week 12").
    ///   - PageSize controls how many steps PrevPage/NextPage jump.
    ///     When PageSize &lt;= 0, the PrevPage/NextPage buttons are hidden.
    ///
    /// Integration:
    ///   - Consumers subscribe to RoutedEvents for XAML-side wiring.
    ///   - Consumers call Invoke*() methods for programmatic triggering.
    ///   - The parent resolves PageSize = AxisLimit ?? PluginSettings.ChartXxxCountAbscissa
    ///     and binds it to this DP.
    /// </summary>
    public partial class PluginChartNavBar : UserControl
    {
        // ── Private DataContext ───────────────────────────────────────────────
        private readonly PluginChartNavBarDataContext _ctx;


        // ────────────────────────────────────────────────────────────────────
        // Dependency Properties
        // ────────────────────────────────────────────────────────────────────

        #region ShowNavBar

        /// <summary>Controls whether the navigation bar is visible.</summary>
        public bool ShowNavBar
        {
            get => (bool)GetValue(ShowNavBarProperty);
            set => SetValue(ShowNavBarProperty, value);
        }
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
        /// Setting this DP from the parent updates the button visual and the CanNavigate flag.
        /// </summary>
        public bool ShowAllData
        {
            get => (bool)GetValue(ShowAllDataProperty);
            set => SetValue(ShowAllDataProperty, value);
        }
        public static readonly DependencyProperty ShowAllDataProperty = DependencyProperty.Register(
            nameof(ShowAllData), typeof(bool), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(false, OnShowAllDataChanged));

        private static void OnShowAllDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            bool active = (bool)e.NewValue;

            ctrl._ctx.CanNavigate = !active;
            ctrl._ctx.ShowAllData = active;

            ctrl._ctx.ShowAllToolTip = active
                ? ResourceProvider.GetString("LOCCommonNavShowAllActive")
                : ResourceProvider.GetString("LOCCommonNavShowAll");
        }

        #endregion

        #region NavLabel

        /// <summary>Optional info label shown as a badge on the right (e.g. "Week 12").</summary>
        public string NavLabel
        {
            get => (string)GetValue(NavLabelProperty);
            set => SetValue(NavLabelProperty, value);
        }
        public static readonly DependencyProperty NavLabelProperty = DependencyProperty.Register(
            nameof(NavLabel), typeof(string), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(string.Empty, OnNavLabelChanged));

        private static void OnNavLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            string lbl = e.NewValue as string ?? string.Empty;
            ctrl._ctx.NavLabel = lbl;
            ctrl._ctx.HasNavLabel = !string.IsNullOrEmpty(lbl);
        }

        #endregion

        #region PageSize

        /// <summary>
        /// Number of items skipped by the PrevPage / NextPage buttons.
        /// Typically bound to AxisLimit (if set) or to PluginSettings.ChartXxxCountAbscissa.
        /// When &lt;= 0, the PrevPage and NextPage buttons are hidden.
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            nameof(PageSize), typeof(int), typeof(PluginChartNavBar),
            new FrameworkPropertyMetadata(0, OnPageSizeChanged));

        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PluginChartNavBar)d;
            int size = (int)e.NewValue;

            // Update DataContext so XAML Visibility binding reacts.
            ctrl._ctx.PageSize = size;
            ctrl._ctx.HasPageButtons = size > 0;

            // Rebuild the tooltip text to include the actual page size.
            ctrl._ctx.PagePrevToolTip = BuildPageToolTip("LOCCommonNavPagePrev", size);
            ctrl._ctx.PageNextToolTip = BuildPageToolTip("LOCCommonNavPageNext", size);
        }

        /// <summary>
        /// Builds a tooltip string: localised prefix + " (" + size + ")" when size > 0,
        /// or just the bare localised string when size is unknown.
        /// </summary>
        private static string BuildPageToolTip(string locKey, int size)
        {
            string baseText = ResourceProvider.GetString(locKey);
            return size > 0 ? string.Format("{0} ({1})", baseText, size) : baseText;
        }

        #endregion


        // ────────────────────────────────────────────────────────────────────
        // Routed Events — bubbling so the parent UserControl can catch them
        // ────────────────────────────────────────────────────────────────────

        public static readonly RoutedEvent FirstClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(FirstClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent PagePrevClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PagePrevClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent PrevClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PrevClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent ShowAllToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(ShowAllToggled), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent NextClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(NextClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent PageNextClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PageNextClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public static readonly RoutedEvent LastClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(LastClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PluginChartNavBar));

        public event RoutedEventHandler FirstClicked { add => AddHandler(FirstClickedEvent, value); remove => RemoveHandler(FirstClickedEvent, value); }
        public event RoutedEventHandler PagePrevClicked { add => AddHandler(PagePrevClickedEvent, value); remove => RemoveHandler(PagePrevClickedEvent, value); }
        public event RoutedEventHandler PrevClicked { add => AddHandler(PrevClickedEvent, value); remove => RemoveHandler(PrevClickedEvent, value); }
        public event RoutedEventHandler ShowAllToggled { add => AddHandler(ShowAllToggledEvent, value); remove => RemoveHandler(ShowAllToggledEvent, value); }
        public event RoutedEventHandler NextClicked { add => AddHandler(NextClickedEvent, value); remove => RemoveHandler(NextClickedEvent, value); }
        public event RoutedEventHandler PageNextClicked { add => AddHandler(PageNextClickedEvent, value); remove => RemoveHandler(PageNextClickedEvent, value); }
        public event RoutedEventHandler LastClicked { add => AddHandler(LastClickedEvent, value); remove => RemoveHandler(LastClickedEvent, value); }


        // ────────────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────────────

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

            InitializeComponent();

            // Assign to visual root, NOT to this — keeps external DP bindings resolvable
            // against the parent's DataContext while internal bindings use _ctx.
            PART_Root.DataContext = _ctx;

            _ctx.ShowAllToolTip = ResourceProvider.GetString("LOCCommonNavShowAll");
            _ctx.PagePrevToolTip = ResourceProvider.GetString("LOCCommonNavPagePrev");
            _ctx.PageNextToolTip = ResourceProvider.GetString("LOCCommonNavPageNext");
        }



        // ────────────────────────────────────────────────────────────────────
        // Public imperative API — callable from parent code-behind
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Programmatically triggers the First action.</summary>
        public void InvokeFirst() => RaiseEvent(new RoutedEventArgs(FirstClickedEvent));
        /// <summary>Programmatically triggers the PrevPage action.</summary>
        public void InvokePagePrev() => RaiseEvent(new RoutedEventArgs(PagePrevClickedEvent));
        /// <summary>Programmatically triggers the Prev action.</summary>
        public void InvokePrev() => RaiseEvent(new RoutedEventArgs(PrevClickedEvent));
        /// <summary>Programmatically toggles the ShowAllData state.</summary>
        public void InvokeToggleShowAll() { ShowAllData = !ShowAllData; RaiseEvent(new RoutedEventArgs(ShowAllToggledEvent)); }
        /// <summary>Programmatically triggers the Next action.</summary>
        public void InvokeNext() => RaiseEvent(new RoutedEventArgs(NextClickedEvent));
        /// <summary>Programmatically triggers the NextPage action.</summary>
        public void InvokePageNext() => RaiseEvent(new RoutedEventArgs(PageNextClickedEvent));
        /// <summary>Programmatically triggers the Last action.</summary>
        public void InvokeLast() => RaiseEvent(new RoutedEventArgs(LastClickedEvent));
    }


    // ────────────────────────────────────────────────────────────────────────
    // DataContext — internal to PluginChartNavBar
    // ────────────────────────────────────────────────────────────────────────

    public class PluginChartNavBarDataContext : ObservableObject
    {
        private bool _showNavBar = true;
        public bool ShowNavBar { get => _showNavBar; set => SetValue(ref _showNavBar, value); }

        private bool _canNavigate = true;
        /// <summary>False while ShowAllData is active — disables all directional buttons.</summary>
        public bool CanNavigate { get => _canNavigate; set => SetValue(ref _canNavigate, value); }

        private bool _showAllData;
        /// <summary>Mirror of the ShowAllData DP — available for future XAML bindings.</summary>
        public bool ShowAllData { get => _showAllData; set => SetValue(ref _showAllData, value); }

        private string _navLabel = string.Empty;
        public string NavLabel { get => _navLabel; set => SetValue(ref _navLabel, value); }

        private bool _hasNavLabel;
        public bool HasNavLabel { get => _hasNavLabel; set => SetValue(ref _hasNavLabel, value); }

        private int _pageSize;
        /// <summary>Number of items per page; drives button Visibility via HasPageButtons.</summary>
        public int PageSize { get => _pageSize; set => SetValue(ref _pageSize, value); }

        private bool _hasPageButtons;
        /// <summary>True when PageSize > 0 — drives PrevPage/NextPage button Visibility.</summary>
        public bool HasPageButtons { get => _hasPageButtons; set => SetValue(ref _hasPageButtons, value); }

        private string _showAllToolTip = string.Empty;
        /// <summary>Dynamic tooltip for the centre button — changes when ShowAllData toggles.</summary>
        public string ShowAllToolTip { get => _showAllToolTip; set => SetValue(ref _showAllToolTip, value); }

        private string _pagePrevToolTip = string.Empty;
        /// <summary>Tooltip for PrevPage button — includes page size when known.</summary>
        public string PagePrevToolTip { get => _pagePrevToolTip; set => SetValue(ref _pagePrevToolTip, value); }

        private string _pageNextToolTip = string.Empty;
        /// <summary>Tooltip for NextPage button — includes page size when known.</summary>
        public string PageNextToolTip { get => _pageNextToolTip; set => SetValue(ref _pageNextToolTip, value); }

        // Commands are assigned in PluginChartNavBar constructor.
        public RelayCommand CmdFirst { get; set; }
        public RelayCommand CmdPagePrev { get; set; }
        public RelayCommand CmdPrev { get; set; }
        public RelayCommand CmdToggleShowAll { get; set; }
        public RelayCommand CmdNext { get; set; }
        public RelayCommand CmdPageNext { get; set; }
        public RelayCommand CmdLast { get; set; }
    }
}