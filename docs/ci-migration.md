# CI migration to GitHub Actions

## Status as of 2026-09-20

Certes has retired its legacy automation. An initial GitHub Actions workflow is
defined for build/package validation. All three OS builds and package smoke
checks passed in [hosted run 35544493782](https://github.com/fszlin/certes/actions/runs/35544493782)
at commit `0d29d32`; the optional legacy test diagnostic was not run.
Run the local checks in
[AGENTS.md](https://github.com/fszlin/certes/blob/main/AGENTS.md) and include their
results in PRs during the transition.

### Initial Actions workflow

`.github/workflows/build.yml` runs on PRs targeting `main`, pushes to `main`, and
manual dispatch, with SDK 10.0.301 and read-only repository permissions:

- `Build (ubuntu-24.04)`, `Build (windows-2025)`, and `Build (macos-26)` compile
  the signed library in Release for all retained targets, then compile the CLI
  and both test projects with `SkipSigning=true` in Debug. They do not execute
  the tests or compile the Azure Functions hosted challenge helper.
- `Package smoke checks` packs the signed library and CLI in Release, then
  consumes both packages using an isolated NuGet cache. The .NET 10 library
  consumer exercises RSA/ECDSA key round-trips and CSR generation; the CLI runs
  `--help` using .NET 10 roll-forward. No packages are published or uploaded.
- `Legacy unit tests (manual diagnostic)` runs only when a manual dispatch sets
  `run_legacy_tests` to true. It runs the entire `net6.0` unit suite with .NET 10
  roll-forward, with no exclusions or failure suppression. The six hosted-Pebble
  failures are expected until fixtures are repaired. This diagnostic must not
  become a required PR check in its current form.

A green build/package run is not a passing unit/integration suite or verified CA
interoperability. The skipped-by-default diagnostic is a temporary CI rollout
boundary, not removal or disabling of test cases. Automatic offline test runs
remain the next priority. Runtime/target modernization is a separate change.

Runner OS labels are explicit versions matching the current `-latest` image
families at selection time: Ubuntu 24.04 and Windows Server 2025 on x64, macOS 26
on ARM64. Revisit these labels before image deprecation; versioned labels still
receive image updates and are not immutable snapshots.

Action references are pinned to commit SHAs. Jobs have timeouts and superseded
runs are cancelled. Required branch checks should be configured only after these
job names and hosted runs have been verified; they have not been configured yet.

The first hosted run exposed missing `.gitmodules` metadata for the existing
`docs/docstrap` gitlink. Its repository URL is restored so checkout can clean up
credentials successfully. Build jobs do not initialize the documentation submodule.

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

Establish required build checks and offline unit tests following the successful
initial hosted run. Keep the test gap short without concealing the six known
hosted-service failures.

Use focused PRs to introduce:

1. Documentation validation, offline unit tests, and local Pebble
   integration tests as the test baseline is repaired. Preserve known failure
   reporting rather than weakening assertions to obtain a green build.
2. New required status checks on `main` after stable Actions check names exist.
3. DocFX builds and GitHub Pages deployment for documentation changes.
4. Explicit release/tag-driven package publishing, gated on successful
   verification, with prerelease support and preserved assembly signing.
5. Dependency updates and scheduled vulnerability checks.

Use minimal workflow permissions and pin third-party actions to reviewed commit
SHAs. PR validation must not publish packages or require publishing credentials.
Update this document and the root README as each replacement becomes operational.
