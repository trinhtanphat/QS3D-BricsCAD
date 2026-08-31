#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Agent" / "McpToolCapabilityContract.cs"
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
DIRECT = V25 / "McpCadDirectModelRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"


def require(errors, condition, message):
    if not condition:
        errors.append(message)


def between(text, start, end):
    first = text.find(start)
    if first < 0:
        return ""
    last = text.find(end, first + len(start))
    return text[first:] if last < 0 else text[first:last]


def main():
    paths = (CORE, SERVER, AGENT, DIRECT, DOMAIN)
    missing = [path for path in paths if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    core = CORE.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    agent = AGENT.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
    domain = DOMAIN.read_text(encoding="utf-8")
    errors = []

    for token in (
        "enum McpExecutionMode", "Auto", "CadDirect", "Qs3dDomain",
        "enum McpToolLane", "BricsCadHost", "DesktopAutomation", "Qs3dDomain",
        "ResolveExecutionMode", "EnsureAllowed", "ClassifyFailure",
        'ExecutionModeViolationCode = "EXECUTION_MODE_VIOLATION"',
        'CadHostUnavailableCode = "CAD_HOST_UNAVAILABLE"',
        'CadCommandFailedCode = "CAD_COMMAND_FAILED"',
        'DesktopConsentRequiredCode = "DESKTOP_CONSENT_REQUIRED"',
        'Qs3dDomainUnavailableCode = "QS3D_DOMAIN_UNAVAILABLE"',
        'Qs3dContextRequiredCode = "QS3D_CONTEXT_REQUIRED"',
        'Qs3dSourceBugCode = "QS3D_SOURCE_BUG"',
    ):
        require(errors, token in core, "Core capability contract lost token: " + token)

    for tool in ("mcp_status", "bricscad_status", "qs3d_status", "qs3d_domain_status", "qs3d_place_single_footing"):
        require(errors, f'Tool("{tool}"' in server, "MCP transport does not publish tool: " + tool)
    for token in (
        '\\"executionMode\\"', '\\"execution_mode\\"', '\\"AUTO\\"', '\\"CAD_DIRECT\\"', '\\"QS3D_DOMAIN\\"',
        '\\"structuredContent\\":{\\"error\\"', '\\"code\\"', '\\"lane\\"', '\\"message\\"',
        "McpToolCapabilityContract.ClassifyFailure",
    ):
        require(errors, token in server, "MCP transport lost mode/error contract token: " + token)

    for token in (
        "McpQs3dDomainRuntime.ResetForServerStart()",
        "McpToolCapabilityContract.ResolveExecutionMode",
        "McpToolCapabilityContract.EnsureAllowed",
        'case "mcp_status"', 'case "bricscad_status"', 'case "qs3d_domain_status"',
        'case "qs3d_status"', 'case "qs3d_run_command"', 'case "qs3d_place_single_footing"',
        "BuildBricscadStatusJson", "BuildMcpStatusJson",
    ):
        require(errors, token in agent, "canonical MCP agent lost lane-routing token: " + token)

    require(errors, '"product":\\"QS3D-BricsCAD' not in agent,
            "BricsCAD status must not retain the old mixed QS3D-BricsCAD status payload")

    direct_registry = between(direct, "private static readonly HashSet<string> Tools", "private static readonly HashSet<string> KnownCommandTokens")
    require(errors, '"qs3d_place_single_footing"' not in direct_registry,
            "qs3d_place_single_footing is still registered as a direct-CAD tool")
    require(errors, 'case "qs3d_place_single_footing"' not in direct,
            "qs3d_place_single_footing is still dispatched by the direct-CAD runtime")
    require(errors, "private static string PlaceSingleFooting" not in direct,
            "QS3D single-footing business authoring still lives in the direct-CAD runtime")

    for token in (
        '"qs3d_status"', '"qs3d_domain_status"', '"qs3d_run_command"', '"qs3d_place_single_footing"',
        "ProjectContextCoordinator.TryGetCached", "SingleFootingCommands.PlaceActiveSingleFootingAt",
        "McpCadAgentRuntime.Qs3dCommandPattern", "RecordFailure", "RecordSuccess",
    ):
        require(errors, token in domain, "QS3D domain runtime lost token: " + token)
    for forbidden in ("ProjectContextCoordinator.GetOrCreate", "SafeDocumentName", "currentLayer", "activeDocument"):
        require(errors, forbidden not in domain,
                "QS3D domain status must not bind/create or leak CAD host status field: " + forbidden)

    cad_dispatch = between(agent, "if (McpCadDirectModelRuntime.IsTool(tool))", "if (McpDesktopAutomationRuntime.IsTool(tool))")
    require(errors, bool(cad_dispatch), "cannot inspect direct-CAD dispatch branch")
    if cad_dispatch:
        require(errors, "McpQs3dDomainRuntime" not in cad_dispatch,
                "CAD-direct dispatch depends on QS3D domain runtime")
        require(errors, "ProjectContextCoordinator" not in cad_dispatch,
                "CAD-direct dispatch depends on QS3D project context")

    if errors:
        print("ERROR: MCP capability-lane preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP function calling separates MCP/BricsCAD/CAD/desktop/QS3D lanes, mode gates and error contracts without coupling CAD-direct to QS3D health.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
