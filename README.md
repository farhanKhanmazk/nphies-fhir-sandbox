# NPHIES FHIR R4 Claims Sandbox & Validator (.NET 10)

NPHIES claim rejections cost KSA clinics and polyclinics millions in delayed revenue due to missing FHIR extensions and identifier mismatches.

This open-source microservice validates **HL7 FHIR R4** transaction bundles against Saudi Arabia's **NPHIES (`nphies-fs`)** rules prior to gateway submission and simulates payer adjudication responses (`ClaimResponse`, `CoverageEligibilityResponse`, and `OperationOutcome`).

## Live Demo & Interactive API Docs
* **Swagger UI:** *(We will add your live URL here in Step 3)*

## Architecture Flow (`$process-message`)

```mermaid
sequenceDiagram
    participant HIS as Hospital / Clinic HIS
    participant API as NPHIES Sandbox (.NET 10)
    participant Val as Firely SDK + FluentValidation
    participant Sim as Payer Adjudication Simulator

    HIS->>API: POST /api/v1/nphies/$process-message (FHIR Bundle)
    API->>Val: Parse JSON & Check KSA Rules (Iqama, Provider License, Extensions)
    alt Validation Fails
        Val-->>API: List of Validation Diagnostics
        API-->>HIS: 422 Unprocessable Entity (FHIR OperationOutcome)
    else Validation Passes
        Val-->>API: Validated FHIR Bundle
        API->>Sim: Adjudicate Line Items (SAR)
        Sim-->>API: Build Response Bundle
        API-->>HIS: 200 OK (FHIR ClaimResponse / EligibilityResponse)
    end
