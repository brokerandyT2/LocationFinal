// Location.Core.ViewModels/TipsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Core.Application.Tips.Queries.GetAllTipTypes;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using MediatR;

namespace Location.Core.ViewModels
{
    public partial class TipsViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;

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

        public TipsViewModel(IMediator mediator,IAlertService alertingService) : base(alertingService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [RelayCommand]
        private async Task LoadTipTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsBusy = true;
                IsError = false;
                ErrorMessage = string.Empty;

                // Clear existing items
                TipTypes.Clear();

                // Get tip types using MediatR query
                var query = new GetAllTipTypesQuery();
                var result = await _mediator.Send(query, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        TipTypes.Add(new TipTypeItemViewModel
                        {
                            Id = item.Id,
                            Name = item.Name,
                            I8n = item.I8n
                        });
                    }

                    // Set default selected tip type if available
                    if (TipTypes.Count > 0)
                    {
                        SelectedTipType = TipTypes.First();
                        SelectedTipTypeId = SelectedTipType.Id;
                        await LoadTipsByTypeAsync(SelectedTipTypeId, cancellationToken);
                    }
                }
                else
                {
                    IsError = true;
                    ErrorMessage = result.ErrorMessage ?? "Failed to load tip types";
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = $"Error loading tip types: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadTipsByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (tipTypeId <= 0)
                    return;

                IsBusy = true;
                IsError = false;
                ErrorMessage = string.Empty;

                // Clear existing items
                Tips.Clear();

                // Get tips by type using MediatR query
                var query = new GetTipsByTypeQuery { TipTypeId = tipTypeId };
                var result = await _mediator.Send(query, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        Tips.Add(new TipItemViewModel
                        {
                            Id = item.Id,
                            TipTypeId = item.TipTypeId,
                            Title = item.Title,
                            Content = item.Content,
                            Fstop = item.Fstop,
                            ShutterSpeed = item.ShutterSpeed,
                            Iso = item.Iso,
                            I8n = item.I8n
                        });
                    }
                }
                else
                {
                    IsError = true;
                    ErrorMessage = result.ErrorMessage ?? "Failed to load tips";
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = $"Error loading tips: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedTipTypeChanged(TipTypeItemViewModel? value)
        {
            if (value != null)
            {
                SelectedTipTypeId = value.Id;
                LoadTipsByTypeCommand.Execute(value.Id);
            }
        }
    }

    // Make these classes public to match the accessibility of the properties that use them
    public class TipItemViewModel : ObservableObject
    {
        private int _id;
        private int _tipTypeId;
        private string _title = string.Empty;
        private string _content = string.Empty;
        private string _fstop = string.Empty;
        private string _shutterSpeed = string.Empty;
        private string _iso = string.Empty;
        private string _i8n = "en-US";

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int TipTypeId
        {
            get => _tipTypeId;
            set => SetProperty(ref _tipTypeId, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string Fstop
        {
            get => _fstop;
            set => SetProperty(ref _fstop, value);
        }

        public string ShutterSpeed
        {
            get => _shutterSpeed;
            set => SetProperty(ref _shutterSpeed, value);
        }

        public string Iso
        {
            get => _iso;
            set => SetProperty(ref _iso, value);
        }

        public string I8n
        {
            get => _i8n;
            set => SetProperty(ref _i8n, value);
        }

        public bool HasCameraSettings => !string.IsNullOrEmpty(Fstop) || !string.IsNullOrEmpty(ShutterSpeed) || !string.IsNullOrEmpty(Iso);

        public string CameraSettingsDisplay =>
            $"{(string.IsNullOrEmpty(Fstop) ? "" : $"F{Fstop} ")}" +
            $"{(string.IsNullOrEmpty(ShutterSpeed) ? "" : $"{ShutterSpeed} ")}" +
            $"{(string.IsNullOrEmpty(Iso) ? "" : $"ISO {Iso}")}".Trim();
    }

    public class TipTypeItemViewModel : ObservableObject
    {
        private int _id;
        private string _name = string.Empty;
        private string _i8n = "en-US";

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string I8n
        {
            get => _i8n;
            set => SetProperty(ref _i8n, value);
        }
    }
}