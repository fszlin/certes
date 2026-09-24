# Releasing Certes

`.github/workflows/release.yml` prepares releases from tags named
`vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-PRERELEASE`. It rejects tags whose
commit is not contained in `main`, verifies that both package versions are
unused, runs the offline and Pebble suites, creates signed packages, and repeats
the package consumer checks. Publishing then waits on the protected
`nuget-release` GitHub environment before obtaining a short-lived NuGet
credential through OIDC.

## One-time repository setup

1. Create the `nuget-release` GitHub environment and add required reviewers. Do
   not allow administrators to bypass its protection for routine releases.
2. Add a repository ruleset that restricts creation, update, and deletion of
   tags matching `v*` to release administrators.
3. Add an environment secret named `NUGET_USER` containing the nuget.org profile
   name that owns or can publish both `Certes` and `dotnet-certes`. This is the
   profile name, not an email address or API key.
4. In that nuget.org account's Trusted Publishing settings, add a GitHub Actions
   policy for owner `fszlin`, repository `certes`, workflow file `release.yml`,
   and environment `nuget-release`. Scope it to the `Certes` and
   `dotnet-certes` package IDs if the policy UI offers package scopes.

## Release notes and package pages

- Every release needs a `## [VERSION] - DATE` section in `docs/CHANGELOG.md`,
  where `VERSION` matches the tag without the `v`. The workflow extracts it with
  `scripts/release-notes.sh` and stops before packing if the section is missing
  or empty. The same text becomes the NuGet `releaseNotes` and the GitHub
  release body. Preview it locally with
  `bash scripts/release-notes.sh 4.0.0-beta.2`.
- Definitions for reference-style links used in the section (such as `[i232]`)
  are appended automatically.
- `scripts/release-notes.sh` fails when the generated notes exceed 30,000
  characters.
- `src/Certes/README.md` and `src/Certes.Cli/README.md` are the package readmes
  shown on nuget.org. Use absolute links. Review them in the release-notes PR
  whenever supported targets, runtime requirements, or usage change.
- Package validation in the workflow checks presence of package readme and
  release-notes metadata, not the semantic quality of their text.
- Package metadata cannot be edited after publishing; fixes need a new version.

## Release flow

To prepare a prerelease, merge the reviewed release notes and version decision to
`main`, wait for the required checks on that `main` commit to pass, then create
and push an annotated tag such as `v4.0.0-beta.1`. Review the
`Verify and package` job and approve the environment deployment only when its
package version and artifacts are correct. NuGet does not provide an atomic
multi-package transaction, so approval authorizes publication of both packages.
The workflow creates the GitHub prerelease after NuGet accepts them.

## Failure recovery

- If publication is interrupted after only one package reaches NuGet, re-run the
  failed `Publish to NuGet and GitHub` job. Pushes use `--skip-duplicate`, so
  the existing package is left unchanged while the missing package and GitHub
  release are completed.
- Do not re-run the full workflow after a partial publish.
- Never reuse a version for different artifacts.
- If the tag was pushed before the `main` checks finished, the prepare job fails
  without publishing; re-run it once those checks succeed.
