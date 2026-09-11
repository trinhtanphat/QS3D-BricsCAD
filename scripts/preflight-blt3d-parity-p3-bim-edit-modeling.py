#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
REQUIRED_IDS = ("bim.authoring", "draw", "tool.editing", "modeling")
BASELINE_GUARDS = (
    "scripts/preflight-blt3d-bim-workspace.py",
    "scripts/preflight-blt-draw-ribbon.py",
    "scripts/preflight-direct-draw-authoring-integration.py",
    "scripts/preflight-modeling-ribbon-functions.py",
    "scripts/preflight-blt3d-tool-ribbon.py",
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


def parse_manifest():
    lines = read("docs/BLT3D-PARITY-MANIFEST.tsv").splitlines()
    if not lines or lines[0] != "# catalog-complete=false":
        fail("manifest must remain fail-closed with exact # catalog-complete=false metadata")
    if len(lines) < 2 or lines[1] != MANIFEST_HEADER:
        fail("manifest canonical header changed")
    rows = {}
    for line_number, line in enumerate(lines[2:], start=3):
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if len(fields) != 8:
            fail(f"manifest line {line_number} must contain exactly eight tab-separated fields")
        if fields[0] in rows:
            fail(f"manifest contains duplicate FeatureId: {fields[0]}")
        rows[fields[0]] = fields
    return rows


def main():
    rows = parse_manifest()
    for feature_id in REQUIRED_IDS:
        if feature_id not in rows:
            fail(f"manifest missing P3 feature: {feature_id}")
        fields = rows[feature_id]
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "CommandWired":
            fail(f"P3 feature must be Applicable/CommandWired with canonical workflow key: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParityBimEditingModelingCatalog.cs")
    for token in (
        'new FeatureId("bim.authoring")', 'new FeatureId("draw")',
        'new FeatureId("tool.editing")', 'new FeatureId("modeling")',
        "public static ParityWorkflowRegistry CreateRegistry()",
        "ParityWorkflowKind.SemanticMutation", "ParityWorkflowSurface.Ui",
        "ParityWorkflowRequirement.ActiveDocument", "ParityWorkflowRequirement.Project",
        "ParityWorkflowRequirement.Zone", "ParityWorkflowRequirement.Floor",
        "ParityWorkflowRequirement.Family", "ParityWorkflowRequirement.Selection",
        "ParityWorkflowRequirement.AtomicMutation", "ParityWorkflowRequirement.Audit",
    ):
        require(catalog, token, "P3 workflow catalog")
    for forbidden in ("Bricscad.", "Teigha.", "System.Windows", "ParityWorkflowSurface.Mcp"):
        if forbidden in catalog:
            fail(f"P3 host-neutral catalog contains unsupported host/surface token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P3BimEditingModelingEvidenceRules();", "P3 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P3BimEditingModelingBindings();", "P3 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 4", "P3 workflow smoke")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p3-bim-edit-modeling.md")
    require(runbook, "`CommandWired`", "P3 runbook")
    require(runbook, "`# catalog-complete=false`", "P3 runbook")
    for feature_id in REQUIRED_IDS:
        require(runbook, f"`{feature_id}`", "P3 runbook feature evidence")
    for guard in BASELINE_GUARDS:
        require(runbook, f"`{guard}`", "P3 runbook baseline evidence")
        read(guard)

    print("PASS: P3 BIM/edit/modeling parity evidence is fail-closed, host-neutral, command-wired, and anchored to existing qualified guards.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
