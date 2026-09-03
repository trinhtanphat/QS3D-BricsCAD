#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message):
    print("ERROR: MCP embedded listener process-lease preflight failed: " + message)
    return 1


def main():
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        "EmbeddedListenerProcessLeaseKey",
        "AppDomain.CurrentDomain",
        "StopPreviousProcessLease",
        "PublishProcessLease",
        "ReleaseProcessLease",
        "StopForProcessLease",
    )
    missing = [token for token in required if token not in text]
    if missing:
        return fail("missing process-wide listener lease contract token(s): " + ", ".join(missing))

    start_index = text.find("public static void Start()")
    stop_previous_index = text.find("StopPreviousProcessLease", start_index)
    bind_index = text.find("StartLoopbackListener", start_index)
    publish_index = text.find("PublishProcessLease", start_index)
    if min(start_index, stop_previous_index, bind_index, publish_index) < 0:
        return fail("could not locate listener start/rebind lease sequence")
    if not (stop_previous_index < bind_index < publish_index):
        return fail("previous listener generation must stop before bind and lease publication must follow bind")

    stop_index = text.find("public static void Stop()")
    release_index = text.find("ReleaseProcessLease", stop_index)
    if stop_index < 0 or release_index < 0:
        return fail("normal Stop() must release the process-wide listener lease")

    print("MCP embedded listener process-lease preflight passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
