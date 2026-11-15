using FluentValidation;
using Hl7.Fhir.Model;
using NphiesSandbox.Api.Domain;

namespace NphiesSandbox.Api.Validators;

public class ClaimResourceValidator : AbstractValidator<Claim>
{
    public ClaimResourceValidator(Patient? patient, Organization? provider)
    {
        RuleFor(c => c.Status)
            .Equal(FinancialResourceStatusCodes.Active)
            .WithMessage("Claim.status must be 'active'.");

        RuleFor(c => c.Type)
            .NotNull()
            .Must(t => t?.Coding?.Any(c =>
                c.Code is "institutional" or "oral" or "pharmacy" or "professional" or "vision") == true)
            .WithMessage("Claim.type must be institutional, professional, pharmacy, oral, or vision.");

        RuleFor(c => c.Extension)
            .Must(ext => ext.Any(e => e.Url == NphiesConstants.EpisodeIdExtension))
            .WithMessage($"Missing mandatory NPHIES extension: {NphiesConstants.EpisodeIdExtension}");

        RuleFor(_ => patient)
            .NotNull().WithMessage("Referenced Patient resource must be present in the Bundle.")
            .DependentRules(() =>
            {
                RuleFor(p => patient!.Identifier)
                    .Must(HasValidSaudiId)
                    .WithMessage("Patient must have a valid 10-digit Saudi National ID (1xxxxxxxxx) or Iqama (2xxxxxxxxx).");
            });

        RuleFor(_ => provider)
            .NotNull().WithMessage("Referenced Provider Organization must be present in the Bundle.")
            .DependentRules(() =>
            {
                RuleFor(o => provider!.Identifier)
                    .Must(ids => ids.Any(i => i.System == NphiesConstants.ProviderLicenseSystem && !string.IsNullOrWhiteSpace(i.Value)))
                    .WithMessage($"Provider Organization requires identifier system '{NphiesConstants.ProviderLicenseSystem}'.");
            });

        RuleFor(c => c.Item)
            .NotEmpty().WithMessage("Claim must contain at least one service line item.");

        RuleForEach(c => c.Item).ChildRules(item =>
        {
            item.RuleFor(i => i.Sequence).GreaterThan(0);
            item.RuleFor(i => i.Extension)
                .Must(ext => ext.Any(e => e.Url == NphiesConstants.PatientInvoiceExtension))
                .WithMessage($"Each Claim.item requires mandatory extension: {NphiesConstants.PatientInvoiceExtension}");
        });
    }

    internal static bool HasValidSaudiId(List<Identifier> identifiers)
    {
        var id = identifiers.FirstOrDefault(i =>
            i.System is NphiesConstants.SaudiNationalIdSystem or NphiesConstants.IqamaSystem);

        if (id is null || string.IsNullOrWhiteSpace(id.Value) || id.Value.Length != 10 || !id.Value.All(char.IsDigit))
            return false;

        return (id.System == NphiesConstants.SaudiNationalIdSystem && id.Value.StartsWith('1')) ||
               (id.System == NphiesConstants.IqamaSystem && id.Value.StartsWith('2'));
    }
}
