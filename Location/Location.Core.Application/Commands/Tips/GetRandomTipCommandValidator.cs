using FluentValidation;

namespace Location.Core.Application.Commands.Tips
{
    /// <summary>
    /// Validates the properties of a <see cref="GetRandomTipCommand"/> instance.
    /// </summary>
    /// <remarks>This validator ensures that the <c>TipTypeId</c> property of the command meets the required
    /// constraints.</remarks>
    public class GetRandomTipCommandValidator : AbstractValidator<GetRandomTipCommand>
    {
        /// <summary>
        /// Validates the properties of a command to retrieve a random tip.
        /// </summary>
        /// <remarks>This validator ensures that the <c>TipTypeId</c> property of the command meets the
        /// required conditions. Specifically, it checks that the <c>TipTypeId</c> is greater than 0.</remarks>
        public GetRandomTipCommandValidator()
        {
            RuleFor(x => x.TipTypeId)
                .GreaterThan(0).WithMessage("TipTypeId must be greater than 0");
        }
    }
}