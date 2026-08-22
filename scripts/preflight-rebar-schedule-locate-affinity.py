#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RebarScheduleWindow.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "var currentRow = ResolveCurrentRow(row);",
        "_locate(currentRow);",
        "private RebarScheduleRow ResolveCurrentRow(RebarScheduleRow displayedRow)",
        "private IReadOnlyList<RebarScheduleRow> BuildCurrentRows()",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
        "string.Equals(x.ElementId, displayedRow.ElementId, StringComparison.OrdinalIgnoreCase)",
        "string.Equals(x.BarMark, displayedRow.BarMark, StringComparison.OrdinalIgnoreCase)",
        "if (matches.Count != 1)",
        "if (!SameRow(displayedRow, matches[0]))",
        "_rows = BuildCurrentRows();",
        "left.DiameterMm.Equals(right.DiameterMm)",
        "left.Quantity == right.Quantity",
        "left.CuttingLengthM.Equals(right.CuttingLengthM)",
        "left.TotalWeightKg.Equals(right.TotalWeightKg)",
        "string.Equals(left.FabricationDetailingRevision, right.FabricationDetailingRevision, StringComparison.Ordinal)",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        print("ERROR: BBS modeless locate-affinity contract is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    locate_pos = text.find("private void Locate()")
    resolve_pos = text.find("var currentRow = ResolveCurrentRow(row);", locate_pos)
    callback_pos = text.find("_locate(currentRow);", locate_pos)
    if locate_pos < 0 or resolve_pos < 0 or callback_pos < 0 or not (locate_pos < resolve_pos < callback_pos):
        print("ERROR: BBS Locate must revalidate the displayed row before invoking the locate callback.")
        return 1

    if "_locate(row);" in text:
        print("ERROR: stale displayed BBS rows must not be passed directly to the locate callback.")
        return 1

    print("PASS: BBS Locate revalidates the unique live ElementId/BarMark row and its full visible schedule state.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
