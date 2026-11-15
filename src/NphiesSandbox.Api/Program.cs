using NphiesSandbox.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "NPHIES FHIR R4 Claims Sandbox & Validator",
        Version = "v1",
        Description = "Validates Saudi NPHIES FHIR R4 bundles and simulates payer adjudication."
    });
});

builder.Services.AddSingleton<IFhirValidationService, FhirValidationService>();
builder.Services.AddSingleton<PayerAdjudicationSimulator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/v1/nphies/validate", async (HttpRequest request, IFhirValidationService validator) =>
{
    using var reader = new StreamReader(request.Body);
    var rawJson = await reader.ReadToEndAsync();
    var (report, _) = validator.ValidateBundle(rawJson);

    return report.IsValid ? Results.Ok(report) : Results.UnprocessableEntity(report);
});

app.MapPost("/api/v1/nphies/$process-message", async (
    HttpRequest request,
    IFhirValidationService validator,
    PayerAdjudicationSimulator simulator) =>
{
    using var reader = new StreamReader(request.Body);
    var rawJson = await reader.ReadToEndAsync();
    var (report, bundle) = validator.ValidateBundle(rawJson);
    var responseJson = simulator.BuildResponseBundle(report, bundle);

    return Results.Content(responseJson, "application/fhir+json", statusCode: report.IsValid ? 200 : 422);
});

app.Run();

public partial class Program { }
