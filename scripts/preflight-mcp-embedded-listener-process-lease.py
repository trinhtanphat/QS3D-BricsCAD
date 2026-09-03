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
        "previous();",
        "stop.Invoke(null, null)",
        "could not stop the previous embedded listener generation",
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

    handoff_start = text.find("private static void StopPreviousProcessLease()")
    handoff_end = text.find("private static void PublishProcessLease()", handoff_start)
    if handoff_start < 0 or handoff_end < 0:
        return fail("could not isolate previous-generation handoff implementation")
    handoff = text[handoff_start:handoff_end]

    swallowed = (
        "try { previous(); } catch { }",
        "catch { }",
    )
    if any(token in handoff for token in swallowed):
        return fail("known QS3D listener handoff must not swallow stop failures and continue to fallback binding")
    if "throw new InvalidOperationException" not in handoff:
        return fail("known QS3D listener handoff failures must propagate fail-closed")

    callback_index = handoff.find("previous();")
    reflection_index = handoff.find("stop.Invoke(null, null)")
    lease_clear_index = handoff.rfind("domain.SetData(EmbeddedListenerProcessLeaseKey, null)")
    if min(callback_index, reflection_index, lease_clear_index) < 0:
        return fail("could not locate fail-closed handoff/lease-clear sequence")
    if not (callback_index < lease_clear_index and reflection_index < lease_clear_index):
        return fail("known-generation process lease must remain published until callback/reflection handoff succeeds")

    stop_index = text.find("public static void Stop()")
    release_index = text.find("ReleaseProcessLease", stop_index)
    if stop_index < 0 or release_index < 0:
        return fail("normal Stop() must release the process-wide listener lease")

    print("MCP embedded listener process-lease preflight passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
