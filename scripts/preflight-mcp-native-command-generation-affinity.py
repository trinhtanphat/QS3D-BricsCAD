#!/usr/bin/env python3
"""Guard queued MCP native commands against managed-wrapper database replacement."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL: " + message, file=sys.stderr)
    raise SystemExit(1)


for token in (
    "NativeDatabaseIdentity",
    "UnmanagedObject",
    "RequireActiveNativeDatabaseGeneration",
):
    if token not in text:
        fail("native-command generation affinity contract missing: " + token)

arm_start = text.find("private static NativeCommandReservation ArmNativeCommandInCadContext(")
arm_end = text.find("private static T InvokeInCadContext<T>", arm_start)
if arm_start < 0 or arm_end < 0:
    fail("could not isolate native-command arm boundary")
arm = text[arm_start:arm_end]
capture = arm.find("var nativeDatabaseIdentity = RequireActiveNativeDatabaseGeneration(document, IntPtr.Zero, \"reservation\");")
pending_ctor = arm.find("new PendingNativeCommand(document, nativeDatabaseIdentity,")
if capture < 0 or pending_ctor < capture:
    fail("reservation must capture non-zero native database generation before publishing pending ownership")

queue_start = text.find("internal static void QueueNativeCommand(")
queue_end = text.find("internal static bool HasPendingNativeCommand", queue_start)
if queue_start < 0 or queue_end < 0:
    fail("could not isolate QueueNativeCommand")
queue = text[queue_start:queue_end]
for token in (
    "reservation.RequireMatches(document, command);",
    "RequireActiveNativeDatabaseGeneration(document, reservation.NativeDatabaseIdentity, \"dispatch\");",
    "reservation.BeginDispatch();",
    "enqueue();",
):
    if token not in queue:
        fail("queue/dispatch generation fence missing: " + token)
if queue.find("reservation.RequireMatches(document, command);") > queue.find("reservation.BeginDispatch();"):
    fail("prepared reservation must be revalidated before native dispatch begins")
if queue.find("RequireActiveNativeDatabaseGeneration(document, reservation.NativeDatabaseIdentity, \"dispatch\");") > queue.find("enqueue();"):
    fail("active document generation must be revalidated before enqueue")

pending_start = text.find("internal sealed class PendingNativeCommand")
pending_end = text.find("internal sealed class NativeCommandReservation", pending_start)
pending = text[pending_start:pending_end]
if "public IntPtr NativeDatabaseIdentity" not in pending:
    fail("pending native command does not retain exact native database generation")
match_start = text.find("private static bool PendingMatchesLocked(")
match_end = text.find("private static void CleanupExpiredStateLocked", match_start)
if match_start < 0 or match_end < 0:
    fail("could not isolate lifecycle callback match boundary")
match = text[match_start:match_end]
if "HasNativeDatabaseGeneration(pending.Document, pending.NativeDatabaseIdentity)" not in match:
    fail("native lifecycle callbacks can accept a stale managed wrapper after database replacement")

reservation_start = text.find("internal sealed class NativeCommandReservation")
reservation_end = text.find("private sealed class InteractiveModalScope", reservation_start)
reservation = text[reservation_start:reservation_end]
for token in (
    "internal IntPtr NativeDatabaseIdentity",
    "internal void RequireMatches(Document document, string command)",
    "HasNativeDatabaseGeneration(document, _pending.NativeDatabaseIdentity)",
):
    if token not in reservation:
        fail("prepared native-command reservation generation contract missing: " + token)

if "Thread.Sleep" in queue or "Task.Delay" in queue:
    fail("native database generation drift must fail closed without retry/polling")

print("PASS: queued MCP native commands are fenced to the exact active native database generation")
