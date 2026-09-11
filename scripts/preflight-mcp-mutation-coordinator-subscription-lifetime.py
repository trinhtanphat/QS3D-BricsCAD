from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

def require(condition: bool, message: str) -> None:
    if not condition:
        failures.append(message)

def position(token: str) -> int:
    return text.find(token)

arm_start = position("private static NativeCommandReservation ArmNativeCommandInCadContext")
arm_end = position("private static T InvokeInCadContext")
arm = text[arm_start:arm_end] if arm_start >= 0 and arm_end > arm_start else ""

require(bool(arm), "could not isolate ArmNativeCommandInCadContext")
require(arm.find("_pending = pending;") >= 0, "authoritative pending ownership is not published")
first_add = min([p for p in [arm.find("CommandWillStart +="), arm.find("CommandEnded +="), arm.find("CommandCancelled +="), arm.find("CommandFailed +=")] if p >= 0] or [-1])
require(first_add >= 0, "native command handler attachment block is missing")
require(0 <= arm.find("_pending = pending;") < first_add,
        "authoritative pending ownership must precede the first fallible native handler attach")
handler_specs = [
    ("WillStartMayBeSubscribed", "CommandWillStart +=", "CommandWillStart -="),
    ("EndedMayBeSubscribed", "CommandEnded +=", "CommandEnded -="),
    ("CancelledMayBeSubscribed", "CommandCancelled +=", "CommandCancelled -="),
    ("FailedMayBeSubscribed", "CommandFailed +=", "CommandFailed -="),
]
for flag, add_token, remove_token in handler_specs:
    flag_set = arm.find(f"pending.{flag} = true;")
    add_pos = arm.find(add_token)
    require(flag_set >= 0 and add_pos >= 0 and flag_set < add_pos,
            f"{flag} must publish may-be-subscribed ownership before {add_token}")
    require(flag in text, f"PendingNativeCommand is missing {flag}")

accept_pos = arm.find("pending.AcceptCallbacks = true;")
last_add = max(arm.find(spec[1]) for spec in handler_specs)
require(accept_pos > last_add, "callbacks must not be accepted until all native handlers attach coherently")

match_start = position("private static bool PendingMatchesLocked")
match_end = position("private static void CleanupExpiredStateLocked")
match = text[match_start:match_end] if match_start >= 0 and match_end > match_start else ""
require("ReferenceEquals(_pending, pending)" in text,
        "callbacks/terminal completion must prove exact authoritative pending identity")
require("AcceptCallbacks" in match or "AcceptCallbacks" in text[position("private static void OnCommandWillStart"):match_end],
        "callbacks must be gated by the authoritative pending acceptance state")
detach_start = position("private static bool TryDetachPendingLocked")
detach_end = position("private static string NormalizeRequiredToken")
detach = text[detach_start:detach_end] if detach_start >= 0 and detach_end > detach_start else ""
require(bool(detach), "could not isolate TryDetachPendingLocked")
for flag, _, remove_token in handler_specs:
    require(f"if (pending.{flag})" in detach,
            f"detach must consult {flag} before touching the native accessor")
    remove_pos = detach.find(remove_token)
    clear_pos = detach.find(f"pending.{flag} = false;")
    require(remove_pos >= 0 and clear_pos > remove_pos,
            f"{flag} may clear only after matching native remove succeeds")

require("return !pending.HasSubscribedHandlers;" in detach,
        "detach result must derive from unresolved per-handler ownership")

if failures:
    for failure in failures:
        print("FAIL:", failure)
    sys.exit(1)

print("PASS: mutation coordinator retains exact native handler ownership across partial attach/detach failures")
