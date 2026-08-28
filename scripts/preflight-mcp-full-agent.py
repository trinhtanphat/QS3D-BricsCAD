#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"


def main() -> int:
    if not SERVER.is_file():
        print("ERROR: missing", SERVER.relative_to(ROOT))
        return 1

    text = SERVER.read_text(encoding="utf-8")
    errors: list[str] = []

    required = {
        "direct line creation": '"cad_create_line"',
        "direct circle creation": '"cad_create_circle"',
        "direct polyline creation": '"cad_create_polyline"',
        "direct text creation": '"cad_create_text"',
        "entity transform": '"cad_entity_transform"',
        "entity delete": '"cad_entity_delete"',
        "layer management": '"cad_layer"',
        "command catalog": '"cad_command_catalog"',
        "bounded command sequencing": '"cad_command_sequence"',
        "view state": '"cad_view_state"',
        "idle wait": '"cad_wait_idle"',
        "BricsCAD-only mouse": '"cad_ui_click"',
        "BricsCAD-only typing": '"cad_ui_type"',
        "BricsCAD-only named keys": '"cad_ui_key"',
        "emergency stop": '"cad_agent_stop"',
        "explicit resume": '"cad_agent_resume"',
        "audit tail": '"cad_audit_tail"',
        "mutation confirmation": "confirmMutation=true",
        "window-relative click guard": "GetClientRect(hwnd, out rect)",
        "client-to-screen mapping": "ClientToScreen(hwnd, ref point)",
        "foreground process verification": "GetWindowThreadProcessId",
        "foreground window verification": "RequireSameForegroundCadWindow",
        "foreground acquisition": "RequireForegroundCadWindow",
        "Unicode SendInput": "SendUnicodeText(hwnd, text)",
        "mouse SendInput": "SendMouse(down)",
        "Win32 input API": 'DllImport("user32.dll"',
        "CAD application context": "ExecuteInApplicationContext",
        "bounded CAD wait": "ManualResetEventSlim",
        "transactional native entities": "transaction.Commit()",
        "mutation audit": "mcp-agent-audit.jsonl",
        "bounded audit rotation": "MaxAuditBytes",
        "allowlisted CAD commands": "AllowedCadCommands",
        "QS3D command allowlist": '"^QS3D[A-Za-z0-9_]*$"',
        "script control-char rejection": "forbidden control characters",
        "script blank-terminator anti-chain guard": "inputs may not continue after a blank command terminator",
        "script known-command anti-injection guard": "inputs may not inject another CAD/QS3D command",
        "tool arguments object scoping": "TryExtractToolCall",
        "tool arguments object parser": "TryExtractObjectProperty",
        "HTTP transfer-encoding rejection": "Transfer-Encoding is not supported; use Content-Length",
        "duplicate critical-header rejection": "Duplicate security-sensitive HTTP header",
        "MCP session termination": 'request.Method == "DELETE"',
        "MCP protocol/session binding": "MCP-Protocol-Version does not match the initialized session",
        "bounded clients": "MaxConcurrentClients",
    }
    for label, token in required.items():
        if token not in text:
            errors.append(f"missing {label}: {token}")

    for command in (
        '"HATCH"', '"DIMLINEAR"', '"BLOCK"', '"XREF"', '"LAYOUT"',
        '"MVIEW"', '"PLOT"', '"SAVEAS"', '"OPEN"', '"UNDO"',
    ):
        if command not in text:
            errors.append(f"missing full-drawing command capability: {command}")

    # Ordinary mutation surfaces must all pass through RequireMutation. Emergency stop/cancel
    # are intentionally confirmation-free so a remote operator can always stop an active action.
    mutation_routes = (
        'case "cad_create_line": return RequireMutation',
        'case "cad_create_circle": return RequireMutation',
        'case "cad_create_polyline": return RequireMutation',
        'case "cad_create_text": return RequireMutation',
        'case "cad_entity_transform": return RequireMutation',
        'case "cad_entity_delete": return RequireMutation',
        'case "cad_layer": return RequireMutation',
        'case "cad_command_sequence": return RequireMutation',
        'case "qs3d_run_command": return RequireMutation',
        'case "cad_ui_click": return RequireMutation',
        'case "cad_ui_type": return RequireMutation',
        'case "cad_ui_key": return RequireMutation',
    )
    for route in mutation_routes:
        if route not in text:
            errors.append(f"mutation bypasses confirmation/stop gate: {route}")

    # Remote MCP must not expose general OS shell/process execution. Browser/cloudflared process
    # launch belongs to separate, local owner-facing onboarding classes, not the network server.
    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start(", "mouse_event("):
        if forbidden in text:
            errors.append(f"forbidden remote execution/legacy input surface in MCP server: {forbidden}")

    if errors:
        print("Full MCP CAD agent preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: embedded MCP exposes direct transactional CAD creation/editing, bounded "
        "allowlisted advanced command workflows, foreground-process-confined SendInput, "
        "emergency stop/resume, idle/status observation, rotating local audit evidence, "
        "bounded HTTP/session handling and mutation confirmation without arbitrary shell execution."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
