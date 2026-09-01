#!/usr/bin/env python3
"""Focused source guard for bounded MCP overload admission responses."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
MAX_REJECT_WRITE_MS = 1000


def main() -> int:
    errors: list[str] = []
    if not SRC.is_file():
        print(f"ERROR: missing {SRC.relative_to(ROOT)}")
        return 1

    source = SRC.read_text(encoding="utf-8")
    admission = re.search(
        r"if\s*\(!ClientSlots\.Wait\(0\)\)\s*\{(?P<body>.*?)\n\s*\}",
        source,
        re.DOTALL,
    )
    if not admission:
        errors.append("missing saturated ClientSlots admission branch")
    else:
        body = admission.group("body")
        if "TryWriteOverloadResponse(client);" not in body:
            errors.append("saturated admission must write a bounded overload response before close")
        if "ThreadPool.QueueUserWorkItem" in body:
            errors.append("rejected overload client must not allocate a worker/client slot")

    required = (
        f"private const int AdmissionRejectWriteTimeoutMilliseconds = {MAX_REJECT_WRITE_MS};",
        "private static void TryWriteOverloadResponse(TcpClient client)",
        "stream.WriteTimeout = AdmissionRejectWriteTimeoutMilliseconds;",
        'TryWriteResponse(stream, 503, "Service Unavailable"',
    )
    for token in required:
        if token not in source:
            errors.append(f"missing overload-response contract: {token}")

    if errors:
        print("FAIL: MCP admission overload response guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP admission overload response guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
