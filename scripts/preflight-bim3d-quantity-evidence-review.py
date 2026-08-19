#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"

METRICS = (
    ("GrossConcreteM3", "HasGrossConcreteM3Evidence"),
    ("DeductionM3", "HasDeductionM3Evidence"),
    ("NetConcreteM3", "HasNetConcreteM3Evidence"),
    ("FormworkM2", "HasFormworkM2Evidence"),
    ("LengthM", "HasLengthMEvidence"),
    ("OuterPerimeterM", "HasOuterPerimeterMEvidence"),
    ("InnerPerimeterM", "HasInnerPerimeterMEvidence"),
    ("DoorAreaM2", "HasDoorAreaM2Evidence"),
    ("SideAreaM2", "HasSideAreaM2Evidence"),
    ("BottomAreaM2", "HasBottomAreaM2Evidence"),
    ("TopAreaM2", "HasTopAreaM2Evidence"),
    ("OtherAreaM2", "HasOtherAreaM2Evidence"),
)


def require(text, needle, rel=WINDOW_REL):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def require_order(text, needles, rel=WINDOW_REL):
    cursor = -1
    for needle in needles:
        pos = text.find(needle, cursor + 1)
        if pos < 0:
            raise SystemExit(f"FAIL: {rel} missing ordered contract: {needle}")
        cursor = pos


def main():
    window = (ROOT / WINDOW_REL).read_text(encoding="utf-8")

    require_order(window, ("InitializeComponent();", "ConfigureEvidenceAwareMetricColumns();", "DocumentBoundWindowLifetime.Attach"))
    for needle in (
        "private void ConfigureEvidenceAwareMetricColumns()",
        "private void ConfigureEvidenceMetricColumn(string valuePath, string evidencePath)",
        "var columnIndex = Array.IndexOf(ColumnKeys, valuePath);",
        "!(QuantityGrid.Columns[columnIndex] is DataGridTextColumn column)",
        "new MultiBinding { Mode = BindingMode.OneWay, Converter = EvidenceMetricConverter.Instance }",
        "private sealed class EvidenceMetricConverter : IMultiValueConverter",
        "if (values == null || values.Length < 2 || !(values[1] is bool hasEvidence) || !hasEvidence)",
        'return "N/A";',
        'value.ToString("0.###", culture)',
        "private static string MetricText(double value, bool hasEvidence, string format)",
        'hasEvidence ? value.ToString(format, CultureInfo.CurrentCulture) : "N/A";',
        "var hasRows = filtered.Count > 0;",
    ):
        require(window, needle)

    for value, evidence in METRICS:
        require(
            window,
            f"ConfigureEvidenceMetricColumn(nameof(QuantityReportRow.{value}), nameof(QuantityReportRow.{evidence}));",
        )
        require(window, f"left.{evidence} == right.{evidence}")

    for evidence in (
        "HasNetConcreteM3Evidence",
        "HasFormworkM2Evidence",
        "HasLengthMEvidence",
        "HasDoorAreaM2Evidence",
    ):
        require(window, f"hasRows && filtered.All(x => x.{evidence})")

    for value, evidence in METRICS:
        require(window, f"MetricText(row.{value}, row.{evidence}, \"0.###\")")

    print(
        "PASS: BQ review renders unsupported metrics as N/A while preserving measured zeroes, "
        "keeps filtered totals conservative, and treats evidence-presence changes as stale-row changes."
    )


if __name__ == "__main__":
    main()
