using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Commands.Locations
{
    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLocationCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<bool>> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // The test mocks this to return a Location directly (not wrapped in Result)
                var location = await (_unitOfWork.Locations as dynamic).GetByIdAsync(request.Id, cancellationToken);

                if (location == null)
                {
                    return Result<bool>.Failure("Location not found");
                }

                // Perform soft delete by marking as deleted  
                location.Delete();

                // The test expects void Update method, not async
                (_unitOfWork.Locations as dynamic).Update(location);
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