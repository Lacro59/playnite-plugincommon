using Playnite.SDK;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CommonPluginsControls.ViewModels
{
    /// <summary>
    /// ViewModel for the TimePicker control.
    /// Exposes Hours, Minutes and optionally Seconds as bindable integers,
    /// and aggregates them into a formatted string property (SelectedTime).
    /// All logic lives here — zero code-behind.
    /// </summary>
    public class TimePickerViewModel : INotifyPropertyChanged
    {
        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Backing fields ────────────────────────────────────────────────────

        private int _hours;
        private int _minutes;
        private int _seconds;
        private bool _showSeconds;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current hour value (0–23).</summary>
        public int Hours
        {
            get => _hours;
            set
            {
                // Wrap around: 23 + 1 = 0, 0 - 1 = 23
                _hours = ((value % 24) + 24) % 24;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HoursDisplay));
                OnPropertyChanged(nameof(SelectedTime));
            }
        }

        /// <summary>Current minute value (0–59).</summary>
        public int Minutes
        {
            get => _minutes;
            set
            {
                _minutes = ((value % 60) + 60) % 60;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinutesDisplay));
                OnPropertyChanged(nameof(SelectedTime));
            }
        }

        /// <summary>Current second value (0–59).</summary>
        public int Seconds
        {
            get => _seconds;
            set
            {
                _seconds = ((value % 60) + 60) % 60;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SecondsDisplay));
                OnPropertyChanged(nameof(SelectedTime));
            }
        }

        /// <summary>Zero-padded display string for hours.</summary>
        public string HoursDisplay => _hours.ToString("D2");

        /// <summary>Zero-padded display string for minutes.</summary>
        public string MinutesDisplay => _minutes.ToString("D2");

        /// <summary>Zero-padded display string for seconds.</summary>
        public string SecondsDisplay => _seconds.ToString("D2");

        /// <summary>
        /// Aggregated time string.
        /// Format: "HH:mm:ss" when ShowSeconds is true, "HH:mm" otherwise.
        /// This is the primary bindable output of the control.
        /// </summary>
        public string SelectedTime
        {
            get => _showSeconds
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", _hours, _minutes, _seconds)
                : string.Format("{0:D2}:{1:D2}", _hours, _minutes);
        }

        /// <summary>
        /// Controls whether the seconds column is visible.
        /// Changing this value re-evaluates SelectedTime format.
        /// </summary>
        public bool ShowSeconds
        {
            get => _showSeconds;
            set
            {
                if (_showSeconds == value) return;
                _showSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTime));
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Increments hours by 1, with wrap-around at 23.</summary>
        public ICommand IncrementHoursCommand { get; }

        /// <summary>Decrements hours by 1, with wrap-around at 0.</summary>
        public ICommand DecrementHoursCommand { get; }

        /// <summary>Increments minutes by 1, with wrap-around at 59.</summary>
        public ICommand IncrementMinutesCommand { get; }

        /// <summary>Decrements minutes by 1, with wrap-around at 0.</summary>
        public ICommand DecrementMinutesCommand { get; }

        /// <summary>Increments seconds by 1, with wrap-around at 59.</summary>
        public ICommand IncrementSecondsCommand { get; }

        /// <summary>Decrements seconds by 1, with wrap-around at 0.</summary>
        public ICommand DecrementSecondsCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public TimePickerViewModel()
        {
            // Initialise to current time
            var now = DateTime.Now;
            _hours = now.Hour;
            _minutes = now.Minute;
            _seconds = now.Second;

            // Wire up RelayCommands — lambdas keep them concise (C# 7.0 compliant)
            IncrementHoursCommand = new RelayCommand(() => Hours++);
            DecrementHoursCommand = new RelayCommand(() => Hours--);
            IncrementMinutesCommand = new RelayCommand(() => Minutes++);
            DecrementMinutesCommand = new RelayCommand(() => Minutes--);
            IncrementSecondsCommand = new RelayCommand(() => Seconds++);
            DecrementSecondsCommand = new RelayCommand(() => Seconds--);
        }

        // ── Public helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Sets the time from a "HH:mm" or "HH:mm:ss" string.
        /// Silently ignores malformed input.
        /// </summary>
        public void SetFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value.Split(':');
            if (parts.Length < 2) return;

            int h, m, s;
            if (!int.TryParse(parts[0], out h)) return;
            if (!int.TryParse(parts[1], out m)) return;
            s = (parts.Length >= 3 && int.TryParse(parts[2], out s)) ? s : 0;

            // Assign through properties so bounds-checking + notifications fire
            Hours = h;
            Minutes = m;
            Seconds = s;
        }

        /// <summary>
        /// Sets the time directly from a TimeSpan for convenience.
        /// </summary>
        public void SetFromTimeSpan(TimeSpan value)
        {
            Hours = value.Hours;
            Minutes = value.Minutes;
            Seconds = value.Seconds;
        }

        /// <summary>Returns the current value as a TimeSpan.</summary>
        public TimeSpan ToTimeSpan()
            => new TimeSpan(_hours, _minutes, _seconds);
    }
}