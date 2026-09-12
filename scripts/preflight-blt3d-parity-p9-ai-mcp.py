#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEADER = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason"
WIRED = ("ai.luna", "mcp.direct-cad")


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
            fail(f"manifest missing P9 wired feature: {feature_id}")
        if fields[3] != feature_id or fields[4] != "Applicable" or fields[5] != "CommandWired":
            fail(f"P9 wired feature is not canonical Applicable/CommandWired: {feature_id}")

    catalog = read("src/QS3D.Core/Features/ParityAiMcpCatalog.cs")
    require(catalog, "public static class ParityAiMcpCatalog", "P9 catalog")
    require(catalog, "public static ParityWorkflowRegistry CreateRegistry()", "P9 catalog")
    for feature_id in WIRED:
        require(catalog, f'new FeatureId("{feature_id}")', "P9 catalog")
    for token in (
        "ParityWorkflowKind.Infrastructure",
        "ParityWorkflowSurface.Ui",
        "ParityWorkflowSurface.Mcp",
        "ParityWorkflowRequirement.ActiveDocument",
    ):
        require(catalog, token, "P9 catalog")
    for forbidden in (
        "Bricscad.", "Teigha.", "System.Windows",
        "ParityWorkflowSurface.Launcher",
        "ParityWorkflowKind.SemanticMutation",
        "api_key", "access_token", "refresh_token",
    ):
        if forbidden in catalog.lower() if forbidden == forbidden.lower() else forbidden in catalog:
            fail(f"P9 host-neutral catalog contains forbidden token: {forbidden}")

    manifest_smoke = read("tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs")
    require(manifest_smoke, "P9AiMcpEvidenceRules();", "P9 manifest smoke")
    workflow_smoke = read("tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs")
    require(workflow_smoke, "P9AiMcpBindings();", "P9 workflow smoke")
    require(workflow_smoke, "P9 AI/MCP catalog must contain exactly two bindings", "P9 workflow smoke")

    ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/BltToolRibbonAugmenter.cs")
    require(ribbon, '"AI_DASHBOARD"', "P9 AI ribbon evidence")
    override = read("src/QS3D.BricsCAD.V25/Ribbon/McpRibbonCommandOverride.cs")
    require(override, '[Prefix + "AI_DASHBOARD"] = "QS3DMCPAGENTCENTER"', "P9 AI route")
    require(override, '[Prefix + "MCP_SETTINGS"] = "QS3DMCPAGENTCENTER"', "P9 MCP route")
    agent_center = read("src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs")
    require(agent_center, 'CommandMethod("QS3DMCPAGENTCENTER"', "P9 Agent Center command")
    require(agent_center, "chưa phải bằng chứng một tools/call", "P9 runtime evidence ceiling")

    server = read("src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs")
    for token in ('"tools/call"', 'Tool("connector_info"', 'Tool("cad_active_document"', 'Tool("cad_audit_tail"'):
        require(server, token, "P9 embedded MCP evidence")
    runtime = read("src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs")
    require(runtime, 'case "cad_active_document"', "P9 direct CAD runtime")
    require(runtime, 'case "cad_audit_tail"', "P9 audit runtime")

    canonical = read("docs/MCP-CANONICAL-RUNBOOK.md")
    require(canonical, "ChatGPT / OpenAI products", "P9 direct MCP architecture")
    require(canonical, "PENDING_LOCAL", "P9 runtime evidence ceiling")
    require(canonical, "LOCAL_ONLY", "P9 runtime evidence ceiling")

    runbook = read("docs/FEATURE-RUNBOOKS/blt3d-parity-p9-ai-mcp.md")
    require(runbook, "`CommandWired`", "P9 runbook")
    require(runbook, "`# catalog-complete=false`", "P9 runbook")
    require(runbook, "clean-room", "P9 clean-room boundary")
    require(runbook, "LUNA", "P9 reference mapping")
    require(runbook, "LOCAL-024", "P9 runtime ceiling")

    print("PASS: P9 AI/MCP parity evidence is source-anchored, host-neutral and runtime-fail-closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())