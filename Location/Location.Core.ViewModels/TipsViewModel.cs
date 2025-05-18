// Location.Core.ViewModels/TipsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Application.Tips.Queries.GetAllTipTypes;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using MediatR;

namespace Location.Core.ViewModels
{
    public partial class TipsViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly ITipTypeRepository _tiptyperepo;
        private readonly ITipRepository _tiprepo;
        // Add the event
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;

        [ObservableProperty]
        private int _selectedTipTypeId;

        [ObservableProperty]
        private TipTypeItemViewModel? _selectedTipType;

        public ObservableCollection<TipItemViewModel> Tips { get; } = new();
        public ObservableCollection<TipTypeItemViewModel> TipTypes { get; } = new();

        public TipsViewModel(IMediator mediator) : base(null)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public TipsViewModel(IMediator mediator, IAlertService alertingService, ITipTypeRepository tipTypeRepository, ITipRepository tiprepo) : base(alertingService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _tiptyperepo = tipTypeRepository;
            _tiprepo = tiprepo;
        }

        [RelayCommand]
        private async Task LoadTipTypesAsync(CancellationToken cancellationToken = default)
        {
            // Keep your existing implementation
            // Just add error event trigger when errors occur:
            // OnErrorOccurred(ErrorMessage);
             await _tiptyperepo.GetAllAsync(cancellationToken);
            return;
        }

        [RelayCommand]
        private async Task LoadTipsByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            // Keep your existing implementation
            // Just add error event trigger when errors occur:
            // OnErrorOccurred(ErrorMessage);
           await _tiprepo.GetByTipTypeIdAsync(tipTypeId, cancellationToken);
            return;
        }

        partial void OnSelectedTipTypeChanged(TipTypeItemViewModel? value)
        {
            // Keep your existing implementation
        }

        // Add this method to raise the event
        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(message));
        }
    }

    // Keep your existing TipItemViewModel and TipTypeItemViewModel classes unchanged
    public class TipItemViewModel : ObservableObject
    {
        // Keep your existing implementation
    }

    public class TipTypeItemViewModel : ObservableObject
    {
        // Keep your existing implementation
    }
}