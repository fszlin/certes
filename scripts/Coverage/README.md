# Unit-test coverage

The Linux build job wraps the full offline .NET 10 unit suite with Microsoft's
`dotnet-coverage`; ReportGenerator produces Markdown and HTML reports. Both tools
are pinned in `.config/dotnet-tools.json`. Windows and macOS run
the same tests without instrumentation. Coverlet is not required by either test
project. The collector runs independently of the VSTest package references.

The job summary shows line and branch coverage for `Certes` and `dotnet-certes`.
Download and extract the `unit-coverage-net10-linux` Actions artifact (retained
for 14 days), then open `report/index.html` to browse per-file and per-line detail.
It also contains `report/SummaryGithub.md` and `coverage.cobertura.xml` for other
coverage viewers. No external coverage service or credentials are needed.

## Reproduce locally (Linux x64, .NET 10 SDK)

From the repository root:

```sh
dotnet tool restore
dotnet build test/Certes.Tests.Integration/Certes.Tests.Integration.csproj -p:SkipSigning=true
dotnet coverage collect --settings scripts/Coverage/settings.xml --output artifacts/coverage/coverage.cobertura.xml --output-format cobertura "dotnet test test/Certes.Tests/Certes.Tests.csproj -f net10.0 -p:SkipSigning=true --no-build --no-restore"
dotnet reportgenerator -reports:artifacts/coverage/coverage.cobertura.xml -targetdir:artifacts/coverage/report '-reporttypes:Html;MarkdownSummaryGithub'
```

The test command's exit status is propagated by the collector. Reporting uses
ReportGenerator directly, without a custom reporting script or additional
scripting runtime. Inspect the report for both shipping assemblies when changing
the collection settings; there is no custom report validator.

## Interpretation

Coverage is informational, with no percentage gate or baseline comparison.
It describes Debug builds exercised by the offline unit suite on .NET 10/Linux;
it does not measure Pebble integration, `net462`, or the other library assets.
Only the two shipping assemblies are included. Generated-code attributes and
`ExcludeFromCodeCoverage` are excluded; compiler-generated async state machines
are not excluded wholesale. Displayed percentages are calculated by
ReportGenerator from the Cobertura line/branch entries; they can differ from the
collector's aggregate rate attributes because of aggregation of class/generic
instantiation entries. Use the ReportGenerator summary consistently for comparison.

Removing the old project-level collector means `--collect:"XPlat Code Coverage"`
is no longer supported out of the box; use the wrapper command above instead.
