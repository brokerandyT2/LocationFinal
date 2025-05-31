using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Location.Photography.ViewModels
{
    public class TimelineEventViewModel : INotifyPropertyChanged
    {
        private DateTime _eventTime;
        private string _eventName = string.Empty;
        private string _eventIcon = string.Empty;

        public DateTime EventTime
        {
            get => _eventTime;
            set
            {
                if (SetProperty(ref _eventTime, value))
                {
                    OnPropertyChanged(nameof(TimeText));
                    OnPropertyChanged(nameof(TimeFromNow));
                }
            }
        }

        public string EventName
        {
            get => _eventName;
            set => SetProperty(ref _eventName, value);
        }

        public string EventIcon
        {
            get => _eventIcon;
            set => SetProperty(ref _eventIcon, value);
        }

        public string TimeText => EventTime.ToString("HH:mm");

        public string TimeFromNow
        {
            get
            {
                var timeSpan = EventTime - DateTime.Now;
                if (timeSpan.TotalMinutes < 0)
                    return "Past";

                if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes}m";

                if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours}h {(int)timeSpan.Minutes}m";

                return $"{(int)timeSpan.TotalDays}d {(int)timeSpan.Hours}h";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}