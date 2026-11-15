using FluentValidation;
using Hl7.Fhir.Model;

namespace NphiesSandbox.Api.Validators;

public class CommunicationResourceValidator : AbstractValidator<Communication>
{
    public CommunicationResourceValidator()
    {
        RuleFor(c => c.Status)
            .NotNull()
            .WithMessage("Communication.status is required.");

        RuleFor(c => c.Subject)
            .NotNull()
            .Must(s => !string.IsNullOrWhiteSpace(s?.Reference))
            .WithMessage("Communication.subject must reference a Patient.");

        RuleFor(c => c.Sender)
            .NotNull()
            .WithMessage("Communication.sender Organization reference is required.");

        RuleFor(c => c.Recipient)
            .NotEmpty()
            .WithMessage("Communication.recipient must include at least one target Organization.");

        RuleFor(c => c.Payload)
            .NotEmpty()
            .WithMessage("Communication.payload must contain at least one attachment or message string.");
    }
}
