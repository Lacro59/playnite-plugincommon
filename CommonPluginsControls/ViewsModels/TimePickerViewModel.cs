using Playnite.SDK;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CommonPluginsControls.ViewModels
{
    public class TimePickerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int _hours;
        private int _minutes;
        private int _seconds;
        private bool _showSeconds;

        public int Hours
        {
            get => _hours;
            set
            {
                _hours = ((value % 24) + 24) % 24;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HoursDisplay));
                OnPropertyChanged(nameof(SelectedTime));
            }
        }

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

        public string HoursDisplay => _hours.ToString("D2");
        public string MinutesDisplay => _minutes.ToString("D2");
        public string SecondsDisplay => _seconds.ToString("D2");

        public string SelectedTime
        {
            get => _showSeconds
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", _hours, _minutes, _seconds)
                : string.Format("{0:D2}:{1:D2}", _hours, _minutes);
        }

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

        public ICommand IncrementHoursCommand { get; }
        public ICommand DecrementHoursCommand { get; }
        public ICommand IncrementMinutesCommand { get; }
        public ICommand DecrementMinutesCommand { get; }
        public ICommand IncrementSecondsCommand { get; }
        public ICommand DecrementSecondsCommand { get; }

        public TimePickerViewModel()
        {
            var now = DateTime.Now;
            _hours = now.Hour;
            _minutes = now.Minute;
            _seconds = now.Second;

            IncrementHoursCommand = new RelayCommand(() => Hours++);
            DecrementHoursCommand = new RelayCommand(() => Hours--);
            IncrementMinutesCommand = new RelayCommand(() => Minutes++);
            DecrementMinutesCommand = new RelayCommand(() => Minutes--);
            IncrementSecondsCommand = new RelayCommand(() => Seconds++);
            DecrementSecondsCommand = new RelayCommand(() => Seconds--);
        }

        public void SetFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value.Split(':');
            if (parts.Length < 2) return;

            int h, m, s = 0;
            if (!int.TryParse(parts[0], out h)) return;
            if (!int.TryParse(parts[1], out m)) return;
            if (parts.Length >= 3) int.TryParse(parts[2], out s);

            _hours = ((h % 24) + 24) % 24;
            _minutes = ((m % 60) + 60) % 60;
            _seconds = ((s % 60) + 60) % 60;

            OnPropertyChanged(nameof(Hours));
            OnPropertyChanged(nameof(Minutes));
            OnPropertyChanged(nameof(Seconds));
            OnPropertyChanged(nameof(HoursDisplay));
            OnPropertyChanged(nameof(MinutesDisplay));
            OnPropertyChanged(nameof(SecondsDisplay));
            OnPropertyChanged(nameof(SelectedTime));
        }

        public void SetFromTimeSpan(TimeSpan value)
        {
            Hours = value.Hours;
            Minutes = value.Minutes;
            Seconds = value.Seconds;
        }

        public TimeSpan ToTimeSpan() => new TimeSpan(_hours, _minutes, _seconds);
    }
}