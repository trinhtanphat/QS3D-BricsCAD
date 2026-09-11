#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
BASELINE_GUARDS = (
    "scripts/preflight-rebar-hub.py",
    "scripts/preflight-generated-rebar-atomicity.py",
    "scripts/preflight-generated-rebar-audit.py",
    "scripts/preflight-rebar-selection-project-lifecycle.py",
    "scripts/preflight-rebar-bbs-provenance.py",
    "scripts/preflight-rebar-schedule-export-active-guard.py",
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
    fields = rows.get("rebar")
    if fields is None:
        fail("manifest missing P5 feature: rebar")
    if fields[3] != "rebar" or fields[4] != "Applicable" or fields[5] != "CommandWired":
        fail("P5 rebar must be Applicable/CommandWired with canonical workflow key")

    catalog = read("src/QS3D.Core/Features/ParityRebarCatalog.cs")
    for token in (
        'new FeatureId("rebar")',
        "public static ParityWorkflowRegistry CreateRegistry()",
        "ParityWorkflowKind.SemanticMutation",
        "ParityWorkflowSurface.Ui",
        "ParityWorkflowRequirement.ActiveDocument",
        "ParityWorkflowRequirement.Project",
        "ParityWorkflowRequirement.Selection",
        "ParityWorkflowRequirement.AtomicMutation",
        "ParityWorkflowRequirement.Audit",
    ):
        require(catalog, token, "P5 rebar catalog")
    for forbidden in (
        "Bricscad.", "Teigha.", "System.Windows",
        "ParityWorkflowSurface.Mcp", "ParityWorkflowSurface.Launcher",
    ):
        if forbidden in catalog:
            fail(f"P5 host-neutral catalog contains unsupported host/surface token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P5RebarEvidenceRules();", "P5 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P5RebarBinding();", "P5 workflow smoke")
    require(workflow_smoke, "registry.Bindings.Count != 1", "P5 workflow smoke")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p5-rebar.md")
    require(runbook, "`CommandWired`", "P5 runbook")
    require(runbook, "`# catalog-complete=false`", "P5 runbook")
    require(runbook, "`rebar`", "P5 runbook feature evidence")
    require(runbook, "fabrication-grade", "P5 evidence ceiling")
    require(runbook, "structural-code compliance", "P5 evidence ceiling")
    for guard in BASELINE_GUARDS:
        require(runbook, f"`{guard}`", "P5 runbook baseline evidence")
        read(guard)

    print("PASS: P5 Rebar parity evidence is fail-closed, host-neutral, command-wired, and anchored to existing qualified guards.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
