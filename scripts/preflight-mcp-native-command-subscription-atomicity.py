#!/usr/bin/env python3
"""Fail closed if MCP native-command event subscription can lose partial-handler ownership."""

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

start = text.find("private static NativeCommandReservation ArmNativeCommandInCadContext(")
end = text.find("\n        private static T InvokeInCadContext<T>", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL: could not isolate ArmNativeCommandInCadContext")
method = text[start:end]

publish = "_pending = pending;"
if method.count(publish) != 1:
    raise SystemExit("FAIL: authoritative writer quarantine must publish exactly one pending candidate")

handlers = [
    ("WillStartMayBeSubscribed", "document.CommandWillStart += pending.WillStartHandler;"),
    ("EndedMayBeSubscribed", "document.CommandEnded += pending.EndedHandler;"),
    ("CancelledMayBeSubscribed", "document.CommandCancelled += pending.CancelledHandler;"),
    ("FailedMayBeSubscribed", "document.CommandFailed += pending.FailedHandler;"),
]
first_add = min(method.find(token) for _, token in handlers)
if first_add < 0 or method.find(publish) < 0 or method.find(publish) > first_add:
    raise SystemExit("FAIL: authoritative writer quarantine must be published before first fallible native +=")

last_add = -1
for flag, token in handlers:
    if method.count(token) != 1:
        raise SystemExit(f"FAIL: expected exactly one native event subscription: {token}")
    flag_set = method.find(f"pending.{flag} = true;")
    add_at = method.find(token)
    if flag_set < 0 or flag_set > add_at:
        raise SystemExit(f"FAIL: {flag} must publish may-be-subscribed ownership before native +=")
    last_add = max(last_add, add_at)

accept_at = method.find("pending.AcceptCallbacks = true;")
if accept_at < last_add:
    raise SystemExit("FAIL: callbacks must remain disabled until every native handler add returns")
rollback_at = method.find("if (TryDetachPendingLocked(pending))")
clear_at = method.find("if (ReferenceEquals(_pending, pending)) _pending = null;", rollback_at)
audit_at = method.find("native command handler rollback failed; writer remains quarantined", rollback_at)
throw_at = method.find("throw;", rollback_at)
if rollback_at < 0 or clear_at < rollback_at or audit_at < clear_at or throw_at < audit_at:
    raise SystemExit("FAIL: partial attach failure must clear only after proven detach, otherwise retain quarantine, then rethrow")

helper_start = text.find("private static bool TryDetachPendingLocked(")
helper_end = text.find("\n        private static string NormalizeRequiredToken", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("FAIL: rollback helper must report whether every unsubscribe succeeded")
helper = text[helper_start:helper_end]
for flag, event_name in (
    ("WillStartMayBeSubscribed", "CommandWillStart"),
    ("EndedMayBeSubscribed", "CommandEnded"),
    ("CancelledMayBeSubscribed", "CommandCancelled"),
    ("FailedMayBeSubscribed", "CommandFailed"),
):
    condition = f"if (pending.{flag})"
    remove = f"pending.Document.{event_name} -= pending."
    clear = f"pending.{flag} = false;"
    if condition not in helper or remove not in helper:
        raise SystemExit(f"FAIL: detach does not retain exact ownership for {event_name}")
    if helper.find(clear) < helper.find(remove):
        raise SystemExit(f"FAIL: {flag} clears before matching native -= succeeds")
if "return !pending.HasSubscribedHandlers;" not in helper:
    raise SystemExit("FAIL: detach result must derive from unresolved per-handler ownership")

# Cleanup paths may reopen writer admission only after every native -= is proven.
reset_start = text.find("internal static void Reset()")
reset_end = text.find("\n        private static NativeCommandReservation ArmNativeCommandInCadContext", reset_start)
reset = text[reset_start:reset_end]
if "if (TryDetachPendingLocked(_pending))" not in reset or "_pending = null;" not in reset:
    raise SystemExit("FAIL: Reset must preserve writer quarantine when native detach is unresolved")

dispose_start = text.find("public void Dispose()", text.find("internal sealed class NativeCommandReservation"))
dispose_end = text.find("\n        private sealed class InteractiveModalScope", dispose_start)
dispose = text[dispose_start:dispose_end]
if "if (TryDetachPendingLocked(_pending))" not in dispose or "McpCadMutationCoordinator._pending = null;" not in dispose:
    raise SystemExit("FAIL: reservation Dispose must clear authoritative pending only after proven full detach")

# Never repair this native boundary by retrying event registration or replaying a CAD command.
if re.search(r"(?:while|for)\s*\([^)]*\)[\s\S]{0,500}Command(?:WillStart|Ended|Cancelled|Failed)\s*\+=", method):
    raise SystemExit("FAIL: native event subscription must not be retried")

print("PASS: MCP native-command subscription ownership is fail-closed across partial attach/detach failure")
