// Location.Photography.ViewModels/Events/OperationErrorEventArgs.cs
using System;

namespace Location.Photography.ViewModels.Events
{
    public class OperationErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public OperationErrorSource Source { get; }
        public Exception Exception { get; }

        public OperationErrorEventArgs(OperationErrorSource source, string message, Exception exception = null)
        {
            Source = source;
            Message = message;
            Exception = exception;
        }
    }

    public enum OperationErrorSource
    {
        Unknown,
        Database,
        Network,
        Validation,
        MediaService,
        LocationService,
        WeatherService
    }
}