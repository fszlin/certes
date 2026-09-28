using System.Globalization;
using System.Text;
using System.Xml.Linq;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --file scripts/Coverage/Summary.cs -- <coverage.cobertura.xml>");
    return 1;
}

try
{
    var report = Path.GetFullPath(args[0]);
    var root = XDocument.Load(report).Root
        ?? throw new InvalidDataException("Coverage report has no root element.");
    var packages = root.Element("packages")?.Elements("package").ToArray() ?? [];
    var expected = new HashSet<string> { "Certes", "dotnet-certes" };
    if (packages.Length != 2 || !expected.SetEquals(packages.Select(p => (string?)p.Attribute("name") ?? "")))
    {
        throw new InvalidDataException("Coverage must contain exactly Certes and dotnet-certes.");
    }

    if ((int?)root.Attribute("lines-valid") is not > 0)
    {
        throw new InvalidDataException("Coverage report contains no executable lines.");
    }

    foreach (var package in packages)
    {
        if (!package.Elements("classes").Elements("class").Elements("lines").Elements("line").Any())
        {
            throw new InvalidDataException($"No executable lines for {package.Attribute("name")?.Value}.");
        }
    }

    var summary = new StringBuilder();
    summary.AppendLine("## Unit-test coverage (.NET 10, Linux)");
    summary.AppendLine();
    summary.AppendLine("| Assembly | Line coverage | Branch coverage |");
    summary.AppendLine("| --- | ---: | ---: |");
    AppendRow("Total", root);
    foreach (var package in packages.OrderBy(p => (string?)p.Attribute("name"), StringComparer.Ordinal))
    {
        AppendRow(package.Attribute("name")!.Value, package);
    }

    summary.AppendLine();
    summary.AppendLine("Scope: Debug builds of the library and CLI exercised by the offline unit suite.");
    summary.AppendLine("Test assemblies, dependencies, generated-code attributes, and explicitly");
    summary.AppendLine("excluded code are omitted. Integration tests and other target frameworks");
    summary.AppendLine("are not included. Percentages are informational; no coverage threshold is enforced.");
    summary.AppendLine();
    summary.AppendLine("Download `unit-coverage-net10-linux` for the Cobertura XML and this summary.");

    var text = summary.ToString();
    File.WriteAllText(Path.Combine(Path.GetDirectoryName(report)!, "summary.md"), text);
    Console.Write(text);
    if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } destination)
    {
        File.AppendAllText(destination, text);
    }

    return 0;

    void AppendRow(string name, XElement element)
    {
        summary.AppendLine($"| {name} | {Percentage(element, "line-rate")} | {Percentage(element, "branch-rate")} |");
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Coverage summary failed: {exception.Message}");
    return 1;
}

static string Percentage(XElement element, string attribute)
{
    var rate = double.Parse(element.Attribute(attribute)?.Value ?? "", CultureInfo.InvariantCulture);
    if (!double.IsFinite(rate) || rate < 0 || rate > 1)
    {
        throw new InvalidDataException($"Invalid coverage rate: {attribute}.");
    }

    return (rate * 100).ToString("F2", CultureInfo.InvariantCulture) + "%";
}
