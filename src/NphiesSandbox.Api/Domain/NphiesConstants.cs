namespace NphiesSandbox.Api.Domain;

public static class NphiesConstants
{
    public const string BaseCanonical = "http://nphies.sa/fhir/ksa/nphies-fs";

    // KSA Identifiers & Licenses
    public const string SaudiNationalIdSystem = "http://nphies.sa/identifier/nationalid";
    public const string IqamaSystem = "http://nphies.sa/identifier/iqama";
    public const string ProviderLicenseSystem = "http://nphies.sa/license/provider-license";
    public const string PayerLicenseSystem = "http://nphies.sa/license/payer-license";

    // Mandatory NPHIES Extensions
    public const string EpisodeIdExtension = $"{BaseCanonical}/StructureDefinition/extension-episode";
    public const string PatientInvoiceExtension = $"{BaseCanonical}/StructureDefinition/extension-patientInvoice";
    public const string EligibilityOfflineRefExtension = $"{BaseCanonical}/StructureDefinition/extension-eligibility-offline-reference";

    // MessageHeader Event Codes
    public const string ClaimRequestEvent = "claim-request";
    public const string EligibilityRequestEvent = "eligibility-request";
    public const string CommunicationEvent = "communication";
}
