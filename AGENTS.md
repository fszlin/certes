# Development guide for coding agents

## Purpose and scope

Certes is an embeddable .NET ACME client library and a companion CLI. The project
is being revived incrementally. Preserve working behavior and compatibility
while making builds, tests, dependencies, and certificate workflows maintainable.

This file applies throughout the repository. Read it and the relevant code/tests
before editing. Treat roadmap items as context, not authorization to expand the
current task. Keep this guide accurate when commands, targets, or blockers change.

## Project map

| Path | Responsibility |
| --- | --- |
| `src/Certes/AcmeContext.cs`, `IAcmeContext.cs` | Public entry point and account/order operations |
| `src/Certes/Acme/` | HTTP transport, resource contexts, protocol models, error handling |
| `src/Certes/Extensions/` | Convenience APIs, order generation, certificate export |
| `src/Certes/Jws/`, `Crypto/`, `Json/` | Signing, key algorithms, wire serialization |
| `src/Certes/Pkcs/` | CSR, certificate-chain, and PFX utilities |
| `src/Certes.Cli/` | Commands, dependency injection, settings, Azure integrations |
| `test/Certes.Tests/` | Offline xUnit/Moq tests and ephemeral certificate fixtures |
| `test/Certes.Tests.Integration/` | ACME integration flows using local Pebble and challtestsrv |
| `test/Certes.Func/` | Azure Functions challenge-test helper; not part of the core library |
| `misc/certes.props` | Shared build settings, signing, versions, warnings-as-errors |
| `docs/` | Usage/API documentation and DocFX site |
| `.github/workflows/build.yml` | Cross-platform compilation and unit tests, package smoke checks |
| `scripts/PackageSmoke/` | Consumer of the locally packed library; not a solution project |
| `scripts/Pebble/` | Pinned container stack, readiness probe, and local integration instructions |
| `azure-pipelines.yml` | Disabled legacy pipeline placeholder |
| `docs/ci-migration.md` | CI retirement state and GitHub Actions migration follow-ups |

`README.md` is the repository landing page. `docs/README.md` is the detailed usage
guide and is included by `docs/index.md` in the documentation site. Update the
appropriate guide when behavior changes; avoid duplicating long usage examples.

## Working approach

- Inspect the working tree first; preserve unrelated or user-authored changes.
- Keep each change focused. Separate dependency migrations, behavior fixes, and
  broad formatting changes so their effects can be verified independently.
- Follow `.editorconfig` and nearby code: four-space C# indentation, Allman
  braces, existing namespaces and naming. Do not mass-reformat files.
- Prefer existing abstractions and test helpers. Add dependencies only for a
  concrete need; keep provider-specific dependencies out of the core library.
- Public API changes must account for interfaces, extension methods, downstream
  implementers, serialization, and all retained target frameworks. Explain
  intended breaking changes rather than introducing them incidentally.
- Preserve assembly signing and package identity unless changing them is part
  of the task. `SkipSigning=true` is a local/test aid, not a release setting.
- Do not hand-edit generated `Strings.Designer.cs` files, build outputs, or
  generated API documentation. Change the source resource/generator input.
- Add focused regression tests for behavior fixes. Assert observable behavior,
  especially failure paths, rather than duplicating implementation details.
- Report files changed, checks actually run, outcomes, and blockers. Distinguish
  compilation success, test success, and verified CA interoperability.

## Branching and releases

- Use short-lived branches for focused tasks, based on current `main`. Examples:
  `docs/revival-guide`, `build/net10`, `test/offline-fixtures`,
  `fix/order-polling`, and `deps/bouncycastle`.
- Keep each branch/PR independently reviewable and verifiable. Do not collect
  the entire revival on a long-lived branch or mix unrelated changes.
- Before creating or switching branches, inspect the working tree and current
  branch. Preserve existing work; do not reset or discard it to start a task.
- Merge reviewed changes into `main` incrementally after relevant checks pass.
  Until the baseline is repaired, report known failures explicitly and distinguish
  them from regressions; do not hide failures to make a PR appear green.
- `main` represents development toward the next release. Merging a PR does not
  authorize publishing a package. Use an explicit release/tag workflow, with
  prereleases when useful; the consolidated release pipeline is still planned.
- Create a release/maintenance branch only when parallel support is needed, such
  as maintaining an existing version while `main` develops breaking changes.
- Commit, push, open/merge PRs, tag, or publish only when requested. Summarize the
  diff and verification results for review; do not assume release authorization.

## AI assistance disclosure

- In PR descriptions and review summaries, briefly disclose the coding harness
  and model used, when known. Do not guess a model name or version.
