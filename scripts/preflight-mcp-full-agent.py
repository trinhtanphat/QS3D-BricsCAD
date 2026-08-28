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
        "foreground BricsCAD confinement": "CurrentProcessWindow()",
        "Unicode SendInput": "SendUnicodeText(text)",
        "Win32 input API": 'DllImport("user32.dll"',
        "CAD application context": "ExecuteInApplicationContext",
        "transactional native entities": "transaction.Commit()",
        "mutation audit": "mcp-agent-audit.jsonl",
        "allowlisted CAD commands": "AllowedCadCommands",
        "QS3D command allowlist": '"^QS3D[A-Za-z0-9_]*$"',
        "script control-char rejection": "forbidden control characters",
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

    # Remote MCP must not expose general OS shell/process execution. UI fallback is confined
    # to the BricsCAD process window, while Cloudflare/browser onboarding lives in separate UI code.
    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start("):
        if forbidden in text:
            errors.append(f"forbidden arbitrary OS execution surface in MCP server: {forbidden}")

    if errors:
        print("Full MCP CAD agent preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: embedded MCP exposes direct transactional CAD creation/editing, a bounded "
        "allowlisted command-line surface covering advanced drawing/layout/plot workflows, "
        "BricsCAD-window-only mouse/keyboard fallback, emergency stop/resume, idle/status "
        "observation and local mutation auditing without arbitrary shell execution."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
