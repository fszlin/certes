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
manual dispatch, with SDK 10.0.301 and read-only repository permissions:

- `Build (ubuntu-24.04)`, `Build (windows-2025)`, and `Build (macos-26)` compile
  the signed library in Release for all retained targets, then compile the CLI
  explicitly and both test projects with `SkipSigning=true` in Debug. They do
  run the full `net6.0` unit suite with .NET 10 runtime roll-forward using those
  compiled outputs. No filters or failure suppression are applied. The Azure
  Functions helper and integration-test execution remain outside these jobs.
- `Package smoke checks` packs the signed library and CLI in Release, then
  consumes both packages using an isolated NuGet cache. The .NET 10 library
  consumer exercises RSA/ECDSA key round-trips and CSR generation; the CLI runs
  `--help` using .NET 10 roll-forward. No packages are published or uploaded.
- The former `Legacy unit tests (manual diagnostic)` job and `run_legacy_tests`
  input are removed: the unit suite now runs on every PR, main push, and manual run.

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
or `net462` runtime behavior. Runtime/target modernization remains a separate change.

Runner OS labels are explicit versions matching the current `-latest` image
families at selection time: Ubuntu 24.04 and Windows Server 2025 on x64, macOS 26
on ARM64. Revisit these labels before image deprecation; versioned labels still
receive image updates and are not immutable snapshots.

Action references are pinned to commit SHAs. Jobs have timeouts and superseded
PR runs are cancelled. Push and manual runs use unique concurrency groups so
rapid merges do not replace pending or running main builds. Required branch
checks should be configured only after these job names and hosted runs have
been verified; they have not been configured yet.

The first hosted run exposed missing `.gitmodules` metadata for the existing
`docs/docstrap` gitlink. Its repository URL is restored so checkout can clean up
credentials successfully. Build jobs do not initialize the documentation submodule.

Known build/package warnings include `NETSDK1138` for the CLI's .NET 6 target,
missing package README notices, and SourceLink's missing `docs/docstrap/.git`
warning from the uninitialized documentation submodule. The SourceLink warning
is retained and disclosed; resolving it belongs with documentation/submodule or
SourceLink maintenance. These checks do not verify debugger source retrieval.

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

Establish required checks after hosted verification of the automatic unit suite,
then replace external integration-test dependencies with local Pebble.

Use focused PRs to introduce:

1. Documentation validation and local Pebble integration tests. Preserve known
   failure reporting rather than weakening assertions to obtain a green build.
2. New required status checks on `main` after stable Actions check names exist.
3. DocFX builds and GitHub Pages deployment for documentation changes.
4. Explicit release/tag-driven package publishing, gated on successful
   verification, with prerelease support and preserved assembly signing.
5. Dependency updates and scheduled vulnerability checks.
6. Decide whether to modernize and compile `test/Certes.Func` or retire it once
   local Pebble replaces the hosted challenge infrastructure; its .NET 7 target
   remains outside current CI coverage.

Use minimal workflow permissions and pin third-party actions to reviewed commit
SHAs. PR validation must not publish packages or require publishing credentials.
Update this document and the root README as each replacement becomes operational.
