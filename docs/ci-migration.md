# CI migration to GitHub Actions

## Status as of 2026-09-20

Certes has retired its legacy automation. An initial GitHub Actions workflow is
defined for build/package validation. All three OS builds and package smoke
checks passed in [hosted run 35544493782](https://github.com/fszlin/certes/actions/runs/35544493782)
at commit `0d29d32`; that initial rollout did not execute unit tests. The follow-up
offline-fixture change adds automatic unit tests to each OS job. All 145 tests
passed on each OS, alongside package smoke checks, in
[hosted run 35545870442](https://github.com/fszlin/certes/actions/runs/35545870442)
at commit `09490e1`, using .NET 10 roll-forward.
Run the local checks in
[AGENTS.md](https://github.com/fszlin/certes/blob/main/AGENTS.md) and include their
results in PRs during the transition.

### Initial Actions workflow

`.github/workflows/build.yml` runs on PRs targeting `main`, pushes to `main`, and
manual dispatch, with the latest `10.0.x` SDK installed by `actions/setup-dotnet` and read-only
repository permissions. The .NET 10 migration changes the modern targets from
`net6.0` to `net10.0` and removes diagnostic runtime roll-forward. Local verification
passed all 145 tests on .NET 10. Hosted run
[35548024969](https://github.com/fszlin/certes/actions/runs/35548024969) verified
all four checks at `80ce393`. The .NET 8 asset/smoke additions also passed in
[run 35549055095](https://github.com/fszlin/certes/actions/runs/35549055095).

- `Build (ubuntu-24.04)`, `Build (windows-2025)`, and `Build (macos-26)` compile
  the signed library in Release for all retained targets, then compile the CLI
  explicitly and both test projects with `SkipSigning=true` in Debug. They do
  run the full `net10.0` unit suite directly on .NET 10 using those
  compiled outputs. No filters or failure suppression are applied. The Azure
  Functions helper and integration-test execution remain outside these jobs.
- `Package smoke checks` packs the signed library and CLI in Release, then
  consumes both packages using an isolated NuGet cache. Library consumers on
  .NET 8 and 10 exercise RSA/ECDSA key round-trips and CSR generation; the CLI runs
  `--help` directly on .NET 10. A compile-only `net6.0` consumer checks selection of
  the library's `netstandard2.0` compatibility asset. MSBuild assertions enforce
  the exact package version and asset path on all three consumer targets. The
  package job installs SDK 8.0.x alongside 10.0.x for native .NET 8 execution.
  No packages are published or
  uploaded.
- The former `Legacy unit tests (manual diagnostic)` job and `run_legacy_tests`
  input are removed: the unit suite now runs on every PR, main push, and manual run.
- `Pebble integration` starts the pinned local CA and challenge responder on a
  Linux runner, waits for readiness, executes the 13 integration cases on .NET 10,
  and tears down the containers even on failure. Local macOS ARM64 verification
  and Linux [run 35550345812](https://github.com/fszlin/certes/actions/runs/35550345812)
  passed at `0bc5579`; repeated successful runs have since established stability.
  This check is now required on `main`. See [local setup](../scripts/Pebble/README.md) for scope and commands.

The six former HTTP 401 failures used a hosted CA to obtain test certificates.
They now generate valid root/intermediate/leaf chains and matching keys locally,
and inspect exported PFX contents. `IntegrationHelper.cs` moved to the integration
project; unit tests no longer have access to that network helper. No test cases
were disabled. A missing-issuer failure test brings the suite to 145 cases.

The new PFX fixture covers RSA and ES256/ES384/ES512 leaf keys, all issued by
RSA-signing CAs. It does not exercise ECDSA-signed chains, cross-signing, unordered
or extraneous issuer bundles, or duplicate subject DNs. Its supplied root also
does not exercise embedded-root fallback. The existing
`CertificateChainTests.CanGenerateFullChainPemWithKey` still exercises fallback
to embedded DST Root CA X3 during PEM export; this is not comprehensive coverage
of every embedded root or of PFX path building. Broader chain coverage belongs
with certificate-store/export fixes.

A green run verifies unit tests and package consumption, not CA interoperability
or `net462` runtime behavior.

Runner OS labels are explicit versions matching the current `-latest` image
families at selection time: Ubuntu 24.04 and Windows Server 2025 on x64, macOS 26
on ARM64. Revisit these labels before image deprecation; versioned labels still
receive image updates and are not immutable snapshots.

Action references are pinned to commit SHAs. Jobs have timeouts and superseded
PR runs are cancelled. Push and manual runs use unique concurrency groups so
rapid merges do not replace pending or running main builds. Required branch
checks are configured after successful verification: all three `Build (...)` checks,
`Package smoke checks`, and `Pebble integration` are required on `main`, restricted
to the GitHub Actions app, with branches required to be up to date. Existing
administrator bypass settings were preserved.

The first hosted run exposed missing `.gitmodules` metadata for the existing
`docs/docstrap` gitlink. Its repository URL is restored so checkout can clean up
credentials successfully. Build jobs do not initialize the documentation submodule.

Known build/package warnings include missing package README notices and
SourceLink's missing `docs/docstrap/.git`
warning from the uninitialized documentation submodule. The SourceLink warning
is retained and disclosed; resolving it belongs with documentation/submodule or
SourceLink maintenance. These checks do not verify debugger source retrieval.

### .NET 10 compatibility decisions

- Local development requires a .NET 10 SDK, with no `global.json` pin. CI installs
  the latest `10.0.x` SDK and logs `dotnet --info` in both jobs. CI follows .NET 10
  SDK updates rather than pinning a patch/feature band; historical verification
  results record the exact versions used at the time.
- Library assets are `net10.0`, `net8.0`, `netstandard2.0`, and `net462`.
  .NET 8/9 consumers select `net8.0`; .NET 6/7 select `netstandard2.0` after removal
  of the dedicated `net6.0` asset. The .NET 8 asset is covered by a native package
  smoke run, not the full unit suite. Microsoft support for .NET 8 ends on
  [November 10, 2026](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core);
  review its support policy before release and again at that date.
- The CLI requires .NET 10. This runtime requirement change must be included in
  release notes. Assembly signing, package identities, and dependency versions
  are retained.
- Legacy formatter-based exception serialization members remain present but are
  marked obsolete on modern .NET, matching the platform's `SYSLIB0051` guidance.
  Legacy-target annotations remain unchanged.
- CLI certificate loading uses `X509CertificateLoader`; integration tests retain
  the existing constructor on `net462`. The Functions helper remains `net7.0`.

### Repository settings changed

- Disabled all six legacy repository webhooks: AppVeyor, Travis CI, Codecov,
  Code Climate, Codacy, and Better Code Hub. Hooks were retained in an inactive
  state rather than deleted.
- Removed `continuous-integration/appveyor/pr` and `certes` from required status
  checks on `main`. Other branch-protection settings were preserved.
- Switched GitHub Pages from branch-based builds to GitHub Actions deployment.
  No replacement documentation deployment workflow is installed yet.

Disabling webhooks prevents new event deliveries through those hooks. It does
not cancel existing runs, disable service-side schedules, uninstall GitHub Apps,
or revoke external service credentials.

### Repository configuration changes

- Removed AppVeyor and Travis configuration, including AppVeyor's automatic
  `gh-pages` publishing script.
- Removed Better Code Hub and Codecov configuration, the Codecov-only local
  tool manifest, obsolete Azure build/pack templates, and legacy CI badges.
- Retained `azure-pipelines.yml` as a disabled placeholder: `trigger: none`,
  `pr: none`, and a job with an always-false condition. This retires its test,
  package, and artifact-upload jobs once the configuration reaches the branch
  used by the pipeline. Other remote branches retain their old YAML until updated.
- Retained `GitVersion.yml` and shared package/signing settings for evaluation
  when implementing releases; retiring CI does not change package identity.
- Retained `.github/stale.yml` because it configures issue/PR housekeeping rather
  than build or publishing automation. Its Stale GitHub App installation status
  is unverified; retaining the file is not confirmation that the bot is active
  or disabled. Review the installation before deciding whether to keep automated
  stale marking and closure during the revival.

These file changes take effect remotely only after they are pushed/merged.

### Azure DevOps disabled and verified

After the owner authenticated the CLI, the following settings were changed in
[fszlin/Certes](https://dev.azure.com/fszlin/Certes) and read back for verification:

- [Build pipeline `certes` (ID 10)](https://dev.azure.com/fszlin/Certes/_build?definitionId=10):
  `queueStatus: disabled`, revision 12. New builds cannot be queued.
- Classic release definition `Certes-Release` (ID 1): revision 23, with all
  automatic release triggers removed. MyGet, NuGet, and NuGet Tool environments
  have no automatic deployment conditions or schedules, and all workflow tasks
  are disabled, including package publishing.
- Queries returned no not-started, in-progress, postponed, or cancelling builds
  for pipeline 10, and no pending/in-progress deployments for release definition 1.

Definitions and historical runs were retained. Existing release snapshots retain
their original configuration; do not manually redeploy historical releases.
Disabling this automation does not depend on whether old service credentials
have expired. Credentials were not revoked or tested for package publishing.

### Other external-service follow-ups

The current GitHub token could not enumerate GitHub App installations. For a
complete cleanup beyond the disabled repository webhooks:

1. In [repository integrations](https://github.com/fszlin/certes/settings/installations),
   remove this repository from legacy CI/analysis GitHub Apps if any remain.
   Avoid uninstalling account-wide integrations used by other repositories.
2. Check the other retired services for existing runs, schedules, or deployments that
   do not depend on GitHub webhook delivery.

## Replacement plan

The Pebble integration job has been verified on hosted runners and promoted to a
required branch check. Further improvements to expand integration coverage are
possible; after production polling/nonce fixes, add an optional stress job with
delays, nonce rejection, and authorization reuse enabled; baseline success does not
cover those conditions.

Use focused PRs to introduce:

1. Documentation validation and broader local Pebble integration coverage. Preserve known
   failure reporting rather than weakening assertions to obtain a green build.
2. Maintain required status checks on `main` as CI coverage evolves.
3. DocFX builds and GitHub Pages deployment for documentation changes.
4. Explicit release/tag-driven package publishing, gated on successful
   verification, with prerelease support and preserved assembly signing.
5. Dependency updates and scheduled vulnerability checks.
6. Retire the unused `test/Certes.Func` hosted challenge helper in a focused change;
   local Pebble now replaces it for integration tests. Its .NET 7 target remains
   outside current CI coverage.

Use minimal workflow permissions and pin third-party actions to reviewed commit
SHAs. PR validation must not publish packages or require publishing credentials.
Update this document and the root README as each replacement becomes operational.
