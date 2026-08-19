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
    subtype = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtype.cs")
    sync = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtypeSelectionSync.cs")
    quick = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs")
    grid = read("src/QS3D.BricsCAD.V25/GridCommands.cs")

    failures = []
    require(subtype, 'private static readonly string[] GridFamilySubtypes', "grid subtype catalog", failures)
    require(subtype, '"Lưới Thẳng", "Lưới Cong"', "straight/curved grid leaves", failures)
    require(subtype, 'if (subtypeCategory.HasValue) category = subtypeCategory.Value;', "subtype category routing", failures)
    require(subtype, 'return ElementCategory.Grid;', "grid category resolution", failures)
    require(subtype, 'family.Properties["GridRadiusM"] = "0.5";', "curved grid radius default", failures)
    require(sync, 'var inferred = InferWorkspaceSubtype(family);', "programmatic subtype sync", failures)
    require(quick, 'var command = isGrid ? "QS3DGRID"', "workspace grid quick route", failures)
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
