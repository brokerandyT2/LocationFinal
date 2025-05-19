using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
namespace Location.Core.Application.Commands.TipTypes
{
    /// <summary>
    /// Represents a command to create a new tip type with the specified name and localization.
    /// </summary>
    /// <remarks>This command is used to initiate the creation of a tip type in the system.  The result of the
    /// operation is returned as a <see cref="Result{T}"/> containing a <see cref="TipTypeDto"/> object  if the creation
    /// is successful.</remarks>
    public class CreateTipTypeCommand : IRequest<Result<TipTypeDto>>
    {
        public string Name { get; set; } = string.Empty;
        public string I8n { get; set; } = "en-US";
    }
    /// <summary>
    /// Handles the creation of a new tip type by processing the <see cref="CreateTipTypeCommand"/> request.
    /// </summary>
    /// <remarks>This handler creates a new tip type entity, sets its localization data, and persists it to
    /// the repository. If the operation is successful, a <see cref="TipTypeDto"/> representing the created tip type is
    /// returned. In case of failure, an error message is included in the result.</remarks>
    public class CreateTipTypeCommandHandler : IRequestHandler<CreateTipTypeCommand, Result<TipTypeDto>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;
        /// <summary>
        /// Handles the creation of new tip types by processing the associated command.
        /// </summary>
        /// <param name="tipTypeRepository">The repository used to persist and manage tip type data. This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipTypeRepository"/> is <see langword="null"/>.</exception>
        public CreateTipTypeCommandHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository ?? throw new ArgumentNullException(nameof(tipTypeRepository));
        }
        /// <summary>
        /// Handles the creation of a new tip type and returns the result.
        /// </summary>
        /// <remarks>This method creates a new tip type entity, persists it to the repository, and maps it
        /// to a data transfer object (DTO). If an error occurs during the process, the method returns a failure result
        /// with the error message.</remarks>
        /// <param name="request">The command containing the details of the tip type to create, including its name and localization data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="TipTypeDto"/> if the operation succeeds,  or an error
        /// message if the operation fails.</returns>
        public async Task<Result<TipTypeDto>> Handle(CreateTipTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tipType = new Domain.Entities.TipType(request.Name);
                tipType.SetLocalization(request.I8n);

                var createdTipType = await _tipTypeRepository.AddAsync(tipType, cancellationToken);

                var tipTypeDto = new TipTypeDto
                {
                    Id = createdTipType.Id,
                    Name = createdTipType.Name,
                    I8n = createdTipType.I8n
                };

                return Result<TipTypeDto>.Success(tipTypeDto);
            }
            catch (Exception ex)
            {
                return Result<TipTypeDto>.Failure($"Failed to create tip type: {ex.Message}");
            }
        }
    }
}