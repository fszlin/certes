# Unit-test coverage

The Linux build job wraps the full offline .NET 10 unit suite with Microsoft's
`dotnet-coverage`, pinned in `.config/dotnet-tools.json`. Windows and macOS run
the same tests without instrumentation. Coverlet is not required by either test
project. The collector runs independently of the VSTest package references.

The job summary shows line and branch coverage for `Certes` and `dotnet-certes`.
Download the `unit-coverage-net10-linux` Actions artifact for `summary.md` and
`coverage.cobertura.xml` (retained for 14 days). Cobertura-compatible viewers can
use the XML for per-file and per-line detail. No external coverage service or
credentials are needed.

## Reproduce locally (Linux x64, .NET 10 SDK, Python 3)

From the repository root:

```sh
dotnet tool restore
dotnet build test/Certes.Tests.Integration/Certes.Tests.Integration.csproj -p:SkipSigning=true
dotnet coverage collect --settings scripts/Coverage/settings.xml --output artifacts/coverage/coverage.cobertura.xml --output-format cobertura "dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true --no-build --no-restore"
python3 scripts/Coverage/summary.py artifacts/coverage/coverage.cobertura.xml
```

The test command's exit status is propagated by the collector. The summary
validator rejects empty reports or reports missing either shipping assembly.
Run its checks with `python3 -B -m unittest discover -s scripts/Coverage`.

## Interpretation

Coverage is informational, with no percentage gate or baseline comparison.
It describes Debug builds exercised by the offline unit suite on .NET 10/Linux;
it does not measure Pebble integration, `net462`, or the other library assets.
Only the two shipping assemblies are included. Generated-code attributes and
`ExcludeFromCodeCoverage` are excluded; compiler-generated async state machines
are not excluded wholesale. Branch coverage is the collector's branch metric.

Removing the old project-level collector means `--collect:"XPlat Code Coverage"`
is no longer supported out of the box; use the wrapper command above instead.
