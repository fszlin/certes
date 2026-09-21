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
| Library | `net10.0`, `net8.0`, `netstandard2.0`, `net462` |
| CLI | `net10.0` |
| Unit and integration tests | `net10.0`, `net462` |
| Azure Functions test helper | `net7.0` |

Development and the CLI now use .NET 10 LTS. CI installs the latest `10.0.x` SDK
and logs the resolved version.
The Functions helper remains on out-of-support .NET 7 pending replacement.
Targets describe this checkout, not necessarily the latest published packages.

The library retains .NET Standard 2.0 and .NET Framework 4.6.2 compatibility assets;
consumers on .NET 8/9 select `net8.0`, while .NET 6/7 use `netstandard2.0` instead
of a dedicated `net6.0` build. CI runs package smoke checks on .NET 8 and 10 and
compiles a .NET 6 consumer but does not run it on the
unsupported .NET 6 runtime. The next CLI package requires .NET 10; this is a
runtime requirement change for existing CLI users.

.NET 8 remains supported by Microsoft until November 10, 2026. Its library
asset supports existing consumers; .NET 10 is the development and CLI baseline.

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

See the [branching and release strategy](AGENTS.md#branching-and-releases)
before preparing a contribution or release.

The initial [GitHub Actions workflow](.github/workflows/build.yml) checks
cross-platform compilation, the full offline unit suite, and local package
consumption. Tests run directly on .NET 10. See
[CI migration status](docs/ci-migration.md) for coverage and
remaining work. Legacy build/release automation is disabled.

Run commands from the repository root:

```sh
dotnet build src/Certes/Certes.csproj
dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true
```

Install a .NET 10 SDK; no runtime roll-forward is needed.
See [AGENTS.md](AGENTS.md) for detailed commands. Unit tests need no CA service or containers;
the separate integration suite still depends on hosted services.

### Revival baseline

The dated build/test results and known blockers are maintained in
[AGENTS.md's revival baseline](AGENTS.md#known-revival-baseline-and-pitfalls).
Passing unit tests do not establish current CA interoperability.

### Revival priorities

1. Add local Pebble integration tests to the maintained CI pipeline.
2. Fix order polling, certificate-chain export, alternate-chain handling, and
   CLI secret-file handling.
3. Refresh dependencies, validate package consumption, and update documentation
   before the next release; prereleases are optional when useful for validation.
4. Evaluate renewal information, certificate profiles, and IP identifiers after
   the reliability baseline is established.

## License

[MIT](LICENSE).
