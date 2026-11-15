using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NphiesSandbox.Api.Domain;
using NphiesSandbox.Api.Validators;

namespace NphiesSandbox.Api.Services;

public interface IFhirValidationService
{
    (NphiesValidationReport Report, Bundle? ParsedBundle) ValidateBundle(string rawJson);
}

public class FhirValidationService : IFhirValidationService
{
    private readonly FhirJsonParser _parser = new(new ParserSettings
    {
        AcceptUnknownMembers = false,
        AllowUnrecognizedEnums = false
    });

    public (NphiesValidationReport Report, Bundle? ParsedBundle) ValidateBundle(string rawJson)
    {
        var issues = new List<ValidationIssueDto>();
        Bundle bundle;

        try
        {
            bundle = _parser.Parse<Bundle>(rawJson);
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssueDto("fatal", "structure", "Bundle", $"FHIR R4 Parse Error: {ex.Message}"));
            return (new NphiesValidationReport(false, "unknown", "unknown", issues), null);
        }

        if (bundle.Type != Bundle.BundleType.Message)
            issues.Add(new ValidationIssueDto("error", "value", "Bundle.type", "NPHIES bundles must have Bundle.type = 'message'."));

        var header = bundle.Entry.FirstOrDefault()?.Resource as MessageHeader;
        if (header is null)
        {
            issues.Add(new ValidationIssueDto("error", "required", "Bundle.entry[0]", "Bundle must start with a MessageHeader resource."));
            return (new NphiesValidationReport(false, bundle.Id ?? "unknown", "missing-header", issues), bundle);
        }

        var eventCode = (header.Event as Coding)?.Code ?? "unknown";
        var resources = bundle.Entry.Select(e => e.Resource).Where(r => r is not null).ToList();

        var patient = resources.OfType<Patient>().FirstOrDefault();
        var provider = resources.OfType<Organization>()
            .FirstOrDefault(o => o.Identifier.Any(i => i.System == NphiesConstants.ProviderLicenseSystem));
        var insurer = resources.OfType<Organization>()
            .FirstOrDefault(o => o.Identifier.Any(i => i.System == NphiesConstants.PayerLicenseSystem));

        switch (eventCode)
        {
            case NphiesConstants.ClaimRequestEvent:
                var claim = resources.OfType<Claim>().FirstOrDefault();
                if (claim is null)
                    issues.Add(new ValidationIssueDto("error", "required", "Bundle.entry", "Missing Claim resource for claim-request."));
                else
                    issues.AddRange(new ClaimResourceValidator(patient, provider).Validate(claim).Errors
                        .Select(e => new ValidationIssueDto("error", "business-rule", $"Claim.{e.PropertyName}", e.ErrorMessage)));
                break;

            case NphiesConstants.EligibilityRequestEvent:
                var eligibility = resources.OfType<CoverageEligibilityRequest>().FirstOrDefault();
                if (eligibility is null)
                    issues.Add(new ValidationIssueDto("error", "required", "Bundle.entry", "Missing CoverageEligibilityRequest resource."));
                else
                    issues.AddRange(new EligibilityResourceValidator(patient, provider, insurer).Validate(eligibility).Errors
                        .Select(e => new ValidationIssueDto("error", "business-rule", $"CoverageEligibilityRequest.{e.PropertyName}", e.ErrorMessage)));
                break;

            case NphiesConstants.CommunicationEvent:
                var comm = resources.OfType<Communication>().FirstOrDefault();
                if (comm is null)
                    issues.Add(new ValidationIssueDto("error", "required", "Bundle.entry", "Missing Communication resource."));
                else
                    issues.AddRange(new CommunicationResourceValidator().Validate(comm).Errors
                        .Select(e => new ValidationIssueDto("error", "business-rule", $"Communication.{e.PropertyName}", e.ErrorMessage)));
                break;

            default:
                issues.Add(new ValidationIssueDto("error", "code-invalid", "MessageHeader.eventCoding.code", $"Unsupported NPHIES event '{eventCode}'."));
                break;
        }

        return (new NphiesValidationReport(issues.Count == 0, bundle.Id ?? Guid.NewGuid().ToString(), eventCode, issues), bundle);
    }
}
