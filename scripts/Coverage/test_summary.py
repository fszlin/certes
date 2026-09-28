"""Failure-path checks for the CI coverage report validator."""

import io
import unittest

from summary import summarize


REPORT = """<coverage lines-valid="4" line-rate="0.75" branch-rate="0.5">
<packages>
  <package name="Certes" line-rate="1" branch-rate="1">
    <classes><class><lines><line number="1" hits="1"/></lines></class></classes>
  </package>
  <package name="dotnet-certes" line-rate="0.5" branch-rate="0">
    <classes><class><lines><line number="1" hits="0"/></lines></class></classes>
  </package>
</packages></coverage>"""


class SummaryTests(unittest.TestCase):
    def test_preserves_aggregate_and_per_assembly_rates(self):
        result = summarize(io.StringIO(REPORT))
        self.assertIn("| Total | 75.00% | 50.00% |", result)
        self.assertIn("| Certes | 100.00% | 100.00% |", result)
        self.assertIn("| dotnet-certes | 50.00% | 0.00% |", result)

    def test_rejects_missing_or_unexpected_assembly(self):
        with self.assertRaises(ValueError):
            summarize(io.StringIO(REPORT.replace('name="Certes"', 'name="Tests"')))

    def test_rejects_empty_report(self):
        with self.assertRaises(ValueError):
            summarize(io.StringIO(REPORT.replace('lines-valid="4"', 'lines-valid="0"')))

    def test_rejects_assembly_without_lines(self):
        with self.assertRaises(ValueError):
            summarize(io.StringIO(REPORT.replace('<line number="1" hits="0"/>', "")))

    def test_rejects_invalid_rates(self):
        with self.assertRaises(ValueError):
            summarize(io.StringIO(REPORT.replace('line-rate="0.75"', 'line-rate="NaN"')))


if __name__ == "__main__":
    unittest.main()
