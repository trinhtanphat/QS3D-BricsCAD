#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "scripts" / "test-mcp-loopback-readonly.py"


def main() -> int:
    if not PROBE.is_file():
        print("ERROR: missing", PROBE.relative_to(ROOT))
        return 1
    text = PROBE.read_text(encoding="utf-8")
    errors: list[str] = []

    required = {
        "loopback-only endpoint": 'DEFAULT_ENDPOINT = "http://127.0.0.1:8765/mcp"',
        "loopback host restriction": 'parsed.hostname not in {"127.0.0.1", "localhost"}',
        "health check": 'base + "/healthz"',
        "bearer rejection": "unauthorized_status != 401",
        "initialize": '"initialize"',
        "initialized notification": '"notifications/initialized"',
        "ping": '"ping"',
        "tools list": '"tools/list"',
        "session id": 'headers.get("mcp-session-id"',
        "session delete": 'request(endpoint, "DELETE"',
        "terminated session retry": "stale_delete_status",
        "terminated session HTTP 404": "stale_delete_status != 404",
        "terminated session result marker": "stale_session_404=PASS",
        "single repository assertion": 'value.get("singleRepository") is not True',
        "full CAD assertion": 'value.get("fullCadAgent") is not True',
        "sanitized secret result": 'secret_output=NONE; mutation_calls=0',
        "read-only status": '("qs3d_status", {})',
        "read-only active document": '("cad_active_document", {})',
        "read-only selection": '("cad_selection", {})',
        "bounded snapshot": '("cad_database_snapshot", {"limit": 5})',
        "view state": '("cad_view_state", {})',
        "idle wait": '("cad_wait_idle", {"timeoutMs": 2000})',
        "audit tail": '("cad_audit_tail", {"limit": 1})',
    }
    for label, token in required.items():
        if token not in text:
            errors.append(f"missing {label}: {token}")

    for forbidden in (
        "subprocess",
        "os.system",
        "powershell",
        "cmd.exe",
        "confirmMutation",
        'call_tool(endpoint, token, session, request_id, "cad_create_',
        'call_tool(endpoint, token, session, request_id, "cad_entity_',
        'call_tool(endpoint, token, session, request_id, "cad_ui_',
        'call_tool(endpoint, token, session, request_id, "cad_command_sequence"',
        'call_tool(endpoint, token, session, request_id, "qs3d_run_command"',
    ):
        if forbidden in text:
            errors.append(f"read-only local probe contains forbidden mutation/shell surface: {forbidden}")

    if 'print(token' in text or 'print("Bearer"' in text or "print('Bearer" in text:
        errors.append("read-only local probe may print bearer material")

    if errors:
        print("MCP loopback read-only probe preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: local MCP loopback probe is loopback-only, exercises auth/session/tool discovery "
        "and terminated-session 404 truth plus bounded read-only CAD observation, performs no mutation, "
        "launches no shell, and does not print bearer material."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
