# Certes

Certes is a .NET client for the Automated Certificate Management Environment
([ACME, RFC 8555](https://www.rfc-editor.org/rfc/rfc8555)) protocol. It provides
an API for obtaining and managing certificates, plus a `certes` command-line tool.

The library implements ACME v2 accounts, orders, wildcard certificates,
HTTP-01/DNS-01/TLS-ALPN-01 challenge support, external account binding,
account-key rollover, revocation, and PEM/PFX export. Applications are responsible
for provisioning challenge responses and scheduling renewals.

## Project status

Certes is being revived after a period of inactivity. The immediate goal is a
reproducible build and reliable tests on a supported .NET runtime, followed by
dependency updates and issuance/export fixes.

The current checkout still targets:

| Component | Targets |
| --- | --- |
| Library | `net6.0`, `netstandard2.0`, `net462` |
| CLI | `net6.0` |
| Unit and integration tests | `net6.0`, `net462` |
| Azure Functions test helper | `net7.0` |

.NET 6 and 7 are out of support. Moving development and the CLI to .NET 10 LTS
is the proposed next step; it has not yet been implemented. Target frameworks in
the project files describe this checkout, not necessarily the latest published
NuGet packages.

## Use Certes

Install the library into your application:

```sh
dotnet add package Certes
```

Or install the published CLI:

```sh
dotnet tool install --global dotnet-certes
certes --help
```

See the package's runtime requirements when installing a published tool version.
Use a staging CA while developing an issuance workflow.

- [Library getting-started guide](docs/README.md)
- [ACME API guide](docs/APIv2.md)
- [CLI guide](docs/CLI.md)
- [Historical changelog](docs/CHANGELOG.md)
- [Contributing](.github/CONTRIBUTING.md)

The usage guides are being refreshed as part of the revival; their historical
examples and implementation-status claims should be checked against the code.

## Develop

**Start with [AGENTS.md](AGENTS.md)** for the project map, development rules,
verification commands, and known blockers. It is intended for both human
contributors and LLM coding agents.

Development uses short-lived, focused branches merged into `main` after review
and verification. Releases are explicit; merging does not publish packages. See
the [branching and release strategy](AGENTS.md#branching-and-releases).

Legacy CI webhooks and obsolete required checks have been disabled. GitHub Actions
workflows are not yet installed. See [CI migration status](docs/ci-migration.md)
for completed changes, including verified Azure build/release shutdown.

Run commands from the repository root:

```sh
dotnet build src/Certes/Certes.csproj
dotnet test test/Certes.Tests/Certes.Tests.csproj -f net6.0 -p:SkipSigning=true
```

The test command requires a .NET 6 runtime by default. The repository does not
yet pin an SDK with `global.json`. See [AGENTS.md](AGENTS.md) for the temporary
runtime roll-forward diagnostic and test-service dependency.

### Revival baseline

At commit `ffa00c6`, reviewed on 2026-09-20 using SDK 10.0.301 on macOS ARM64:

- The library built for all three targets with zero warnings or errors.
- CLI and unit-test compilation succeeded.
- Running the `net6.0` unit tests with roll-forward to .NET 10 produced
  **138 passed and 6 failed**. All six failures reached the old hosted Pebble
  endpoint, which returned HTTP 401.

This is a dated diagnostic baseline, not a claim of current CA interoperability
or a supported .NET 10 test configuration.

### Revival priorities

1. Establish a supported SDK/runtime, offline unit tests, local Pebble integration
   tests, and one maintained CI pipeline.
2. Fix order polling, certificate-chain export, alternate-chain handling, and
   CLI secret-file handling.
3. Refresh dependencies, validate package consumption, and update documentation
   before the next release; prereleases are optional when useful for validation.
4. Evaluate renewal information, certificate profiles, and IP identifiers after
   the reliability baseline is established.

## License

[MIT](LICENSE).
