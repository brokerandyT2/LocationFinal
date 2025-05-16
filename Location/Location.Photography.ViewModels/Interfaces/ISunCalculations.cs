// Location.Photography.ViewModels/Interfaces/ISunCalculations.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Location.Photography.ViewModels.Interfaces
{
    public interface ISunCalculations
    {
        // Properties and method signatures...
        // Omitted for brevity but remains the same as before

        event EventHandler<Location.Photography.ViewModels.Events.OperationErrorEventArgs> ErrorOccurred;

        void CalculateSun();
        Task LoadLocationsAsync();
    }
}