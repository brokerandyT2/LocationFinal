using MediatR;

namespace Location.Core.ViewModels
{
    public class ViewModelErrorHandler : INotification
    {
        [Flags]
        public enum DisplayOptions
        {
            None = 0,
            ShowOk = 1,
            ShowCancel = 2
        }
        [Flags]
        public enum HandlerOptions
        {
            None = 0,
            ShowAlert = 1,
            LogError = 2,
            ShowOk = 4,
            ShowCancel = 8
        }

        public HandlerOptions EHOptions { get; set; } = HandlerOptions.ShowAlert | HandlerOptions.LogError;
        public DisplayOptions DspOptions { get; set; } = DisplayOptions.ShowOk | DisplayOptions.ShowCancel;
        public string Title { get; set; } = "Error";
        public string Message { get; set; } = "An error occurred. Please try again later.";

    }
}
