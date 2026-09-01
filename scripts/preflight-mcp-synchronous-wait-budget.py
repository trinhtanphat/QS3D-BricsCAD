#!/usr/bin/env python3
"""Focused source guard for synchronous MCP wait response budgets."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
DESKTOP = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDesktopAutomationRuntime.cs"
DIAGNOSTICS = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"
DOC = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-synchronous-wait-budget.md"
MAX_SYNC_WAIT_MS = 7000


def const_value(text: str, name: str):
    match = re.search(rf"private const int {re.escape(name)}\s*=\s*(\d+)\s*;", text)
    return None if not match else int(match.group(1))


def main() -> int:
    errors: list[str] = []
    for path in (DESKTOP, DIAGNOSTICS):
        if not path.is_file():
            errors.append(f"missing {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    desktop = DESKTOP.read_text(encoding="utf-8")
    diagnostics = DIAGNOSTICS.read_text(encoding="utf-8")

    if const_value(desktop, "MaxWaitMilliseconds") != MAX_SYNC_WAIT_MS:
        errors.append("desktop MaxWaitMilliseconds must be 7000")
    if const_value(desktop, "MaxSequenceMilliseconds") != MAX_SYNC_WAIT_MS:
        errors.append("desktop MaxSequenceMilliseconds must be 7000")
    if const_value(diagnostics, "MaxWaitMilliseconds") != MAX_SYNC_WAIT_MS:
        errors.append("diagnostics MaxWaitMilliseconds must be 7000")

    for forbidden in (
        '"maximum\\\":15000',
        '"maximum\\\":30000',
        "Wait up to 15 seconds",
        "at most 30 seconds",
    ):
        if forbidden in desktop:
            errors.append(f"desktop descriptor still advertises an over-budget wait: {forbidden}")
    if '"maximum\\\":15000' in diagnostics:
        errors.append("diagnostics_wait schema still advertises 15000 ms")

    for token in (
        'var timeout = Integer(body, "timeoutMs", 5000, 0, MaxWaitMilliseconds);',
        'var maxDuration = StrictOptionalInteger(body, "maxDurationMs", 5000, 1000, MaxSequenceMilliseconds);',
        "EnsureSequenceRunning",
        "Sequence execution is fail-fast",
    ):
        if token not in desktop:
            errors.append(f"desktop bounded-wait contract missing: {token}")

    if 'var timeout = Integer(body, "timeoutMs", 5000, 0, MaxWaitMilliseconds);' not in diagnostics:
        errors.append("diagnostics_wait must retain 5000 ms default under the bounded maximum")

    if not DOC.is_file():
        errors.append(f"missing runbook: {DOC.relative_to(ROOT)}")
    else:
        doc = DOC.read_text(encoding="utf-8")
        for token in (
            "Lane-Key: `issue-5168`",
            "15 seconds",
            "30 seconds",
            "7000 ms",
            "diagnostics_wait",
            "desktop_wait_for_window",
            "desktop_sequence",
            "raw 502",
            "chunked",
            "LOCAL_ONLY",
        ):
            if token not in doc:
                errors.append(f"runbook missing contract token: {token}")

    if errors:
        print("FAIL: MCP synchronous wait response-budget guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP synchronous wait response-budget guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
