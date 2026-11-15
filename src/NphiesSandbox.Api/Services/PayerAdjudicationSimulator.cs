using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NphiesSandbox.Api.Domain;

namespace NphiesSandbox.Api.Services;

public class PayerAdjudicationSimulator
{
    private readonly FhirJsonSerializer _serializer = new(new SerializerSettings { Pretty = true });

    public string BuildResponseBundle(NphiesValidationReport report, Bundle? requestBundle)
    {
        if (!report.IsValid || requestBundle is null)
        {
            var outcome = new OperationOutcome
            {
                Id = Guid.NewGuid().ToString(),
                Issue = report.Issues.Select(i => new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.BusinessRule,
                    Diagnostics = i.Diagnostics,
                    Location = [i.Location]
                }).ToList()
            };
            return _serializer.SerializeToString(outcome);
        }

        var responseHeader = new MessageHeader
        {
            Id = $"hdr-{Guid.NewGuid():N}",
            Event = new Coding("http://nphies.sa/terminology/CodeSystem/ksa-message-events", $"{report.MessageEvent}-response"),
            Source = new MessageHeader.MessageSourceComponent { Endpoint = "http://nphies.sa/sandbox/payer" }
        };

        Resource payloadResource = report.MessageEvent switch
        {
            NphiesConstants.ClaimRequestEvent => BuildClaimResponse(requestBundle.Entry.Select(e => e.Resource).OfType<Claim>().First()),
            NphiesConstants.EligibilityRequestEvent => BuildEligibilityResponse(requestBundle.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().First()),
            _ => new OperationOutcome
            {
                Id = $"ack-{Guid.NewGuid():N}",
                Issue = [new OperationOutcome.IssueComponent { Severity = OperationOutcome.IssueSeverity.Information, Code = OperationOutcome.IssueType.Informational, Diagnostics = "Communication acknowledged by NPHIES Sandbox." }]
            }
        };

        var responseBundle = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Type = Bundle.BundleType.Message,
            Timestamp = DateTimeOffset.UtcNow,
            Entry =
            [
                new Bundle.EntryComponent { FullUrl = $"urn:uuid:{responseHeader.Id}", Resource = responseHeader },
                new Bundle.EntryComponent { FullUrl = $"urn:uuid:{payloadResource.Id}", Resource = payloadResource }
            ]
        };

        return _serializer.SerializeToString(responseBundle);
    }

    private static ClaimResponse BuildClaimResponse(Claim claim)
    {
        var response = new ClaimResponse
        {
            Id = $"resp-{Guid.NewGuid():N}",
            Status = FinancialResourceStatusCodes.Active,
            Type = claim.Type,
            Use = ClaimUseCode.Claim,
            Patient = claim.Patient,
            Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Insurer = new ResourceReference { Display = "Simulated KSA Payer" },
            Request = new ResourceReference($"Claim/{claim.Id}"),
            Outcome = ClaimProcessingCodes.Complete,
            Disposition = "Adjudicated by NPHIES Sandbox Simulator."
        };

        decimal totalBenefit = 0m;
        foreach (var item in claim.Item)
        {
            var net = item.Net?.Value ?? 0m;
            var approved = net <= 5000m ? net : Math.Round(net * 0.80m, 2);
            totalBenefit += approved;

            response.AddItem.Add(new ClaimResponse.AddedItemComponent
            {
                ProductOrService = item.ProductOrService,
                Adjudication =
                [
                    new ClaimResponse.AdjudicationComponent
                    {
                        Category = new CodeableConcept("http://terminology.hl7.org/CodeSystem/adjudication", "benefit"),
                        Amount = new Money { Value = approved, Currency = Money.Currencies.SAR }
                    }
                ]
            });
        }

        response.Total.Add(new ClaimResponse.TotalComponent
        {
            Category = new CodeableConcept("http://terminology.hl7.org/CodeSystem/adjudication", "benefit"),
            Amount = new Money { Value = totalBenefit, Currency = Money.Currencies.SAR }
        });

        return response;
    }

    private static CoverageEligibilityResponse BuildEligibilityResponse(CoverageEligibilityRequest req) => new()
    {
        Id = $"elig-resp-{Guid.NewGuid():N}",
        Status = FinancialResourceStatusCodes.Active,
        Purpose = req.Purpose
            .Select(p => p.HasValue ? (CoverageEligibilityResponse.EligibilityResponsePurpose?)(int)p.Value : null)
            .ToList(),
        Patient = req.Patient,
        Created = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
        Request = new ResourceReference($"CoverageEligibilityRequest/{req.Id}"),
        Outcome = ClaimProcessingCodes.Complete,
        Insurer = req.Insurer,
        Insurance = [new CoverageEligibilityResponse.InsuranceComponent { Coverage = new ResourceReference("Coverage/cov-1"), Inforce = true }]
    };
}
