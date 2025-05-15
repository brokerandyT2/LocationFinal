using System.Collections.Generic;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Queries.GetAllTips
{
    public class GetAllTipsQuery : IRequest<Result<List<GetAllTipsQueryResponse>>>
    {
    }

    public class GetAllTipsQueryResponse
    {
        public int Id { get; set; }
        public int TipTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Fstop { get; set; } = string.Empty;
        public string ShutterSpeed { get; set; } = string.Empty;
        public string Iso { get; set; } = string.Empty;
        public string I8n { get; set; } = "en-US";
    }
}