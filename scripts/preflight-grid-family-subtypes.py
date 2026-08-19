#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + needle)


def forbid(text, needle, label, failures):
    if needle in text:
        failures.append(label + ": forbidden " + needle)


def main():
    subtype = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.GridFamilySubtype.cs")
    sync = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtypeSelectionSync.cs")
    refresh = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtypeRefreshSync.cs")
    recovery = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dParameterModeRecovery.cs")
    quick = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs")
    grid = read("src/QS3D.BricsCAD.V25/GridCommands.cs")

    failures = []
    require(subtype, 'private static readonly string[] GridFamilySubtypes', "grid subtype catalog", failures)
    require(subtype, '"Lưới Thẳng", "Lưới Cong"', "straight/curved grid leaves", failures)
    require(subtype, 'ElementCategory.Grid', "grid category routing", failures)
    require(subtype, 'SeedGridDefault(family, "GridRadiusM", "0.5");', "curved grid 500 mm radius default", failures)
    require(subtype, 'ApplyGridFamilySubtypeFilter();', "grid family subtype filtering", failures)

    require(subtype, 'AttachFamilySubtypeInteractions();', "base subtype handlers must be attached before Grid routing replaces shared handlers", failures)
    require(subtype, 'ModelTree.SelectedItemChanged -= OnFamilySubtypeTreeSelectionChanged;', "Grid routing must detach Foundation-only tree handler", failures)
    require(subtype, 'FamilySearch.TextChanged -= OnFamilySubtypeSearchChanged;', "Grid routing must detach Foundation-only search handler", failures)
    require(subtype, 'ModelTree.SelectedItemChanged += OnWorkspaceFamilySubtypeTreeSelectionChanged;', "subtype-aware tree dispatcher", failures)
    require(subtype, 'FamilySearch.TextChanged += OnWorkspaceFamilySubtypeSearchChanged;', "subtype-aware search dispatcher", failures)
    require(subtype, 'if (ResolveGridSubtype(item).Length > 0)', "Grid tree selection must be handled before Foundation fallback", failures)
    require(subtype, 'OnFamilySubtypeTreeSelectionChanged(sender, e);', "non-Grid tree selection must preserve legacy Foundation routing", failures)
    require(subtype, 'if (IsGridSubtype(_familySubtypeFilter))', "Grid search must bypass Foundation-only filtering", failures)
    require(subtype, 'OnFamilySubtypeSearchChanged(sender, e);', "non-Grid search must preserve legacy routing", failures)

    require(subtype, 'e.Handled = true;\n            CreateGridFamilyFromWorkspaceSubtype(false);', "Grid Add must create the selected subtype Family directly", failures)
    require(subtype, 'FamilyList.SelectedItem = live;', "new Grid Family must be selected after commit", failures)
    require(subtype, 'if (live != null) _viewModel.ShowFamilyProperties();', "new Grid Family must show inline Properties", failures)
    forbid(subtype, 'CreateMenuItem("Tham số"', "Grid Add must not open a parameter mode chooser", failures)
    forbid(subtype, 'CreateMenuItem("Solid3D"', "Grid Add must not open a Solid3D mode chooser", failures)
    forbid(subtype, 'menu.IsOpen = true;', "Grid Add must not open a context menu", failures)

    require(sync, 'family.Category == ElementCategory.Grid', "programmatic grid subtype sync", failures)
    require(sync, 'InferFoundationSubtype(family.Name)', "legacy foundation subtype sync", failures)

    require(refresh, 'if (IsGridSubtype(_familySubtypeFilter))', "same-document refresh must detect Grid subtype", failures)
    require(refresh, 'ApplyGridFamilySubtypeFilter();', "same-document refresh must preserve Grid subtype filtering", failures)
    require(refresh, 'else\n                        ApplyFamilySubtypeFilter();', "same-document refresh must retain Foundation fallback", failures)

    require(recovery, 'if (IsGridSubtype(panel._familySubtypeFilter))', "BLT3D parameter recovery must recognize Grid subtype", failures)
    require(recovery, 'panel.ApplyGridFamilySubtypeFilter();', "BLT3D parameter recovery must use Grid filter", failures)
    require(recovery, 'panel.ApplyFamilySubtypeFilter();', "BLT3D parameter recovery must retain non-Grid fallback", failures)

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

    print("PASS: Workspace Grid subtype routing preserves direct Add -> Family -> inline Properties, refresh filtering, and LINE/ARC capture contracts.")
    return 0


if __name__ == "__main__":
    sys.exit(main())