#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HUB = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDiagnosticHub.cs"
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def between(text: str, start: str, end: str) -> str:
    a = text.find(start)
    if a < 0:
        raise SystemExit(f"missing start marker: {start}")
    b = text.find(end, a + len(start))
    if b < 0:
        raise SystemExit(f"missing end marker: {end}")
    return text[a:b]


def main() -> int:
    hub = HUB.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    invoke = between(hub, "internal static string InvokeInCadContext", "internal static void Record")
    call_tool = between(server, "private static string CallTool", "private static string ScreenshotToolSuccess")

    errors = []
    required = (
        "private const int CadReadTimeoutMilliseconds = 8000;",
        "item.Done.Wait(CadReadTimeoutMilliseconds)",
        "CadReadCancelledBeforeStart",
        "queued diagnostic read was cancelled before start",
        "completion is uncertain",
    )
    for token in required:
        if token not in hub:
            errors.append(f"missing bounded CAD-context contract: {token}")

    if "CadReadTimeoutMilliseconds = 10000" in hub:
        errors.append("direct CAD-context wait must leave response budget instead of consuming the full 10-second window")

    structured_error_contract = (
        "var failure = McpToolCapabilityContract.ClassifyFailure(tool, ex);",
        "ToolError(failure.Code, McpToolCapabilityContract.LaneName(failure.Lane), failure.Message)",
    )
    if not all(token in call_tool for token in structured_error_contract):
        errors.append("embedded MCP server must keep converting runtime failures into structured MCP tool errors")

    if "Interlocked.CompareExchange(ref item.State, CadReadCancelledBeforeStart, CadReadQueued)" not in invoke:
        errors.append("queued CAD work must remain cancellable before it starts")

    if errors:
        print("FAIL: MCP direct CAD-context response-budget guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: direct CAD-context work reserves response budget, fails closed before start, and preserves completion-uncertain semantics after work begins.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
