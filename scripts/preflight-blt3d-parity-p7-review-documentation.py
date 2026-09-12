#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
WIRED = ("view", "quantity", "revision")
REFERENCE_MINIMUM = ("drawing-manager",)
EVIDENCE_ORDER = {
    "ReferenceCaptured": 0,
    "UiPresent": 1,
    "CommandWired": 2,
    "SemanticBehaviorPass": 3,
    "SaveReopenPass": 4,
    "V25V26ParityPass": 5,
}


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
            fail(f"manifest missing P7 wired feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "CommandWired":
            fail(f"P7 wired feature is not canonical Applicable/CommandWired: {feature_id}")
    for feature_id in REFERENCE_MINIMUM:
        fields = rows.get(feature_id)
        if fields is None:
            fail(f"manifest missing P7 reference feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable":
            fail(f"P7 reference feature lost canonical applicability/workflow identity: {feature_id}")
        stage = fields[5]
        if stage not in EVIDENCE_ORDER or EVIDENCE_ORDER[stage] < EVIDENCE_ORDER["ReferenceCaptured"]:
            fail(f"P7 reference feature regressed below ReferenceCaptured: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParityReviewDocumentationCatalog.cs")
    require(catalog, "public static class ParityReviewDocumentationCatalog", "P7 catalog")
    require(catalog, "public static ParityWorkflowRegistry CreateRegistry()", "P7 catalog")
    for feature_id in WIRED:
        require(catalog, f'new FeatureId("{feature_id}")', "P7 catalog")
    for token in (
        "ParityWorkflowKind.ReadOnly",
        "ParityWorkflowSurface.Ui",
        "ParityWorkflowRequirement.ActiveDocument",
    ):
        require(catalog, token, "P7 catalog")
    for forbidden in (
        'new FeatureId("drawing-manager")', "Bricscad.", "Teigha.",
        "System.Windows", "ParityWorkflowSurface.Mcp", "ParityWorkflowSurface.Launcher",
        "ParityWorkflowKind.SemanticMutation",
    ):
        if forbidden in catalog:
            fail(f"P7 host-neutral catalog contains forbidden token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P7ReviewDocumentationEvidenceRules();", "P7 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P7ReviewDocumentationBindings();", "P7 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 3", "P7 workflow smoke")

    command_sources = {
        "view": ("src/QS3D.BricsCAD.V25/ViewportCommands.cs", 'CommandMethod("QS3DVIEW3D"'),
        "quantity": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DBQ"'),
        "revision": ("src/QS3D.BricsCAD.V25/ReviewCommands.cs", 'CommandMethod("QS3DREVDIFF"'),
    }
    for feature_id, (relative, token) in command_sources.items():
        require(read(relative), token, f"P7 command evidence {feature_id}")

    project_browser = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs")
    require(project_browser, 'Header = "Project Browser"', "P7 drawing-manager inventory")
    sheet_commands = read("src/QS3D.BricsCAD.V25/SemanticSheetCommands.cs")
    require(sheet_commands, 'CommandMethod("QS3DSHEETBUILD"', "P7 drawing-manager inventory")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p7-review-documentation.md")
    require(runbook, "`CommandWired`", "P7 runbook")
    require(runbook, "`ReferenceCaptured`", "P7 runbook")
    require(runbook, "`# catalog-complete=false`", "P7 runbook")
    require(runbook, "clean-room", "P7 clean-room boundary")

    print("PASS: P7 review/documentation parity evidence is source-anchored and keeps Drawing Manager fail-closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
