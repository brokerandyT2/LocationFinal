// Location.Core.ViewModels/LocationsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Locations.Queries.GetLocationById;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using MediatR;

namespace Location.Core.ViewModels
{
    public partial class LocationsViewModel : BaseViewModel
    {
        private readonly IMediator _mediator;

        public ObservableCollection<LocationListItemViewModel> Locations { get; } = new();

        public LocationsViewModel(IMediator mediator) : base(null)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public LocationsViewModel(IMediator mediator, IAlertService alertingService) : base(alertingService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [RelayCommand]
        private async Task LoadLocationsAsync(CancellationToken cancellationToken = default)
        {
            // Implementation remains the same
        }
    }

    // Make this class public to match the accessibility of the Locations property
    public partial class LocationListItemViewModel : ObservableObject
    {
        private int _id;
        private string _title = string.Empty;
        private double _latitude;
        private double _longitude;
        private string _photo = string.Empty;
        private bool _isDeleted;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public double Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        public double Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }

        public string Photo
        {
            get => _photo;
            set => SetProperty(ref _photo, value);
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public string FormattedCoordinates => $"{Latitude:F6}, {Longitude:F6}";
    }
}