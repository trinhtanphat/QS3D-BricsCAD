#!/usr/bin/env python3
"""Focused source guard for the cad_wait_idle MCP schema default."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def main() -> int:
    errors: list[str] = []
    for path in (RUNTIME, SERVER):
        if not path.is_file():
            errors.append(f"missing {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")

    expected_dispatch = 'case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 5000, 100, 7000));'
    if expected_dispatch not in runtime:
        errors.append("cad_wait_idle runtime must remain default=5000, minimum=100, maximum=7000")

    expected_schema = '\\"timeoutMs\\":{\\"type\\":\\"integer\\",\\"minimum\\":100,\\"maximum\\":7000,\\"default\\":5000}'
    expected_tool = 'Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "' + expected_schema + '")'
    if expected_tool not in server:
        errors.append("cad_wait_idle schema must publish default 5000 ms")

    legacy_tool = 'Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "\\"timeoutMs\\":{\\"type\\":\\"integer\\",\\"minimum\\":100,\\"maximum\\":7000}")'
    if legacy_tool in server:
        errors.append("cad_wait_idle descriptor still omits the runtime default")

    if errors:
        print("FAIL: MCP cad_wait_idle schema-default guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP cad_wait_idle schema-default guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
