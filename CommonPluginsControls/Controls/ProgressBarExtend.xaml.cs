using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Logique d'interaction pour ProgressBarExtend.xaml
    /// </summary>
    public partial class ProgressBarExtend : UserControl
    {
        private Decorator _indicator;
        private Decorator indicator
        {
            get
            {
                if (_indicator == null)
                {
                    _indicator = (Decorator)PART_ProgressBar.Template.FindName("PART_Indicator", PART_ProgressBar);
                }
                return _indicator;
            }

            set
            {
                _indicator = value;
            }
        }


        #region Text property
        public string TextValue
        {
            get
            {
                return (string)GetValue(TextValueProperty);
            }

            set
            {
                SetValue(TextValueProperty, value);
            }
        }

        public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
            nameof(TextValue), 
            typeof(string), 
            typeof(ProgressBarExtend), 
            new PropertyMetadata(string.Empty, TextValuePropertyChangedCallback));

        private static void TextValuePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ProgressBarExtend;
            obj.TextValue = (string)e.NewValue;
        }

        public bool TextInsideVisibility
        {
            get
            {
                return (bool)GetValue(TextInsideVisibilityProperty);
            }

            set
            {
                SetValue(TextInsideVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty TextInsideVisibilityProperty = DependencyProperty.Register(
            nameof(TextInsideVisibility), 
            typeof(bool), 
            typeof(ProgressBarExtend), 
            new PropertyMetadata(false, TextVisibilityChangedCallback));

        public bool TextAboveVisibility
        {
            get
            {
                return (bool)GetValue(TextAboveVisibilityProperty);
            }

            set
            {
                SetValue(TextAboveVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty TextAboveVisibilityProperty = DependencyProperty.Register(
            nameof(TextAboveVisibility),
            typeof(bool),
            typeof(ProgressBarExtend),
            new PropertyMetadata(false, TextVisibilityChangedCallback));

        public bool TextBelowVisibility
        {
            get
            {
                return (bool)GetValue(TextBelowVisibilityProperty);
            }

            set
            {
                SetValue(TextBelowVisibilityProperty, value);
            }
        }

        public static readonly DependencyProperty TextBelowVisibilityProperty = DependencyProperty.Register(
            nameof(TextBelowVisibility),
            typeof(bool),
            typeof(ProgressBarExtend),
            new PropertyMetadata(false, TextVisibilityChangedCallback));

        private static void TextVisibilityChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ProgressBarExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                switch (e.Property.Name)
                {
                    case "TextInsideVisibility":
                        obj.TextInsideVisibility = (bool)e.NewValue;
                        break;
                    case "TextAboveVisibility":
                        obj.TextAboveVisibility = (bool)e.NewValue;
                        break;
                    case "TextBelowVisibility":
                        obj.TextBelowVisibility = (bool)e.NewValue;
                        break;
                }
            }
        }
        #endregion


        #region ProgressBar property
        public double Minimum
        {
            get
            {
                return (double)GetValue(MinimumProperty);
            }

            set
            {
                SetValue(MinimumProperty, value);
            }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN, ProgressBarChangedCallback));

        public double Maximum
        {
            get
            {
                return (double)GetValue(MaximumProperty);
            }

            set
            {
                SetValue(MaximumProperty, value);
            }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN, ProgressBarChangedCallback));

        public double Value
        {
            get
            {
                return (double)GetValue(ValueProperty);
            }

            set
            {
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN, ProgressBarChangedCallback));


        public double MarginLeft
        {
            get
            {
                return (double)GetValue(MarginLeftProperty);
            }

            set
            {
                SetValue(MarginLeftProperty, value);
            }
        }

        public static readonly DependencyProperty MarginLeftProperty = DependencyProperty.Register(
            nameof(MarginLeft),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN, ProgressBarChangedCallback));


        public double MarginRight
        {
            get
            {
                return (double)GetValue(MarginRightProperty);
            }

            set
            {
                SetValue(MarginRightProperty, value);
            }
        }

        public static readonly DependencyProperty MarginRightProperty = DependencyProperty.Register(
            nameof(MarginRight),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN, ProgressBarChangedCallback));


        public double IndicatorHeight
        {
            get
            {
                return (double)GetValue(IndicatorHeightProperty);
            }

            set
            {
                SetValue(IndicatorHeightProperty, value);
            }
        }

        public static readonly DependencyProperty IndicatorHeightProperty = DependencyProperty.Register(
            nameof(IndicatorHeight),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN));

        public double IndicatorDemiHeight
        {
            get
            {
                return (double)GetValue(IndicatorDemiHeightProperty);
            }

            set
            {
                SetValue(IndicatorDemiHeightProperty, value);
            }
        }

        public static readonly DependencyProperty IndicatorDemiHeightProperty = DependencyProperty.Register(
            nameof(IndicatorDemiHeight),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN));

        public double IndicatorWidth
        {
            get
            {
                return (double)GetValue(IndicatorWidthProperty);
            }

            set
            {
                SetValue(IndicatorWidthProperty, value);
            }
        }

        public static readonly DependencyProperty IndicatorWidthProperty = DependencyProperty.Register(
            nameof(IndicatorWidth),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN));


        public double TextWidth
        {
            get
            {
                return (double)GetValue(TextWidthProperty);
            }

            set
            {
                SetValue(TextWidthProperty, value);
            }
        }

        public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register(
            nameof(TextWidth),
            typeof(double),
            typeof(ProgressBarExtend),
            new PropertyMetadata(double.NaN));


        private static void ProgressBarChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ProgressBarExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                switch (e.Property.Name)
                {
                    case "Minimum":
                        obj.Minimum = (double)e.NewValue;
                        break;
                    case "Maximum":
                        obj.Maximum = (double)e.NewValue;
                        break;
                    case "Value":
                        obj.Value = (double)e.NewValue;
                        break;
                    case "MarginLeft":
                        obj.MarginLeft = (double)e.NewValue;
                        break;
                    case "MarginRight":
                        obj.MarginRight = (double)e.NewValue;
                        break;
                    case "TextWidth":
                        obj.TextWidth = (double)e.NewValue;
                        break;
                }
            }
        }
        #endregion


        public ProgressBarExtend()
        {
            InitializeComponent();
        }


        public double GetIndicatorWidth()
        {
            if (indicator != null)
            {
                return indicator.ActualWidth;
            }
            else
            {
                return 0;
            }
        }

        public double GetIndicatorHeight()
        {
            if (PART_ProgressBar == null)
            {
                return 0;
            }

            return PART_ProgressBar.ActualHeight;
        }


        private void PART_ProgressBar_LayoutUpdated(object sender, EventArgs e)
        {
            IndicatorHeight = GetIndicatorHeight();
            IndicatorDemiHeight = IndicatorHeight / 2;
            IndicatorWidth = GetIndicatorWidth();
        }
    }
}
