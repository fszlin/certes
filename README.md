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
reproducible build and reliable tests on supported .NET runtimes, followed by
focused protocol hardening and dependency modernization.

The current checkout still targets:

| Component | Targets |
| --- | --- |
| Library | `net10.0`, `net8.0`, `netstandard2.0` |
| CLI | `net10.0` |
| Unit and integration tests | `net10.0`, `net462` |
| Azure Functions test helper | `net7.0` |

Development and the CLI now use .NET 10 LTS. CI installs the latest `10.0.x` SDK
and logs the resolved version.
The unused Functions helper remains on out-of-support .NET 7 pending retirement.
Targets describe this checkout, not necessarily the latest published packages.

The library retains .NET Standard 2.0 compatibility assets; consumers on .NET 8/9
select `net8.0`, while .NET 6/7 use `netstandard2.0` instead of a dedicated
`net6.0` build. CI runs package smoke checks on .NET 8 and 10 and compiles a
.NET 6 consumer but does not run it on the unsupported .NET 6 runtime. The next
CLI package requires .NET 10; this is a runtime requirement change for existing
CLI users. Azure deployment commands were removed from the CLI; use dedicated
Azure tooling for DNS/app deployment workflows.

.NET 8 remains supported by Microsoft until November 10, 2026. Its library
asset supports existing consumers; .NET 10 is the development and CLI baseline.

## Upgrade guide

If you automate `dotnet-certes`, review these changes before upgrading:

- CLI runtime now requires .NET 10.
- The `az` command group was removed (`az set`, `az dns`, `az app`). Use dedicated Azure tooling for DNS/app deployment flows.
- The CLI parser migration is complete; legacy `System.CommandLine` command paths were removed in favor of the Spectre runtime path.

If you build/extend Certes from source:

- The library dependency moved from `Portable.BouncyCastle` to `BouncyCastle.Cryptography`.
- Tests and helper code now follow the newer BouncyCastle APIs and stricter DN validation semantics.

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

The [GitHub Actions workflow](.github/workflows/build.yml) checks
cross-platform compilation, the full offline unit suite, package smoke
consumption, and local Pebble integration. Tests run directly on .NET 10. See
[CI migration status](docs/ci-migration.md) for coverage and
remaining work. Legacy build/release automation is disabled.

Run commands from the repository root:

```sh
dotnet build src/Certes/Certes.csproj
dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true
```

Install a .NET 10 SDK; no runtime roll-forward is needed.
See [AGENTS.md](AGENTS.md) for detailed commands. Unit tests need no CA service or containers;
the separate integration suite uses the [local Pebble container stack](scripts/Pebble/README.md).

### Revival baseline

The dated build/test results and known blockers are maintained in
[AGENTS.md's revival baseline](AGENTS.md#known-revival-baseline-and-pitfalls).
Passing unit tests do not establish current CA interoperability.

### Revival priorities

1. Harden order lifecycle behavior (polling, retry budgets, and failure paths)
   with focused unit and integration coverage.
2. Refresh remaining dependencies and modernize CLI provider integrations while
   keeping provider-specific dependencies out of the core library.
3. Finalize release workflow and documentation updates for repeatable packaging
   and verification.
4. Evaluate renewal information, certificate profiles, and IP identifiers after
   the reliability baseline is established.

## License

[MIT](LICENSE).
