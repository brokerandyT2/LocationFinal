using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    /// <summary>
    /// Represents a command to create a new tip with specified details.
    /// </summary>
    /// <remarks>This command is used to create a new tip by providing its type, title, content, and optional
    /// metadata such as camera settings (e.g., F-stop, shutter speed, ISO) and localization information.</remarks>
    public class CreateTipCommand : IRequest<Result<List<TipDto>>>
    {
        public int TipTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Fstop { get; set; } = string.Empty;
        public string ShutterSpeed { get; set; } = string.Empty;
        public string Iso { get; set; } = string.Empty;
        public string I8n { get; set; } = "en-US";
    }
    /// <summary>
    /// Represents the response returned after creating a tip command.
    /// </summary>
    /// <remarks>This class contains details about the created tip, including its identifier, type, title,
    /// content,  and associated photographic settings such as f-stop, shutter speed, and ISO.  It also includes
    /// localization information.</remarks>
    public class CreateTipCommandResponse
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