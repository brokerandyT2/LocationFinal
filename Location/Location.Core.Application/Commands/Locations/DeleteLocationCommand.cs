using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    public class DeleteLocationCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }
}