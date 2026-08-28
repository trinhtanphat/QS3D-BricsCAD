#!/usr/bin/env python3
"""Fail closed when the canonical #4352 MCP cross-agent handoff drifts or disappears."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HANDOFF = ROOT / "docs" / "agent-work-claims" / "issue-4352-chatgpt-mcp-session-handoff.md"
RUNBOOK = ROOT / "docs" / "MCP-FULL-CAD-AGENT.md"


def main() -> int:
    errors: list[str] = []
    if not HANDOFF.is_file():
        print(f"ERROR: missing canonical MCP handoff: {HANDOFF.relative_to(ROOT)}")
        return 1
    if not RUNBOOK.is_file():
        print(f"ERROR: missing MCP runbook: {RUNBOOK.relative_to(ROOT)}")
        return 1

    handoff = HANDOFF.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    required_handoff = {
        "canonical issue": "#4352",
        "lane key": "issue-4352",
        "canonical branch": "agent/interactive-20260828-mcpui/issue-4352-gui-cloudflare-onboarding",
        "single-repository runtime": "only `QS3D-BricsCAD`",
        "second repo reference-only boundary": "`QS3D-CAD-MCP`",
        "click-first UI": "click-first",
        "no terminal UX": "PowerShell, CMD",
        "provider-owned password boundary": "QS3D must never ask for, inspect, log, or store the Cloudflare password",
        "loopback endpoint": "127.0.0.1:8765/mcp",
        "API-first policy": "direct CAD API tool first",
        "bounded command surface": "cad_command_sequence",
        "mouse fallback": "cad_ui_click",
        "keyboard fallback": "cad_ui_type",
        "emergency stop": "cad_agent_stop",
        "mutation confirmation": "confirmMutation=true",
        "local-only truth": "PENDING_LOCAL",
        "V25/V26 runtime": "licensed V25/V26",
        "future-agent procedure": "Future-agent operating procedure",
        "definition of done": "Definition of done",
        "session consolidation": "#4314",
    }
    for label, token in required_handoff.items():
        if token not in handoff:
            errors.append(f"handoff missing {label}: {token}")

    required_runbook = (
        "QS3DMCPAGENTCENTER",
        "McpCloudflaredBootstrapper",
        "McpPublicEndpointResolver",
        "cad_entity_inspect",
        "cad_agent_stop",
        "PENDING_LOCAL",
    )
    for token in required_runbook:
        if token not in runbook:
            errors.append(f"runbook missing current MCP contract marker: {token}")

    forbidden_handoff_claims = (
        "Runtime qualification: LOCAL_PASS",
        "Status: 100% / LOCAL_PASS",
        "second MCP repository is required",
    )
    for token in forbidden_handoff_claims:
        if token in handoff:
            errors.append(f"handoff contains forbidden/stale claim: {token}")

    if errors:
        print("Canonical MCP session-handoff preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: #4352 has one canonical cross-agent handoff covering the single-repository "
        "embedded MCP architecture, click-first Cloudflare onboarding, full CAD/UI agent "
        "surface, security boundaries, future-agent procedure and exact-SHA LOCAL_ONLY tail."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
