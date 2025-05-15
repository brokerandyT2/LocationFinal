using System;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Queries.GetSettingByKey
{
    public class GetSettingByKeyQuery : IRequest<Result<GetSettingByKeyQueryResponse>>
    {
        public string Key { get; set; } = string.Empty;
    }

    public class GetSettingByKeyQueryResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}