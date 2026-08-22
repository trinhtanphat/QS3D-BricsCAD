#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs"


def fail(message: str) -> None:
    print("Updater generation publication preflight FAILED:", file=sys.stderr)
    print("- " + message, file=sys.stderr)
    raise SystemExit(1)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


if not SOURCE.is_file():
    fail("missing UpdateCoordinator.cs")

text = SOURCE.read_text(encoding="utf-8")
helper_signature = "private bool TryPublishCurrent(int generation, UpdateCheckResult result, bool automaticNotification)"
require(helper_signature in text, "missing generation-aware publication helper")

helper_start = text.index(helper_signature)
helper_end_marker = "private static SemanticReleaseVersion GetCurrentVersion()"
helper_end = text.find(helper_end_marker, helper_start)
require(helper_end >= 0, "cannot isolate generation-aware publication helper")
helper = text[helper_start:helper_end]

lock_pos = helper.find("lock (_sync)")
current_pos = helper.find("if (!_started || generation != _generation) return false;", lock_pos)
state_pos = helper.find("_last = result;", current_pos)
dispatcher_capture_pos = helper.find("dispatcher = _dispatcher;", state_pos)
require(
    min(lock_pos, current_pos, state_pos, dispatcher_capture_pos) >= 0
    and lock_pos < current_pos < state_pos < dispatcher_capture_pos,
    "generation freshness, _last mutation and dispatcher capture must be ordered in one coordinator critical section",
)

callback_pos = helper.find("Action publish = () =>")
callback_lock_pos = helper.find("lock (_sync)", callback_pos)
callback_current_pos = helper.find("if (!_started || generation != _generation) return;", callback_lock_pos)
state_event_pos = helper.find("StateChanged?.Invoke(this, result);", callback_current_pos)
auto_event_pos = helper.find("AutomaticUpdateFound?.Invoke(this, result);", state_event_pos)
require(
    min(callback_pos, callback_lock_pos, callback_current_pos, state_event_pos, auto_event_pos) >= 0
    and callback_pos < callback_lock_pos < callback_current_pos < state_event_pos < auto_event_pos,
    "dispatcher delivery must revalidate the same active generation before StateChanged/AutomaticUpdateFound",
)

require(
    re.search(
        r"TryPublishCurrent\s*\(\s*generation\s*,\s*new UpdateCheckResult\(UpdateState\.Checking",
        text,
        re.S,
    )
    is not None,
    "CheckCoreAsync must publish Checking through the generation-aware helper",
)
require(
    "TryPublishCurrent(generation, result, automatic && result.HasUpdate);" in text,
    "successful async checks must publish through the generation-aware helper",
)
require(
    "TryPublishCurrent(generation, result, false);" in text,
    "async check errors must publish through the generation-aware helper",
)
require(
    "if (lifecycleCurrent) TryPublishCurrent(generation, failed, false);" in text,
    "schedule failure publication must remain bound to the captured current lifecycle",
)
require(
    "TryPublishCurrent(generation, scheduled, false);" in text,
    "scheduled state publication must use the generation-aware helper",
)

for forbidden in (
    "private bool IsGenerationCurrent(int generation)",
    "if (!IsGenerationCurrent(generation)) return result;",
    "if (IsGenerationCurrent(generation)) Publish(result, false);",
    "private void Publish(UpdateCheckResult result, bool automaticNotification)",
):
    require(forbidden not in text, "split generation/publication pattern returned: " + forbidden)

check_start = text.find("private Task<UpdateCheckResult> CheckAsync(bool automatic)")
check_end = text.find("private async Task<UpdateCheckResult> CheckCoreAsync", check_start)
require(check_start >= 0 and check_end > check_start, "cannot isolate CheckAsync")
check_body = text[check_start:check_end]
require(
    "if (!_started)" in check_body and "return Task.FromResult(_last);" in check_body,
    "stopped coordinators must keep refusing new refresh/network work",
)

print(
    "Updater generation publication preflight PASS: lifecycle-visible state is generation-atomic and queued dispatcher notifications revalidate lifecycle ownership before delivery."
)
