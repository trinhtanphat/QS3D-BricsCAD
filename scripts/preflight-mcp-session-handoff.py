#!/usr/bin/env python3
"""Fail closed when the canonical #4352 MCP cross-agent handoff drifts or disappears."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HANDOFF = ROOT / "docs" / "agent-work-claims" / "issue-4352-chatgpt-mcp-session-handoff.md"
RUNBOOK = ROOT / "docs" / "MCP-FULL-CAD-AGENT.md"
LOCAL_INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"


def main() -> int:
    errors: list[str] = []
    if not HANDOFF.is_file():
        print(f"ERROR: missing canonical MCP handoff: {HANDOFF.relative_to(ROOT)}")
        return 1
    if not RUNBOOK.is_file():
        print(f"ERROR: missing MCP runbook: {RUNBOOK.relative_to(ROOT)}")
        return 1
    if not LOCAL_INBOX.is_file():
        print(f"ERROR: missing LOCAL_ONLY queue: {LOCAL_INBOX.relative_to(ROOT)}")
        return 1

    handoff = HANDOFF.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")
    local_inbox = LOCAL_INBOX.read_text(encoding="utf-8")

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
        "active compiled transport": "`src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs` — active loopback HTTP transport",
        "legacy transport exclusion": "`src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs` — legacy historical monolith only",
        "exact JSON admission": "exact `application/json` media-type admission",
        "JSON lookalike rejection": "JSON media-type lookalikes",
        "API-first policy": "direct CAD API tool first",
        "bounded command surface": "cad_command_sequence",
        "mouse fallback": "cad_ui_click",
        "keyboard fallback": "cad_ui_type",
        "emergency stop": "cad_agent_stop",
        "top-level mutation confirmation": "top-level `confirmMutation=true`",
        "timeout source audit complete": "atomic `Queued → Running` / `Queued → CancelledBeforeStart` transitions",
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
        "top-level `confirmMutation=true`",
        "`application/json`",
    )
    for token in required_runbook:
        if token not in runbook:
            errors.append(f"runbook missing current MCP contract marker: {token}")

    required_local_inbox = {
        "single MCP local queue item": "## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification",
        "MCP local priority": "- Priority: P0",
        "MCP local open status": "- Status: OPEN",
        "MCP local issue": "issue #4352",
        "MCP local remote disposition": "PENDING_LOCAL / DO_NOT_RETRY_REMOTE",
        "MCP exact-SHA boundary": "exact candidate SHA",
        "MCP V25/V26 host boundary": "licensed BricsCAD V25/V26",
        "MCP Cloudflare boundary": "Cloudflare",
        "MCP ChatGPT boundary": "ChatGPT",
        "MCP handoff link": "docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md",
        "MCP runbook link": "docs/MCP-FULL-CAD-AGENT.md",
        "MCP pending evidence": "- Evidence: `PENDING_LOCAL`",
    }
    for label, token in required_local_inbox.items():
        if token not in local_inbox:
            errors.append(f"LOCAL_ONLY inbox missing {label}: {token}")
    if local_inbox.count("## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification") != 1:
        errors.append("LOCAL_ONLY inbox must contain exactly one LOCAL-024/#4352 MCP qualification item")

    forbidden_handoff_claims = (
        "Runtime qualification: LOCAL_PASS",
        "Status: 100% / LOCAL_PASS",
        "second MCP repository is required",
    )
    for token in forbidden_handoff_claims:
        if token in handoff:
            errors.append(f"handoff contains forbidden/stale claim: {token}")

    mcp_local_section = ""
    marker = "## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification"
    if marker in local_inbox:
        mcp_local_section = local_inbox.split(marker, 1)[1]
        next_header = mcp_local_section.find("\n## ")
        if next_header >= 0:
            mcp_local_section = mcp_local_section[:next_header]
    for token in ("LOCAL_PASS", "Status: PASS", "COMPLETE / 100%"):
        if token in mcp_local_section:
            errors.append(f"#4352 LOCAL_ONLY inbox item contains premature runtime claim: {token}")

    if errors:
        print("Canonical MCP session-handoff preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: #4352 has one canonical cross-agent handoff and one matching P0 LOCAL_ONLY "
        "queue item covering the single-repository modular MCP architecture, click-first "
        "Cloudflare onboarding, exact JSON/top-level mutation boundaries, full CAD/UI agent "
        "surface, future-agent procedure and exact-SHA licensed V25/V26 + Cloudflare + "
        "ChatGPT qualification tail."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
