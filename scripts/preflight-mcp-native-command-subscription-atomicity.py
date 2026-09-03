#!/usr/bin/env python3
"""Fail closed if MCP native-command event subscription can publish partial handlers."""

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

subscriptions = [
    "document.CommandWillStart += pending.WillStartHandler;",
    "document.CommandEnded += pending.EndedHandler;",
    "document.CommandCancelled += pending.CancelledHandler;",
    "document.CommandFailed += pending.FailedHandler;",
]
for token in subscriptions:
    if method.count(token) != 1:
        raise SystemExit(f"FAIL: expected exactly one native event subscription: {token}")

publish = "_pending = pending;"
if method.count(publish) != 2:
    raise SystemExit("FAIL: expected success publication plus rollback-failure quarantine publication")
success_publish_at = method.rindex(publish)
if any(method.index(token) > success_publish_at for token in subscriptions):
    raise SystemExit("FAIL: success _pending publication must follow all event subscriptions")

# Attachment must be transactional. If detach itself cannot be proven, the candidate must be
# published as quarantine state before rethrow so outer gate release cannot admit a second writer.
# Inspect the conditional body rather than requiring it to contain only the assignment: production
# may emit redacted audit metadata while quarantining, and that must not turn this guard into a
# false negative.
rollback = re.search(
    r"catch\s*\{\s*if\s*\(!TryDetachPendingLocked\(pending\)\)\s*\{(?P<body>.*?)\}\s*throw;\s*\}",
    method,
    flags=re.DOTALL,
)
try_at = method.find("try", method.index(subscriptions[0]) - 200)
if try_at < 0 or rollback is None:
    raise SystemExit("FAIL: native event attachment lacks rollback quarantine/rethrow")
rollback_body = rollback.group("body")
if rollback_body.count(publish) != 1:
    raise SystemExit("FAIL: rollback failure must publish exactly one quarantine candidate")
if "return" in rollback_body:
    raise SystemExit("FAIL: rollback quarantine must not return instead of rethrowing the host failure")
if not (try_at < method.index(subscriptions[0]) < method.index(subscriptions[-1]) < rollback.start() < success_publish_at):
    raise SystemExit("FAIL: rollback quarantine must cover every event add before success publication")

helper_start = text.find("private static bool TryDetachPendingLocked(")
helper_end = text.find("\n        private static string NormalizeRequiredToken", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("FAIL: rollback helper must report whether every unsubscribe succeeded")
helper = text[helper_start:helper_end]
for event_name in ("CommandWillStart", "CommandEnded", "CommandCancelled", "CommandFailed"):
    if f"pending.Document.{event_name} -= pending." not in helper:
        raise SystemExit(f"FAIL: rollback helper does not detach {event_name}")
if "return detached;" not in helper or "detached = false;" not in helper:
    raise SystemExit("FAIL: rollback helper must report unsubscribe failure instead of swallowing it")

# All cleanup paths that can reopen writer admission must preserve quarantine on detach failure.
reset_start = text.find("internal static void Reset()")
reset_end = text.find("\n        private static NativeCommandReservation ArmNativeCommandInCadContext", reset_start)
reset = text[reset_start:reset_end]
if "if (TryDetachPendingLocked(_pending))" not in reset or "_pending = null;" not in reset:
    raise SystemExit("FAIL: Reset must not clear pending state unless unsubscribe cleanup succeeds")

dispose_start = text.find("public void Dispose()", text.find("internal sealed class NativeCommandReservation"))
dispose_end = text.find("\n        private sealed class InteractiveModalScope", dispose_start)
dispose = text[dispose_start:dispose_end]
if "if (TryDetachPendingLocked(_pending))" not in dispose or "McpCadMutationCoordinator._pending = null;" not in dispose:
    raise SystemExit("FAIL: reservation Dispose must preserve quarantine when unsubscribe cleanup fails")

# Never repair this native boundary by retrying event registration.
if re.search(r"(?:while|for)\s*\([^)]*\)[\s\S]{0,500}Command(?:WillStart|Ended|Cancelled|Failed)\s*\+=", method):
    raise SystemExit("FAIL: native event subscription must not be retried")

print("PASS: MCP native-command event attachment is atomic and cleanup failure remains quarantined")
