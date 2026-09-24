using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using CommonPluginsControls.Controls;
using CommonPlayniteShared.Common;
using LiveCharts;
using LiveCharts.Wpf;

namespace CommonPluginsControls.LiveChartsCommon
{
    /// <summary>
    /// Logique d'interaction pour CustomersTooltipForTime.xaml
    /// </summary>
    public partial class CustomerToolTipForTime : IChartTooltip
    {
        public TooltipSelectionMode? SelectionMode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;


        #region Properties
        private TooltipData _data;
        public TooltipData Data
        {
            get
            {
                SharedConverter sharedConverter = new SharedConverter();
                DataTitle = sharedConverter.Convert(_data, null, null, CultureInfo.CurrentCulture).ToString();
                DataTitleInfo = string.Empty;
                if (ShowWeekPeriode && DatesPeriodes.Count > 0)
                {
                    int.TryParse(Regex.Replace(DataTitle, @"[^\d]", string.Empty), out int WeekNumber);
                    if (WeekNumber > 0)
                    {
                        DateTime First = DatesPeriodes.Find(x => x.Week == WeekNumber)?.Monday ?? default(DateTime);
                        DateTime Last = DatesPeriodes.Find(x => x.Week == WeekNumber)?.Sunday ?? default(DateTime);
                        DataTitleInfo = "[" + First.ToString(Constants.DateUiFormat) + " - " + Last.ToString(Constants.DateUiFormat) + "]";
                    }
                }
                else if (_data?.Points?.Count > 0 && _data.Points[0].ChartPoint.Instance is CustomerForTime cust)
                {
                    if (!string.IsNullOrEmpty(cust.SecondaryName))
                    {
                        DataTitleInfo = cust.SecondaryName;
                    }
                }
                OnPropertyChanged("DataTitle");
                OnPropertyChanged("DataTitleInfo");

                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }

        public TextBlockWithIconMode Mode
        {
            get { return (TextBlockWithIconMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(TextBlockWithIconMode),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(TextBlockWithIconMode.IconTextFirstWithText));

        public bool ShowIcon
        {
            get { return (bool)GetValue(ShowIconProperty); }
            set { SetValue(ShowIconProperty, value); }
        }

        public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
            nameof(ShowIcon),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(false, OnShowContentFlagsChanged));

        /// <summary>
        /// When true, the series name is shown in the left tooltip column (Genres/Tags).
        /// When false with <see cref="ShowIcon"/> true, only the icon is shown (Games).
        /// When both false, only the playtime remains (Sources).
        /// </summary>
        public bool ShowLabel
        {
            get { return (bool)GetValue(ShowLabelProperty); }
            set { SetValue(ShowLabelProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register(
            nameof(ShowLabel),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(true, OnShowContentFlagsChanged));

        /// <summary>
        /// True when the left column (icon and/or name) should be visible.
        /// Dependency property so DataTemplate bindings refresh when flags change.
        /// </summary>
        public bool ShowLeftContent
        {
            get { return (bool)GetValue(ShowLeftContentProperty); }
            private set { SetValue(ShowLeftContentProperty, value); }
        }

        public static readonly DependencyProperty ShowLeftContentProperty = DependencyProperty.Register(
            nameof(ShowLeftContent),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(true));

        public bool ShowTitle
        {
            get { return (bool)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }

        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
            nameof(ShowTitle),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(false));

        public bool ShowWeekPeriode
        {
            get { return (bool)GetValue(ShowWeekPeriodeProperty); }
            set { SetValue(ShowWeekPeriodeProperty, value); }
        }

        public static readonly DependencyProperty ShowWeekPeriodeProperty = DependencyProperty.Register(
            nameof(ShowWeekPeriode),
            typeof(bool),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(false));

        public List<WeekStartEnd> DatesPeriodes
        {
            get { return (List<WeekStartEnd>)GetValue(DatesPeriodesProperty); }
            set { SetValue(DatesPeriodesProperty, value); }
        }

        public static readonly DependencyProperty DatesPeriodesProperty = DependencyProperty.Register(
            nameof(DatesPeriodes),
            typeof(List<WeekStartEnd>),
            typeof(CustomerToolTipForTime),
            new FrameworkPropertyMetadata(new List<WeekStartEnd>()));

        public string DataTitle { get; set; }
        public string DataTitleInfo { get; set; }
        #endregion


        private static void OnShowContentFlagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CustomerToolTipForTime tooltip = d as CustomerToolTipForTime;
            if (tooltip == null)
            {
                return;
            }

            tooltip.UpdateShowLeftContent();
            tooltip.ApplyModeFromFlags();
        }

        private void UpdateShowLeftContent()
        {
            ShowLeftContent = ShowIcon || ShowLabel;
            OnPropertyChanged(nameof(ShowLeftContent));
        }

        /// <summary>
        /// Maps ShowIcon/ShowLabel to a TextBlockWithIconMode for API compatibility (tooltip XAML uses ShowIcon/ShowLabel directly).
        /// </summary>
        private void ApplyModeFromFlags()
        {
            if (ShowIcon && !ShowLabel)
            {
                if (Mode == TextBlockWithIconMode.IconTextFirstWithText
                    || Mode == TextBlockWithIconMode.IconTextFirstOnly
                    || Mode == TextBlockWithIconMode.IconTextOnly
                    || Mode == TextBlockWithIconMode.IconTextWithText)
                {
                    Mode = TextBlockWithIconMode.IconTextFirstOnly;
                }
                else
                {
                    Mode = TextBlockWithIconMode.IconFirstOnly;
                }
            }
            else if (!ShowIcon && ShowLabel)
            {
                Mode = TextBlockWithIconMode.TextOnly;
            }
            else if (ShowIcon && ShowLabel)
            {
                if (Mode == TextBlockWithIconMode.IconTextFirstOnly
                    || Mode == TextBlockWithIconMode.IconTextOnly
                    || Mode == TextBlockWithIconMode.IconTextFirstWithText
                    || Mode == TextBlockWithIconMode.IconTextWithText)
                {
                    Mode = TextBlockWithIconMode.IconTextFirstWithText;
                }
                else if (Mode == TextBlockWithIconMode.IconFirstOnly || Mode == TextBlockWithIconMode.IconOnly)
                {
                    Mode = TextBlockWithIconMode.IconFirstWithText;
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public CustomerToolTipForTime()
        {
            InitializeComponent();

            DataContext = this;
            UpdateShowLeftContent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // LiveCharts may replace DataContext when hosting the tooltip; restore self for flag bindings.
            if (!ReferenceEquals(DataContext, this))
            {
                DataContext = this;
            }

            UpdateShowLeftContent();
            ApplyModeFromFlags();
        }
    }
}
