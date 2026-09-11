#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
REQUIRED_IDS = (
    "shell.start",
    "shell.workspace",
    "shell.ribbon",
    "project.setup",
    "project.zone",
    "project.floor",
    "project.family",
)
BASELINE_GUARDS = (
    "scripts/preflight-blt3d-bim-workspace.py",
    "scripts/preflight-blt3d-workspace.py",
    "scripts/preflight-project-ribbon-actions.py",
    "scripts/preflight-project-setup-floor-reference.py",
    "scripts/preflight-family-manager-qs-quick-workflow.py",
    "scripts/preflight-workspace-family-command-affinity.py",
    "scripts/preflight-workspace-footer-context.py",
    "scripts/preflight-workspace-document-context.py",
    "scripts/preflight-project-floor-zone-canonical-reference.py",
    "scripts/preflight-project-floor-zone-mutation-integrity.py",
    "scripts/preflight-family-level-manager-publication.py",
    "scripts/preflight-family-level-manager-single-instance-veto-safe.py",
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
    text = read("docs/BLT3D-PARITY-MANIFEST.tsv")
    lines = text.splitlines()
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
        feature_id = fields[0]
        if feature_id in rows:
            fail(f"manifest contains duplicate FeatureId: {feature_id}")
        rows[feature_id] = fields
    return rows


def main():
    rows = parse_manifest()
    for feature_id in REQUIRED_IDS:
        if feature_id not in rows:
            fail(f"manifest missing P2 feature: {feature_id}")
        fields = rows[feature_id]
        if fields[3] != feature_id:
            fail(f"P2 workflow key must equal FeatureId: {feature_id}")
        if fields[4] != "Applicable":
            fail(f"P2 feature must remain applicable: {feature_id}")
        if fields[5] != "CommandWired":
            fail(f"P2 source evidence must be exactly CommandWired: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParityShellProjectNavigationCatalog.cs")
    for token in (
        'new FeatureId("shell.start")',
        'new FeatureId("shell.workspace")',
        'new FeatureId("shell.ribbon")',
        'new FeatureId("project.setup")',
        'new FeatureId("project.zone")',
        'new FeatureId("project.floor")',
        'new FeatureId("project.family")',
        "public static ParityWorkflowRegistry CreateRegistry()",
        "Binding(ShellStartId, ParityWorkflowKind.Infrastructure,",
        "ParityWorkflowSurface.Ui | ParityWorkflowSurface.Launcher",
        "Binding(ShellWorkspaceId, ParityWorkflowKind.ReadOnly,",
        "Binding(ShellRibbonId, ParityWorkflowKind.ReadOnly,",
        "Binding(ProjectSetupId, ParityWorkflowKind.Infrastructure,",
        "Binding(ProjectZoneId, ParityWorkflowKind.SemanticMutation,",
        "Binding(ProjectFloorId, ParityWorkflowKind.SemanticMutation,",
        "Binding(ProjectFamilyId, ParityWorkflowKind.SemanticMutation,",
        "ParityWorkflowRequirement.ActiveDocument",
        "ParityWorkflowRequirement.Project",
        "ParityWorkflowRequirement.AtomicMutation",
        "ParityWorkflowRequirement.Audit",
    ):
        require(catalog, token, "P2 workflow catalog")

    for forbidden in ("Bricscad.", "Teigha.", "System.Windows", "ParityWorkflowSurface.Mcp"):
        if forbidden in catalog:
            fail(f"P2 host-neutral catalog contains unsupported host/surface token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P2ShellProjectEvidenceRules();", "P2 manifest smoke")
    for feature_id in REQUIRED_IDS:
        require(manifest_smoke, f'"{feature_id}"', "P2 manifest smoke")

    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P2ShellProjectBindings();", "P2 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 7", "P2 workflow smoke")
    require(workflow_smoke, "ParityWorkflowKind.SemanticMutation", "P2 workflow smoke")
    require(workflow_smoke, "ParityWorkflowRequirement.AtomicMutation", "P2 workflow smoke")
    require(workflow_smoke, "ParityWorkflowRequirement.Audit", "P2 workflow smoke")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p2-shell-project-navigation.md")
    require(runbook, "`CommandWired`", "P2 runbook")
    require(runbook, "`# catalog-complete=false`", "P2 runbook")
    for guard in BASELINE_GUARDS:
        require(runbook, f"`{guard}`", "P2 runbook baseline evidence")

    print("PASS: P2 shell/project navigation parity evidence is fail-closed, host-neutral, command-wired, and anchored to existing qualified guards.")
    return 0


if __name__ == "__main__":
    sys.exit(main())