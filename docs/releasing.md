# Releasing Certes

Certes releases are driven by `.github/workflows/release.yml` and tags named
`vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-PRERELEASE`.

The workflow verifies that the tag commit is on `main`, requires successful
required checks, runs offline and Pebble test suites, packs signed artifacts,
verifies package metadata, and then waits for approval in the `nuget-release`
environment before publishing.

## One-time repository setup

1. Create the `nuget-release` GitHub environment and set required reviewers.
2. Add a repository ruleset that restricts create/update/delete operations for
   tags matching `v*` to release administrators.
3. Add repository environment secret `NUGET_USER` in `nuget-release` with the
   nuget.org profile name authorized to publish both `Certes` and
   `dotnet-certes`.
4. In nuget.org Trusted Publishing, add a GitHub Actions policy for:
   - owner `fszlin`
   - repository `certes`
   - workflow `release.yml`
   - environment `nuget-release`
   - package IDs `Certes` and `dotnet-certes` (if package scoping is available)

## Changelog and package metadata requirements

- Each release requires a `## [VERSION] - DATE` section in
  `docs/CHANGELOG.md` where `VERSION` matches the tag without the leading `v`.
- The workflow uses `scripts/release-notes.sh` to extract that section and fails
  if the section is missing or empty.
- The extracted notes become both NuGet package release notes and the GitHub
  release body.
- Package readmes in `src/Certes/README.md` and `src/Certes.Cli/README.md` are
  validated during release packaging.

You can preview notes locally:

```sh
bash scripts/release-notes.sh 4.0.0
```

## Release flow

1. Merge release notes/version preparation to `main`.
2. Wait for required checks on that `main` commit.
3. Create and push an annotated tag, for example `v4.0.1` or
   `v4.1.0-beta.1`.
4. In GitHub Actions, review `Verify and package` output.
5. Approve `nuget-release` only after verifying package version and artifacts.

NuGet does not provide an atomic multi-package transaction; approval authorizes
publishing both packages.

## Failure recovery

- If publish fails after one package is already accepted by NuGet, re-run only
  `Publish to NuGet and GitHub`.
- Do not re-run the full workflow to publish different artifacts under the same
  version.
- Do not reuse a published version.
- If a tag was pushed before required checks on `main` completed, re-run the
  failed prepare job after checks finish.
