using CommonPluginsControls.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CommonPluginsControls.Controls
{
    public partial class TimePicker : UserControl
    {
        private readonly TimePickerViewModel _vm;

        public static readonly RoutedEvent TimeChangedEvent =
            EventManager.RegisterRoutedEvent(
                "TimeChanged",
                RoutingStrategy.Direct,
                typeof(RoutedEventHandler),
                typeof(TimePicker));

        public event RoutedEventHandler TimeChanged
        {
            add => AddHandler(TimeChangedEvent, value);
            remove => RemoveHandler(TimeChangedEvent, value);
        }

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(string),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTimePropertyChanged));

        public string SelectedTime
        {
            get => (string)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        private static void OnSelectedTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            var newValue = e.NewValue as string ?? string.Empty;

            if (string.Compare(newValue, control._vm.SelectedTime ?? string.Empty, StringComparison.Ordinal) != 0)
            {
                control._vm.SetFromString(newValue);
            }
        }

        public static readonly DependencyProperty ShowSecondsProperty =
            DependencyProperty.Register(
                nameof(ShowSeconds),
                typeof(bool),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(false, OnShowSecondsPropertyChanged));

        public bool ShowSeconds
        {
            get => (bool)GetValue(ShowSecondsProperty);
            set => SetValue(ShowSecondsProperty, value);
        }

        private static void OnShowSecondsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            control._vm.ShowSeconds = (bool)e.NewValue;
        }

        public TimePickerViewModel ViewModel => _vm;

        public TimePicker()
        {
            _vm = new TimePickerViewModel();
            InitializeComponent();

            _vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(TimePickerViewModel.SelectedTime))
                {
                    if (string.Compare(SelectedTime ?? string.Empty, _vm.SelectedTime ?? string.Empty, StringComparison.Ordinal) != 0)
                    {
                        SetCurrentValue(SelectedTimeProperty, _vm.SelectedTime);
                    }
                    RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
                }
            };
        }

        private void Hours_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Hours++;
            else _vm.Hours--;
            e.Handled = true;
        }

        private void Minutes_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Minutes++;
            else _vm.Minutes--;
            e.Handled = true;
        }

        private void Seconds_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Seconds++;
            else _vm.Seconds--;
            e.Handled = true;
        }

        private void NumericField_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void NumericField_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
            => (sender as TextBox)?.SelectAll();

        private void NumericField_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }

        /// <summary>
        /// Commits typed digits to the view model. Text boxes use OneWay display bindings, so
        /// keyboard input must be applied explicitly (spinner commands update the VM directly).
        /// </summary>
        private void NumericField_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null)
            {
                return;
            }

            var tag = tb.Tag as string;
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            var text = (tb.Text ?? string.Empty).Trim();
            if (text.Length == 0 || !int.TryParse(text, out var value))
            {
                BindingOperations.GetBindingExpression(tb, TextBox.TextProperty)?.UpdateTarget();
                return;
            }

            if (string.Equals(tag, "Hours", StringComparison.Ordinal))
            {
                _vm.Hours = value;
            }
            else if (string.Equals(tag, "Minutes", StringComparison.Ordinal))
            {
                _vm.Minutes = value;
            }
            else if (string.Equals(tag, "Seconds", StringComparison.Ordinal))
            {
                _vm.Seconds = value;
            }
        }

        public string GetValueAsString() => _vm.SelectedTime;

        public TimeSpan GetValueAsTimeSpan() => _vm.ToTimeSpan();

        public void SetValueAsString(string hour, string minute, string second = "00")
            => _vm.SetFromString(string.Format("{0}:{1}:{2}", hour, minute, second));
    }
}