- State whether AI assisted with implementation, review, or both. A session
  checking its own changes is a self-review, not an independent review.
- Include checks actually run, their results, and any unresolved limitations.
  Model attribution does not replace verification evidence.
- Claim human review only when a human has actually performed it. Do not imply
  human approval or independent review from automated checks alone.
- Keep this disclosure in PR/review metadata rather than source-file comments.

Example PR disclosure (replace the placeholders with actual details):

> **AI assistance:** Prepared with [harness] using [model].
> **Verification:** [Checks run, results, and limitations].

Example review disclosure:

> **AI-assisted review:** [Self-review or independent review] using [model].
> **Scope and verification:** [Diff reviewed, checks run, and limitations].

## Build and verification

Legacy repository webhooks and obsolete required checks are disabled. The
GitHub Actions workflow checks compilation, the full offline unit suite, and
package consumption. Unit tests run on all three OS runners without filters or
failure suppression. The former `run_legacy_tests` opt-in is removed. Consult
`docs/ci-migration.md` before changing automation. Run relevant checks locally and
report results during this transition; hosted workflow success must be verified
after pushing the workflow.

Run commands from the repository root. Check `dotnet --info` before diagnosing
runtime failures. Install a .NET 10 SDK for development. There is no `global.json`;
local SDK selection follows the installed .NET environment. CI explicitly installs
the latest available `10.0.x` SDK in `.github/workflows/build.yml` and logs
`dotnet --info`. Record the resolved SDK/runtime versions when reporting checks.

The CLI and modern unit tests target and run on .NET 10 directly, without
`DOTNET_ROLL_FORWARD`. Workflow syntax can be checked with `actionlint`.

Current targets are `net10.0;net8.0;netstandard2.0;net462` for the library, `net10.0` for
the CLI, and `net10.0;net462` for tests. The Functions helper still targets the
out-of-support `net7.0` and remains outside CI pending retirement; the integration
suite now uses local Pebble instead.
The package smoke project runs on .NET 8 and 10 and also compiles (but does not
run) a `net6.0` consumer. MSBuild assertions verify the selected package asset
and version for each target, including `netstandard2.0` selection on .NET 6.
.NET 8 support ends November 10, 2026; revisit that asset's support policy then.

### Build the core library across its targets

```sh
dotnet build src/Certes/Certes.csproj
```

### Compile and run the unit-test project

```sh
dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true
```

This also builds the CLI. The test host requires the .NET 10 runtime.
Building `net462` on a non-Windows machine does not demonstrate that its tests
run; use a Windows/.NET Framework environment for runtime verification.

Always report which runtime was used. Use `--filter FullyQualifiedName~<TestClass>` for
focused tests when appropriate, then run checks relevant to the affected surface.

### Integration tests

```sh
docker compose -f scripts/Pebble/compose.yml up -d
bash scripts/Pebble/wait.sh
dotnet test test/Certes.Tests.Integration/Certes.Tests.Integration.csproj -f net10.0 -p:SkipSigning=true
docker compose -f scripts/Pebble/compose.yml down
```

Inspect `test/Certes.Tests.Integration/IntegrationHelper.cs` and the relevant integration
test before changing the setup. See `scripts/Pebble/README.md` for prerequisites,
TLS certificate pinning, fixed loopback ports, and coverage limits. Docker Desktop
is verified on macOS ARM64; Linux CI uses Docker. Podman is not yet verified.
The local harness runs only on .NET 10; `net462` remains compile-only and rejects
network initialization. Never add a global TLS bypass or public-CA fallback.
Keep unit tests network-independent: `CertificateFixture` generates
an ephemeral root/intermediate/leaf chain and matching key locally. The network
helper belongs only to the integration-test project. Restoring NuGet packages
still requires a package source/cache; offline execution refers to the tests.

### Dependency checks

After restoring/building the relevant project:

```sh
dotnet list src/Certes/Certes.csproj package --vulnerable --include-transitive --no-restore
dotnet list src/Certes.Cli/Certes.Cli.csproj package --vulnerable --include-transitive --no-restore
dotnet list src/Certes.Cli/Certes.Cli.csproj package --deprecated --include-transitive --no-restore
```

For package/build changes, also verify `dotnet pack` for the affected shipping
project in Release configuration and test consumption of the resulting package.
Do not infer release readiness from the unsigned test build alone.

To reproduce the package smoke checks (POSIX shell, .NET 10 SDK), use an empty
temporary NuGet cache so a prior package with the same smoke version cannot mask
the current output:

