namespace NphiesSandbox.Api.Domain;

public record ValidationIssueDto(
    string Severity,
    string Code,
    string Location,
    string Diagnostics);

public record NphiesValidationReport(
    bool IsValid,
    string BundleId,
    string MessageEvent,
    IReadOnlyList<ValidationIssueDto> Issues);
