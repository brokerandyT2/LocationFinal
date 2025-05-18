using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
namespace Location.Core.Application.Commands.TipTypes
{
    public class CreateTipTypeCommand : IRequest<Result<TipTypeDto>>
    {
        public string Name { get; set; } = string.Empty;
        public string I8n { get; set; } = "en-US";
    }

    public class CreateTipTypeCommandHandler : IRequestHandler<CreateTipTypeCommand, Result<TipTypeDto>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;

        public CreateTipTypeCommandHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository ?? throw new ArgumentNullException(nameof(tipTypeRepository));
        }

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