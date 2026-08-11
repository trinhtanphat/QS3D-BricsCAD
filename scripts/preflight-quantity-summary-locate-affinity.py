#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "var currentRow = ResolveCurrentRow(row);",
        "_locate(currentRow);",
        "private QuantityReportRow ResolveCurrentRow(QuantityReportRow displayedRow)",
        "if (_recalculate == null)",
        "var displayedIds = CanonicalIds(displayedRow.ElementIds);",
        "if (displayedIds.Length == 0)",
        "var currentRows = _recalculate() ?? Array.Empty<QuantityReportRow>();",
        "SameElementIdentity(displayedIds, x)",
        "if (matches.Count != 1)",
        "if (!SameRow(displayedRow, matches[0]))",
        "expectedIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase)",
        "string.Equals(left.DrawingFingerprint, right.DrawingFingerprint, StringComparison.Ordinal)",
        "left.NetConcreteM3.Equals(right.NetConcreteM3)",
        "left.FormworkM2.Equals(right.FormworkM2)",
        "left.LengthM.Equals(right.LengthM)",
        "Nullable.Equals(left.DensityKgM3, right.DensityKgM3)",
        "Nullable.Equals(left.MassKg, right.MassKg)",
        "CanonicalIds(left.SourceHandles).SequenceEqual(CanonicalIds(right.SourceHandles), StringComparer.OrdinalIgnoreCase)",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        print("ERROR: BQ modeless locate-affinity contract is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    locate_pos = text.find("private void LocateCurrent()")
    resolve_pos = text.find("var currentRow = ResolveCurrentRow(row);", locate_pos)
    callback_pos = text.find("_locate(currentRow);", locate_pos)
    if locate_pos < 0 or resolve_pos < 0 or callback_pos < 0 or not (locate_pos < resolve_pos < callback_pos):
        print("ERROR: BQ Locate must revalidate the displayed row before invoking the locate callback.")
        return 1

    if "_locate(row);" in text:
        print("ERROR: stale displayed BQ rows must not be passed directly to the locate callback.")
        return 1

    print("PASS: BQ Locate revalidates canonical semantic element identity and full live row state before CAD selection.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
