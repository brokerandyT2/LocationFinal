using System;
using System.Collections.Generic;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Queries.GetAllSettings
{
    public class GetAllSettingsQuery : IRequest<Result<List<GetAllSettingsQueryResponse>>>
    {
    }

    public class GetAllSettingsQueryResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}