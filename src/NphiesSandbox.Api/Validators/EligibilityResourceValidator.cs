using FluentValidation;
using Hl7.Fhir.Model;
using NphiesSandbox.Api.Domain;

namespace NphiesSandbox.Api.Validators;

public class EligibilityResourceValidator : AbstractValidator<CoverageEligibilityRequest>
{
    public EligibilityResourceValidator(Patient? patient, Organization? provider, Organization? insurer)
    {
        RuleFor(e => e.Status)
            .Equal(FinancialResourceStatusCodes.Active)
            .WithMessage("CoverageEligibilityRequest.status must be 'active'.");

        RuleFor(e => e.Purpose)
            .NotEmpty()
            .WithMessage("CoverageEligibilityRequest.purpose must specify at least one purpose (e.g., validation, benefits).");

        RuleFor(_ => patient)
            .NotNull().WithMessage("Referenced Patient resource must be present in the Bundle.")
            .DependentRules(() =>
            {
                RuleFor(p => patient!.Identifier)
                    .Must(ClaimResourceValidator.HasValidSaudiId)
                    .WithMessage("Patient must have a valid 10-digit Saudi National ID (1xxxxxxxxx) or Iqama (2xxxxxxxxx).");
            });

        RuleFor(_ => provider)
            .NotNull().WithMessage("Provider Organization with provider-license must be present in the Bundle.");

        RuleFor(_ => insurer)
            .NotNull().WithMessage("Insurer Organization with payer-license must be present in the Bundle.");
    }
}
