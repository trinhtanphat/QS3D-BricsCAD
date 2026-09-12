#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
WIRED = ("drawing-manager", "ifc")


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: missing required token: {needle}")


def manifest_rows():
    lines = read("docs/BLT3D-PARITY-MANIFEST.tsv").splitlines()
    if not lines or lines[0] != "# catalog-complete=false":
        fail("manifest must remain fail-closed")
    if len(lines) < 2 or lines[1] != HEADER:
        fail("manifest canonical header changed")
    rows = {}
    for line_number, line in enumerate(lines[2:], start=3):
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if len(fields) != 8:
            fail(f"manifest line {line_number} must contain exactly eight fields")
        if fields[0] in rows:
            fail(f"duplicate FeatureId: {fields[0]}")
        rows[fields[0]] = fields
    return rows


def main():
    rows = manifest_rows()
    for feature_id in WIRED:
        fields = rows.get(feature_id)
        if fields is None:
            fail(f"manifest missing P8 wired feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "CommandWired":
            fail(f"P8 wired feature is not canonical Applicable/CommandWired: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParityDrawingInteroperabilityCatalog.cs")
    require(catalog, "public static class ParityDrawingInteroperabilityCatalog", "P8 catalog")
    require(catalog, "public static ParityWorkflowRegistry CreateRegistry()", "P8 catalog")
    for feature_id in WIRED:
        require(catalog, f'new FeatureId("{feature_id}")', "P8 catalog")
    for token in (
        "ParityWorkflowKind.Infrastructure",
        "ParityWorkflowSurface.Ui",
        "ParityWorkflowRequirement.ActiveDocument",
    ):
        require(catalog, token, "P8 catalog")
    for forbidden in (
        "Bricscad.", "Teigha.", "System.Windows",
        "ParityWorkflowSurface.Mcp", "ParityWorkflowSurface.Launcher",
        "ParityWorkflowKind.SemanticMutation",
    ):
        if forbidden in catalog:
            fail(f"P8 host-neutral catalog contains forbidden token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P8DrawingInteroperabilityEvidenceRules();", "P8 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P8DrawingInteroperabilityBindings();", "P8 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 2", "P8 workflow smoke")

    right_shell = read("src/QS3D.BricsCAD.V25/UI/RightPanel.Blt3dReferenceShell.cs")
    require(right_shell, '"QUẢN LÝ BẢN VẼ"', "P8 Drawing Manager shell")
    right_panel = read("src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs")
    for token in ('Send("_XATTACH")', "XrefService.Reload", 'TrySend(doc, "_MOVE")', "XrefService.Detach"):
        require(right_panel, token, "P8 Drawing Manager lifecycle")
    xref_service = read("src/QS3D.BricsCAD.V25/Cad/XrefService.cs")
    require(xref_service, "SetInstanceLayersLocked", "P8 Drawing Manager Xref service")

    ifc_source = read("src/QS3D.BricsCAD.V25/ReferenceUiCommands.cs")
    for token in (
        'CommandMethod("QS3DIFCIMPORT"',
        'CommandMethod("QS3DIFCIMPORTLIGHT"',
        'CommandMethod("QS3DIFCREMOVE"',
        'CommandMethod("QS3DIFCEXPORT"',
        'Forward("_IFCEXPORT"',
    ):
        require(ifc_source, token, "P8 IFC command evidence")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p8-drawing-ifc.md")
    require(runbook, "`CommandWired`", "P8 runbook")
    require(runbook, "`# catalog-complete=false`", "P8 runbook")
    require(runbook, "clean-room", "P8 clean-room boundary")
    require(runbook, "round-trip", "P8 evidence ceiling")
    require(runbook, "rename", "P8 Drawing Manager evidence ceiling")
    require(runbook, "clip", "P8 Drawing Manager evidence ceiling")

    print("PASS: P8 Drawing Manager/IFC parity evidence is source-anchored and remains host-native/fail-closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
