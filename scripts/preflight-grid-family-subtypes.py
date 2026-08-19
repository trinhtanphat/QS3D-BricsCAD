#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + needle)


def main():
    subtype = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.GridFamilySubtype.cs")
    sync = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtypeSelectionSync.cs")
    quick = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs")
    grid = read("src/QS3D.BricsCAD.V25/GridCommands.cs")

    failures = []
    require(subtype, 'private static readonly string[] GridFamilySubtypes', "grid subtype catalog", failures)
    require(subtype, '"Lưới Thẳng", "Lưới Cong"', "straight/curved grid leaves", failures)
    require(subtype, 'ElementCategory.Grid', "grid category routing", failures)
    require(subtype, 'SeedGridDefault(family, "GridRadiusM", "0.5");', "curved grid 500 mm radius default", failures)
    require(subtype, 'ApplyGridFamilySubtypeFilter();', "grid family subtype filtering", failures)
    require(sync, 'family.Category == ElementCategory.Grid', "programmatic grid subtype sync", failures)
    require(sync, 'InferFoundationSubtype(family.Name)', "legacy foundation subtype sync", failures)
    require(quick, 'Send("QS3DGRID");', "workspace grid quick route", failures)
    require(quick, 'var command = advanced ? "QS3DDRAWACTIVEADV" : "QS3DDRAWACTIVE";', "existing active-family draw route", failures)
    require(grid, 'Cad.CadSelectionGuard.AcquireCurrentSelection(document)', "interactive grid selection", failures)
    require(grid, 'return string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase);', "curved grid ARC contract", failures)
    require(grid, 'return string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase);', "straight grid LINE contract", failures)

    if failures:
        print("Grid family subtype preflight FAILED:")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Workspace Grid subtype routing preserves Lưới Thẳng/Lưới Cong and routes capture by LINE/ARC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
