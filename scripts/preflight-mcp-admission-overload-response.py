#!/usr/bin/env python3
"""Focused source guard for MCP loopback overload admission responses."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
DOC = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-admission-overload-response.md"


def main() -> int:
    errors: list[str] = []
    if not SRC.is_file():
        print(f"ERROR: missing {SRC.relative_to(ROOT)}")
        return 1
    source = SRC.read_text(encoding="utf-8")

    required = (
        "BusyResponseWriteTimeoutMilliseconds",
        "RejectBusyClient",
        "503 Service Unavailable",
        "Connection: close",
        "Retry-After: 1",
        "ClientSlots.Wait(0)",
    )
    for token in required:
        if token not in source:
            errors.append(f"missing overload-response contract token: {token}")

    slot = source.find("if (!ClientSlots.Wait(0))")
    queued = source.find("ThreadPool.QueueUserWorkItem(HandleClient, client)", slot)
    if slot < 0:
        errors.append("missing bounded client-slot admission branch")
    else:
        branch = source[slot:queued] if queued > slot else source[slot:slot + 1200]
        if "RejectBusyClient(client);" not in branch:
            errors.append("saturated admission must return explicit overload response")
        if "client.Close()" in branch and "RejectBusyClient(client);" not in branch:
            errors.append("saturated admission must not silently close/reset the socket")

    helper = source[source.find("private static void RejectBusyClient"):]
    if "WriteTimeout = BusyResponseWriteTimeoutMilliseconds" not in helper:
        errors.append("busy response must use bounded write timeout")
    if "ClientSlots.Release" in helper or "Sessions" in helper:
        errors.append("rejected overload clients must not allocate/release MCP client slots or sessions")

    if not DOC.is_file():
        errors.append(f"missing runbook: {DOC.relative_to(ROOT)}")
    else:
        doc = DOC.read_text(encoding="utf-8")
        for token in (
            "Lane-Key: `issue-5166`",
            "raw 502",
            "503 Service Unavailable",
            "connector_info",
            "MaxConcurrentClients",
            "Connection: close",
            "LOCAL_ONLY",
        ):
            if token not in doc:
                errors.append(f"runbook missing contract token: {token}")

    if errors:
        print("FAIL: MCP admission overload response guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP admission overload response guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