```sh
export NUGET_PACKAGES="$(mktemp -d)"
export CERTES_PACKAGE_VERSION=0.0.0-ci-smoke
dotnet pack src/Certes/Certes.csproj -c Release -p:ContinuousIntegrationBuild=true --output artifacts/packages
dotnet pack src/Certes.Cli/Certes.Cli.csproj -c Release -p:ContinuousIntegrationBuild=true --output artifacts/packages
dotnet run --project scripts/PackageSmoke/PackageSmoke.csproj -c Release -f net10.0
dotnet run --project scripts/PackageSmoke/PackageSmoke.csproj -c Release -f net8.0
dotnet build scripts/PackageSmoke/PackageSmoke.csproj -c Release -f net6.0
dotnet tool install dotnet-certes --version "$CERTES_PACKAGE_VERSION" --tool-path artifacts/tools --add-source artifacts/packages
artifacts/tools/certes --help
```

Use a fresh `artifacts/tools` directory for tool installation. These checks verify
the version supplied by `CERTES_PACKAGE_VERSION`; the consumer requires this
variable and shares it with packing and CLI installation. This verifies
local package consumption only; packages are not published. Install the .NET 8
runtime for its native smoke run (CI installs SDK 8.0.x alongside 10.0.x).
The .NET 6 compatibility probe is compile-only. These checks do not verify every
library target at runtime or full certificate issuance.

For documentation-only changes, check links and `git diff --check`; compilation
and test reruns are unnecessary unless code examples or behavior also change.

## Protocol and cryptography rules

- Use RFC 8555 and the relevant extension specifications as protocol references.
  `docs/implementation-status.md` predates the final RFC and is not authoritative.
- Preserve JWS wire semantics, nonce handling, POST-as-GET, external account
  binding, and ACME problem details. Cover wire-level behavior when changing
  signing or serialization.
- Use established cryptographic APIs. Changes to BouncyCastle or key handling
  need RSA/ECDSA, key round-trip, CSR, and PEM/PFX interoperability coverage.
- Distinguish chain packaging from certificate trust validation. Exercise
  alternate/cross-signed chains and missing roots rather than assuming a fixed
  Let's Encrypt chain.
- Exercise pending/ready/processing/valid/invalid order states, `Retry-After`,
  bad nonces, and network errors when changing issuance or polling.
- Use test keys and local/staging CAs for development. Keep private keys, tokens,
  and account credentials out of commits, logs, and test output. The loopback
  certificate pin in the integration helper must not migrate into production transport.

## Known revival baseline and pitfalls

Baseline reviewed on 2026-09-20, using SDK 10.0.301 on macOS ARM64. Protocol
observations below originate at `ffa00c6`; build/test and audit status
were updated after the .NET 10 migration:

- The core library built for all targets; CLI/unit-test compilation also passed.
- On the native `net10.0` target, all 149 unit tests pass, with no skips. Four
  offline TLS-ALPN certificate-generation cases cover RSA/ECDSA keys, SANs,
  self-signatures, and the critical ACME identifier extension. The six
  former hosted-Pebble failures now use local certificate fixtures with matching
  keys and PFX assertions; a missing-issuer test was added. The 13 integration tests
  pass locally against pinned Pebble 2.10.1 on Docker Desktop/macOS ARM64. This
  verifies the local test CA, not public CA interoperability. Production polling
  issues below are not fixed by the bounded polling in the test helper.
- `IOrderContextExtensions.Generate()` defaults to 60 polling retries, honors
  server-directed `Retry-After` intervals, and preserves explicit retry budgets.
- `Pkcs/CertificationStore.cs` (the filename differs from `CertificateStore`)
  requires a path to a self-signed root, uses old embedded roots, and indexes
  issuers by subject DN. This affects both PEM and PFX export.
- CLI settings contain account keys/Azure credentials. `FileUtil` currently
  uses default file permissions and non-atomic writes.
- CLI Azure Fluent dependencies remain legacy. A fresh audit after the .NET 10
  retarget reported no vulnerable packages in the library or CLI graphs; the old
  net6.0 graph had flagged `System.Text.RegularExpressions 4.3.0`. Dependencies
  were not upgraded in this migration. Re-run audits before drawing current
  conclusions; package findings do not prove exploitability.
- Legacy CI has been retired, repository webhooks disabled, and Azure build/release
  automation disabled and verified. Initial Actions build/package checks are
  defined with automatic unit-test execution; all four checks are required on main
  (see `docs/ci-migration.md`).
- Cancellation, renewal information, certificate profiles, and IP identifiers
  are not implemented in the current APIs.

Recheck these observations before using them as the basis for a change. Remove
or revise each entry when fixed, and update the root README's baseline/status
when the supported development workflow changes.
