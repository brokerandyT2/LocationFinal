// Location.Photography.ViewModels/Events/OperationErrorEventArgs.cs
namespace Location.Photography.ViewModels.Events
{
    public class OperationErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }
        public OperationErrorSource Source { get; }

        public OperationErrorEventArgs(OperationErrorSource source, string message, Exception? exception = null)
        {
            Source = source;
            Message = message;
            Exception = exception;
        }

        public OperationErrorEventArgs(string message) : this(OperationErrorSource.Unknown, message, null)
        {
        }
    }

    public enum OperationErrorSource
    {
        Unknown,
        Validation,
        Database,
        Network,
        Sensor,
        Permission,
        Device,
        MediaService,
        Navigation,
        Calculation
    }
}