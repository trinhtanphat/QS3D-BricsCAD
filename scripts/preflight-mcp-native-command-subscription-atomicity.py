#!/usr/bin/env python3
"""Fail closed if MCP native-command event subscription can publish partial handlers."""

from pathlib import Path
import re
import sys

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
if method.count(publish) != 1:
    raise SystemExit("FAIL: native pending publication must remain singular")
publish_at = method.index(publish)
if any(method.index(token) > publish_at for token in subscriptions):
    raise SystemExit("FAIL: _pending must not publish before all event subscriptions succeed")

# Attachment must be transactional. A failed host event add may occur after one or more
# earlier adds succeeded; rollback therefore has to encompass the whole subscription block,
# detach the same candidate, and rethrow instead of retrying or swallowing the failure.
try_at = method.find("try", method.index(subscriptions[0]) - 200)
catch_match = re.search(
    r"catch\s*\{\s*DetachPendingLocked\(pending\);\s*throw;\s*\}",
    method,
    flags=re.DOTALL,
)
if try_at < 0 or catch_match is None:
    raise SystemExit("FAIL: native event attachment lacks fail-closed rollback/rethrow")
if not (try_at < method.index(subscriptions[0]) < method.index(subscriptions[-1]) < catch_match.start() < publish_at):
    raise SystemExit("FAIL: rollback must cover every event add and complete before _pending publication")

# Never repair this native boundary by retrying event registration.
if re.search(r"(?:while|for)\s*\([^)]*\)[\s\S]{0,500}Command(?:WillStart|Ended|Cancelled|Failed)\s*\+=", method):
    raise SystemExit("FAIL: native event subscription must not be retried")

print("PASS: MCP native-command event subscriptions publish atomically with rollback on attachment failure")
