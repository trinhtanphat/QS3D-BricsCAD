#!/usr/bin/env python3
"""Focused source guard for cad_wait_idle transport response budget."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
MAX_WAIT_MS = 7000


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

    expected_dispatch = f'case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 5000, 100, {MAX_WAIT_MS}));'
    if expected_dispatch not in runtime:
        errors.append("cad_wait_idle runtime must default to 5000 ms and cap at 7000 ms")

    expected_schema = '\\"timeoutMs\\":{\\"type\\":\\"integer\\",\\"minimum\\":100,\\"maximum\\":7000}'
    if expected_schema not in server:
        errors.append("active MCP V2 cad_wait_idle schema must cap timeoutMs at 7000 ms")

    if 'case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 10000, 100, 30000));' in runtime:
        errors.append("cad_wait_idle still has the legacy 10s default / 30s maximum")
    if '\\"maximum\\":30000' in server and 'Tool("cad_wait_idle"' in server:
        # Narrow exact descriptor verification above is authoritative; this message makes RED diagnosis explicit.
        errors.append("cad_wait_idle active descriptor still advertises an over-budget maximum")

    for token in (
        "while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)",
        'SafeSystemVariable("CMDACTIVE")',
        "Thread.Sleep(100);",
        'return "{\\\"idle\\\":false,\\\"timeoutMs\\\":"',
    ):
        if token not in runtime:
            errors.append(f"cad_wait_idle semantics unexpectedly changed/missing: {token}")

    if errors:
        print("FAIL: MCP cad_wait_idle response-budget guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP cad_wait_idle response-budget guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
