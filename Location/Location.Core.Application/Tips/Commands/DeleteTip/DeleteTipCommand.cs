using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    public class DeleteTipCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }
}