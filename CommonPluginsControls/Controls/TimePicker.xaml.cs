using CommonPluginsControls.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Modernized TimePicker control.
    /// The code-behind is intentionally minimal: it only owns DependencyProperties
    /// and delegates all logic to <see cref="TimePickerViewModel"/>.
    /// </summary>
    public partial class TimePicker : UserControl
    {
        // ── Internal ViewModel ────────────────────────────────────────────────

        private readonly TimePickerViewModel _vm;

        // ── Routed event (backward compatibility) ─────────────────────────────

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

        // ── DependencyProperty: SelectedTime (string, two-way bindable) ───────

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(string),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTimePropertyChanged));

        /// <summary>
        /// Primary bindable property.
        /// Format: "HH:mm" or "HH:mm:ss" depending on <see cref="ShowSeconds"/>.
        /// Supports two-way binding from parent views.
        /// </summary>
        public string SelectedTime
        {
            get => (string)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        private static void OnSelectedTimePropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            var newValue = e.NewValue as string;

            // Avoid feedback loop when the change originates from the VM itself
            if (newValue != control._vm.SelectedTime)
            {
                control._vm.SetFromString(newValue);
            }
        }

        // ── DependencyProperty: ShowSeconds ───────────────────────────────────

        public static readonly DependencyProperty ShowSecondsProperty =
            DependencyProperty.Register(
                nameof(ShowSeconds),
                typeof(bool),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(false, OnShowSecondsPropertyChanged));

        /// <summary>
        /// When true, the seconds column is displayed and SelectedTime uses "HH:mm:ss".
        /// Default: false ("HH:mm").
        /// </summary>
        public bool ShowSeconds
        {
            get => (bool)GetValue(ShowSecondsProperty);
            set => SetValue(ShowSecondsProperty, value);
        }

        private static void OnShowSecondsPropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimePicker)d;
            control._vm.ShowSeconds = (bool)e.NewValue;
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public TimePicker()
        {
            _vm = new TimePickerViewModel();
            DataContext = _vm;

            InitializeComponent();

            // Propagate ViewModel changes back to the DependencyProperty
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TimePickerViewModel.SelectedTime))
                {
                    // Update the DP only when the value actually changed
                    if (SelectedTime != _vm.SelectedTime)
                    {
                        SelectedTime = _vm.SelectedTime;
                    }

                    // Raise the routed event for backward-compatible subscribers
                    RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
                }
            };
        }

        // ── Mouse wheel support ───────────────────────────────────────────────

        /// <summary>
        /// Allows incrementing/decrementing hours with the mouse wheel
        /// when the cursor is over the hour field.
        /// </summary>
        private void Hours_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Hours++;
            else _vm.Hours--;
            e.Handled = true;
        }

        /// <summary>Mouse wheel support for the minutes field.</summary>
        private void Minutes_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Minutes++;
            else _vm.Minutes--;
            e.Handled = true;
        }

        /// <summary>Mouse wheel support for the seconds field.</summary>
        private void Seconds_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) _vm.Seconds++;
            else _vm.Seconds--;
            e.Handled = true;
        }

        // ── Keyboard text validation (digits only) ────────────────────────────

        private void NumericField_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block any non-digit character
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        // ── Focus helpers (select all on focus for quick edit) ────────────────

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

        // ── Public API (backward compatibility helpers) ───────────────────────

        /// <summary>Returns the current time as "HH:mm:ss" or "HH:mm".</summary>
        public string GetValueAsString() => _vm.SelectedTime;

        /// <summary>Returns the current value as a <see cref="TimeSpan"/>.</summary>
        public TimeSpan GetValueAsTimeSpan() => _vm.ToTimeSpan();

        /// <summary>Sets the time programmatically from individual string parts.</summary>
        public void SetValueAsString(string hour, string minute, string second = "00")
            => _vm.SetFromString(string.Format("{0}:{1}:{2}", hour, minute, second));
    }
}