#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing UpdateCoordinator.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "private int _inFlightGeneration = -1;",
        "_generation++;",
        "_inFlight = null;",
        "_inFlightGeneration = -1;",
        "if (!_started)",
        "return Task.FromResult(_last);",
        "var generation = _generation;",
        "_inFlightGeneration == generation",
        "_inFlightGeneration = generation;",
        "_inFlight = CheckCoreAsync(automatic, generation);",
        "private bool TryPublishCurrent(int generation, UpdateCheckResult result, bool automaticNotification)",
        "if (!_started || generation != _generation) return false;",
        "if (!_started || generation != _generation) return;",
        "var generation = CaptureGeneration();",
        "private int CaptureGeneration()",
        "TryScheduleCurrentGeneration(generation, release, out var lifecycleCurrent, out var error)",
        "private bool TryScheduleCurrentGeneration(int generation, UpdateReleaseInfo release, out bool lifecycleCurrent, out string error)",
        "lifecycleCurrent = _started && generation == _generation;",
        "if (!lifecycleCurrent)",
        "return SecureUpdateLauncher.TrySchedule(release, out error);",
        "if (lifecycleCurrent) TryPublishCurrent(generation, failed, false);",
        "TryPublishCurrent(generation, scheduled, false);",
    ):
        if token not in text:
            errors.append("UpdateCoordinator lifecycle contract missing: " + token)

    stop = text.find("internal void Stop()")
    stop_generation = text.find("_generation++;", stop)
    stop_task = text.find("_inFlight = null;", stop_generation)
    stop_owner = text.find("_inFlightGeneration = -1;", stop_task)
    if min(stop, stop_generation, stop_task, stop_owner) < 0 or not (
        stop < stop_generation < stop_task < stop_owner
    ):
        errors.append("Stop must advance generation and release old single-flight ownership before a later Start")

    check = text.find("private Task<UpdateCheckResult> CheckAsync(bool automatic)")
    stopped_guard = text.find("if (!_started)", check)
    stopped_return = text.find("return Task.FromResult(_last);", stopped_guard)
    snapshot = text.find("var generation = _generation;", stopped_return)
    reuse = text.find("_inFlightGeneration == generation", snapshot)
    own = text.find("_inFlightGeneration = generation;", reuse)
    launch = text.find("_inFlight = CheckCoreAsync(automatic, generation);", own)
    if min(check, stopped_guard, stopped_return, snapshot, reuse, own, launch) < 0 or not (
        check < stopped_guard < stopped_return < snapshot < reuse < own < launch
    ):
        errors.append(
            "CheckAsync must refuse stopped lifecycle work before generation snapshot, same-generation reuse, ownership and network-task launch"
        )

    check_end = text.find("private async Task<UpdateCheckResult> CheckCoreAsync", check)
    check_body = text[check:check_end if check_end >= 0 else len(text)]
    if check_body.count("CheckCoreAsync(automatic, generation)") != 1:
        errors.append("CheckAsync must expose exactly one guarded CheckCoreAsync launch path")

    publish = text.find("private bool TryPublishCurrent(int generation, UpdateCheckResult result, bool automaticNotification)")
    publish_lock = text.find("lock (_sync)", publish)
    publish_current = text.find("if (!_started || generation != _generation) return false;", publish_lock)
    publish_state = text.find("_last = result;", publish_current)
    dispatcher_guard = text.find("if (!_started || generation != _generation) return;", publish_state)
    state_event = text.find("StateChanged?.Invoke(this, result);", dispatcher_guard)
    if min(publish, publish_lock, publish_current, publish_state, dispatcher_guard, state_event) < 0 or not (
        publish < publish_lock < publish_current < publish_state < dispatcher_guard < state_event
    ):
        errors.append(
            "Lifecycle publication must atomically reject stale generations before state mutation and revalidate generation before dispatcher delivery"
        )

    schedule = text.find("internal async Task<UpdateCheckResult> ScheduleLatestAsync()")
    schedule_generation = text.find("var generation = CaptureGeneration();", schedule)
    await_check = text.find("await CheckAsync(false).ConfigureAwait(false);", schedule_generation)
    release = text.find("var release = fresh.Release;", await_check)
    authorize = text.find("TryScheduleCurrentGeneration(generation, release, out var lifecycleCurrent, out var error)", release)
    if min(schedule, schedule_generation, await_check, release, authorize) < 0 or not (
        schedule < schedule_generation < await_check < release < authorize
    ):
        errors.append("ScheduleLatestAsync must freeze lifecycle generation before await and authorize scheduling only afterward")

    helper = text.find("private bool TryScheduleCurrentGeneration")
    helper_lock = text.find("lock (_sync)", helper)
    helper_current = text.find("lifecycleCurrent = _started && generation == _generation;", helper_lock)
    helper_refuse = text.find("if (!lifecycleCurrent)", helper_current)
    helper_schedule = text.find("return SecureUpdateLauncher.TrySchedule(release, out error);", helper_refuse)
    if min(helper, helper_lock, helper_current, helper_refuse, helper_schedule) < 0 or not (
        helper < helper_lock < helper_current < helper_refuse < helper_schedule
    ):
        errors.append("Detached updater launch must be lock-linearized behind the active captured lifecycle generation")

    schedule_end = text.find("private int CaptureGeneration()", schedule)
    schedule_body = text[schedule:schedule_end if schedule_end >= 0 else len(text)]
    if "SecureUpdateLauncher.TrySchedule(" in schedule_body:
        errors.append("ScheduleLatestAsync must not call SecureUpdateLauncher directly outside lifecycle authorization")

    for forbidden in (
        "if (_inFlight != null && !_inFlight.IsCompleted) return _inFlight;",
        "if (!IsGenerationCurrent(generation)) return result;",
        "if (IsGenerationCurrent(generation)) Publish(result, false);",
        "private bool IsGenerationCurrent(int generation)",
    ):
        if forbidden in text:
            errors.append("generation-blind or split lifecycle pattern returned: " + forbidden)

if errors:
    print("Updater lifecycle preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Updater lifecycle preflight PASS: stopped coordinators refuse new manual checks before network launch, restart isolates single-flight work by generation, lifecycle state/event publication is generation-atomic, and detached scheduling stays bound to the active captured lifecycle.")
