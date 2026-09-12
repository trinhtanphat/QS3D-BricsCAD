#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
WIRED = (
    "room", "room.finish", "earthwork", "stair",
    "railing", "curtain", "door-opening", "grid",
)
REFERENCE_ONLY = (
    "pile-cap", "pile-cap.rebar", "pile.drop-to-cap", "steel-detail",
    "copy-to-level", "blinding-concrete", "opening-to-slab",
)


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
            fail(f"manifest missing P6 wired feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "CommandWired":
            fail(f"P6 wired feature is not canonical Applicable/CommandWired: {feature_id}")
    for feature_id in REFERENCE_ONLY:
        fields = rows.get(feature_id)
        if fields is None:
            fail(f"manifest missing P6 reference feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "ReferenceCaptured":
            fail(f"P6 reference-only feature advanced beyond evidence ceiling: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParitySpecialCategoriesCatalog.cs")
    require(catalog, "public static class ParitySpecialCategoriesCatalog", "P6 catalog")
    require(catalog, "public static ParityWorkflowRegistry CreateRegistry()", "P6 catalog")
    for feature_id in WIRED:
        require(catalog, f'new FeatureId("{feature_id}")', "P6 catalog")
    for token in (
        "ParityWorkflowKind.SemanticMutation",
        "ParityWorkflowSurface.Ui",
        "ParityWorkflowRequirement.ActiveDocument",
        "ParityWorkflowRequirement.Project",
        "ParityWorkflowRequirement.Selection",
        "ParityWorkflowRequirement.AtomicMutation",
        "ParityWorkflowRequirement.Audit",
    ):
        require(catalog, token, "P6 catalog")
    for forbidden in ("Bricscad.", "Teigha.", "System.Windows", "ParityWorkflowSurface.Mcp", "ParityWorkflowSurface.Launcher"):
        if forbidden in catalog:
            fail(f"P6 host-neutral catalog contains forbidden host/surface token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P6SpecialCategoryEvidenceRules();", "P6 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P6SpecialCategoryBindings();", "P6 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 8", "P6 workflow smoke")

    command_sources = {
        "room": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DROOM"'),
        "room.finish": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DFINISH"'),
        "earthwork": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DEARTHWORK"'),
        "stair": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DSTAIR"'),
        "railing": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DRAILING"'),
        "curtain": ("docs/COMMANDS.md", "`QS3DCURTAIN`"),
        "door-opening": ("src/QS3D.BricsCAD.V25/Commands.cs", 'CommandMethod("QS3DDOOR"'),
        "grid": ("src/QS3D.BricsCAD.V25/GridCommands.cs", 'CommandMethod("QS3DGRID"'),
    }
    for feature_id, (relative, token) in command_sources.items():
        require(read(relative), token, f"P6 command evidence {feature_id}")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p6-special-categories.md")
    require(runbook, "`CommandWired`", "P6 runbook")
    require(runbook, "`ReferenceCaptured`", "P6 runbook")
    require(runbook, "`# catalog-complete=false`", "P6 runbook")
    require(runbook, "61 XAML", "P6 recovery inventory evidence")
    require(runbook, "clean-room", "P6 clean-room boundary")

    print("PASS: P6 special-category parity evidence is fail-closed, source-anchored, and does not overclaim reference-only workflows.")
    return 0


if __name__ == "__main__":
    sys.exit(main())