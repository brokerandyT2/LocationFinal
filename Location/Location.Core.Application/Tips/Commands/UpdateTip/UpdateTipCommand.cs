using System;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    /// <summary>
    /// Represents a command to update an existing tip with new details.
    /// </summary>
    /// <remarks>This command is used to modify the properties of an existing tip, such as its title, content,
    /// and associated metadata. The command includes fields for specifying the tip's type, localized  language, and
    /// photographic settings such as f-stop, shutter speed, and ISO.</remarks>
    public class UpdateTipCommand : IRequest<Result<UpdateTipCommandResponse>>
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
    /// <summary>
    /// Represents the response returned after updating a tip in the system.
    /// </summary>
    /// <remarks>This class contains the updated details of a tip, including its identifier, type, title,
    /// content,  and associated photographic settings such as f-stop, shutter speed, and ISO. It also includes 
    /// localization information.</remarks>
    public class UpdateTipCommandResponse
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