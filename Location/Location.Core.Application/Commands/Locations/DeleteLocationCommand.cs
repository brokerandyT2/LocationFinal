using Location.Core.Application.Common;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Interfaces;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    public class DeleteLocationCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }

    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILocationRepository _locationRepository;

        public DeleteLocationCommandHandler(
            IUnitOfWork unitOfWork,
            ILocationRepository locationRepository)
        {
            _unitOfWork = unitOfWork;
            _locationRepository = locationRepository;
        }

        public async Task<Result<bool>> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);
                if (location == null)
                {
                    return Result<bool>.Failure("Location not found");
                }

                // Soft delete as per memory (locations can't be deleted, just marked as deleted)
                location.Delete();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete location: {ex.Message}");
            }
        }
    }
}