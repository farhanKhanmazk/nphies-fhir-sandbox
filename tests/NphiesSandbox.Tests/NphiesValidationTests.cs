using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using NphiesSandbox.Api.Services;
using Xunit;

namespace NphiesSandbox.Tests;

public class NphiesValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly FhirValidationService _validator = new();
    private readonly HttpClient _client;

    public NphiesValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProcessMessage_ValidClaimBundle_Returns200WithClaimResponse()
    {
        var validClaimJson = """
        {
          "resourceType": "Bundle",
          "id": "bundle-claim-01",
          "type": "message",
          "entry": [
            {
              "resource": {
                "resourceType": "MessageHeader",
                "eventCoding": { "code": "claim-request" },
                "source": { "endpoint": "http://hospital.sa/fhir" }
              }
            },
            {
              "resource": {
                "resourceType": "Patient",
                "id": "pat-1",
                "identifier": [{ "system": "http://nphies.sa/identifier/iqama", "value": "2456789012" }]
              }
            },
            {
              "resource": {
                "resourceType": "Organization",
                "id": "org-1",
                "identifier": [{ "system": "http://nphies.sa/license/provider-license", "value": "PRV-JED-101" }]
              }
            },
            {
              "resource": {
                "resourceType": "Claim",
                "id": "clm-1",
                "status": "active",
                "type": { "coding": [{ "code": "professional" }] },
                "use": "claim",
                "patient": { "reference": "Patient/pat-1" },
                "created": "2026-11-15",
                "provider": { "reference": "Organization/org-1" },
                "priority": { "coding": [{ "code": "normal" }] },
                "insurance": [{ "sequence": 1, "focal": true, "coverage": { "reference": "Coverage/cov-1" } }],
                "extension": [
                  { "url": "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/extension-episode", "valueIdentifier": { "value": "EP-100" } }
                ],
                "item": [
                  {
                    "sequence": 1,
                    "productOrService": { "coding": [{ "code": "99213" }] },
                    "net": { "value": 400.00, "currency": "SAR" },
                    "extension": [
                      { "url": "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/extension-patientInvoice", "valueIdentifier": { "value": "INV-100" } }
                    ]
                  }
                ]
              }
            }
          ]
        }
        """;

        var response = await _client.PostAsync(
            "/api/v1/nphies/$process-message",
            new StringContent(validClaimJson, Encoding.UTF8, "application/fhir+json"));

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"resourceType\": \"ClaimResponse\"", body);
    }

    [Fact]
    public void ValidateBundle_InvalidNineDigitIqama_FailsValidation()
    {
        var invalidIqamaJson = """
        {
          "resourceType": "Bundle",
          "type": "message",
          "entry": [
            { "resource": { "resourceType": "MessageHeader", "eventCoding": { "code": "claim-request" }, "source": { "endpoint": "http://hospital.sa" } } },
            { "resource": { "resourceType": "Patient", "identifier": [{ "system": "http://nphies.sa/identifier/iqama", "value": "24567890" }] } },
            { "resource": { "resourceType": "Claim", "status": "active", "type": { "coding": [{ "code": "professional" }] }, "use": "claim", "patient": { "reference": "Patient/1" }, "created": "2026-11-15", "provider": { "reference": "Organization/1" }, "priority": { "coding": [{ "code": "normal" }] }, "insurance": [{ "sequence": 1, "focal": true, "coverage": { "reference": "Coverage/1" } }] } }
          ]
        }
        """;

        var (report, _) = _validator.ValidateBundle(invalidIqamaJson);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.Diagnostics.Contains("10-digit"));
    }
}
