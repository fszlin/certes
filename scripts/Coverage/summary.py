"""Summarize the two shipping assemblies from a dotnet-coverage Cobertura report."""

import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def summarize(report):
    root = ET.parse(report).getroot()
    packages = root.findall("./packages/package")
    expected = {"Certes", "dotnet-certes"}
    if len(packages) != 2 or {p.get("name") for p in packages} != expected:
        raise ValueError("Coverage must contain exactly Certes and dotnet-certes.")
    if int(root.attrib["lines-valid"]) <= 0:
        raise ValueError("Coverage report contains no executable lines.")
    for package in packages:
        if not package.findall("./classes/class/lines/line"):
            raise ValueError(f"No executable lines for {package.attrib['name']}.")

    rows = [
        "## Unit-test coverage (.NET 10, Linux)",
        "",
        "| Assembly | Line coverage | Branch coverage |",
        "| --- | ---: | ---: |",
    ]
    for name, element in [("Total", root)] + [
        (p.attrib["name"], p) for p in sorted(packages, key=lambda p: p.attrib["name"])
    ]:
        rates = [float(element.attrib[key]) for key in ("line-rate", "branch-rate")]
        if not all(0 <= rate <= 1 for rate in rates):
            raise ValueError(f"Invalid coverage rate for {name}.")
        rows.append(f"| {name} | {rates[0]:.2%} | {rates[1]:.2%} |")
    rows += [
        "",
        "Scope: Debug builds of the library and CLI exercised by the offline unit suite.",
        "Test assemblies, dependencies, generated-code attributes, and explicitly",
        "excluded code are omitted. Integration tests and other target frameworks",
        "are not included. Percentages are informational; no coverage threshold is enforced.",
        "",
        "Download `unit-coverage-net10-linux` for the Cobertura XML and this summary.",
        "",
    ]
    return "\n".join(rows)


def main():
    report = Path(sys.argv[1])
    summary = summarize(report)
    report.with_name("summary.md").write_text(summary, encoding="utf-8")
    print(summary)
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as output:
            output.write(summary)


if __name__ == "__main__":
    main()
