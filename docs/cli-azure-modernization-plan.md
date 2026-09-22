# CLI Azure SDK modernization plan

## Goal

Modernize `src/Certes.Cli/` Azure integrations to supported SDKs while keeping
current command behavior stable for existing users.

This document is a migration plan, not an implementation PR.

## Current state inventory

The CLI currently depends on legacy Fluent management SDKs in
`src/Certes.Cli/Certes.Cli.csproj`:

- `Microsoft.Azure.Management.Dns.Fluent` 1.38.1
- `Microsoft.Azure.Management.AppService.Fluent` 1.38.1

Current command/auth touchpoints:

- `src/Certes.Cli/Commands/AzureCommandBase.cs`
  - Builds `RestClient` + `AzureCredentials` from service principal inputs.
  - Requires `--tenant-id`, `--client-id`, `--client-secret`,
    `--subscription-id`.
- `src/Certes.Cli/Commands/AzureSetCommand.cs`
  - Validates credentials by listing resource groups.
- `src/Certes.Cli/Commands/AzureDnsCommand.cs`
  - Finds DNS zone and creates TXT record sets for ACME DNS-01.
- `src/Certes.Cli/Commands/AzureAppCommand.cs`
  - Uploads certificate to App Service and binds hostname (optionally slot).
- `src/Certes.Cli/Program.cs`
  - Registers Fluent management clients in DI.

Related tests:

- `test/Certes.Tests/Cli/Commands/AzureSetCommandTests.cs`
- `test/Certes.Tests/Cli/Commands/AzureDnsCommandTests.cs`
- `test/Certes.Tests/Cli/Commands/AzureAppCommandTests.cs`

## Target SDK direction

Preferred modern stack:

- `Azure.Identity` for authentication.
- Azure Resource Manager SDK (`Azure.ResourceManager.*`) for management
  operations.

Expected service mappings:

- DNS: legacy `IDnsManagementClient` -> ARM DNS resources
  (`Azure.ResourceManager.Dns`).
- App Service certificate + hostname binding: legacy
  `IWebSiteManagementClient` -> ARM App Service resources
  (`Azure.ResourceManager.AppService`).
- Resource groups validation in `az set`: legacy
  `IResourceManagementClient` -> ARM resource groups collection.

If a required App Service operation is missing from the ARM surface, use a
small compatibility adapter around the newest supported management package for
that operation only.

## Compatibility requirements

Must preserve in the first migration cut:

- Existing commands and arguments:
  - `certes az set`
  - `certes az dns`
  - `certes az app`
  - `--tenant-id`, `--client-id`, `--client-secret`, `--subscription-id`,
    `--resource-group`, `--slot`
- Existing settings model in `src/Certes.Cli/Settings/AzureSettings.cs`.
- Existing JSON output shape consumed by scripts (or document any intentional
  change as breaking).

May be added in a later, separate PR:

- Managed identity / workload identity / `DefaultAzureCredential` fallback.
- Sovereign cloud selection flags.

## Phased implementation plan

1. Introduce an internal Azure abstraction layer for CLI commands.
   - Define narrow interfaces for:
     - validating subscription/resource groups
     - creating/updating DNS TXT records
     - uploading App Service cert + binding hostname
2. Add ARM-based implementation beside Fluent implementation.
   - Keep commands unchanged; swap implementation via DI in `Program.cs`.
3. Preserve command behavior with existing tests.
   - Update command tests to target abstractions rather than Fluent types.
4. Remove Fluent package references after behavior parity is proven.
5. Add optional auth enhancements in follow-up PR(s).

## Verification plan

For migration PRs (implementation phase):

- Unit tests:
  - `dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true --filter FullyQualifiedName~Azure`
  - `dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true`
- CLI build:
  - `dotnet build src/Certes.Cli/Certes.Cli.csproj`
- Dependency audit:
  - `dotnet list src/Certes.Cli/Certes.Cli.csproj package --vulnerable --include-transitive --no-restore`
  - `dotnet list src/Certes.Cli/Certes.Cli.csproj package --deprecated --include-transitive --no-restore`

Manual smoke checks (non-secret, local):

- `certes --help`
- `certes az --help`
- `certes az dns --help`
- `certes az app --help`

## Risks and mitigations

- Auth behavior drift (token acquisition, subscription resolution).
  - Mitigation: preserve explicit service-principal flow first; add new auth
    modes later.
- Output or error-message drift in scripts.
  - Mitigation: snapshot/approval-style assertions for command output in CLI
    command tests.
- API surface mismatch for App Service bindings.
  - Mitigation: isolate service-specific adapter and keep fallback package use
    minimal and explicit.

## Out of scope for this planning PR

- No package upgrades are performed here.
- No CLI behavior changes are implemented here.
- No new credentials or cloud-environment flags are introduced here.
