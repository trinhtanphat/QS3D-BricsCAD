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
        "var generation = _generation;",
        "_inFlightGeneration == generation",
        "_inFlightGeneration = generation;",
        "_inFlight = CheckCoreAsync(automatic, generation);",
        "if (!IsGenerationCurrent(generation)) return result;",
        "if (IsGenerationCurrent(generation)) Publish(result, false);",
        "lock (_sync) return _started && generation == _generation;",
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
    snapshot = text.find("var generation = _generation;", check)
    reuse = text.find("_inFlightGeneration == generation", snapshot)
    own = text.find("_inFlightGeneration = generation;", reuse)
    launch = text.find("_inFlight = CheckCoreAsync(automatic, generation);", own)
    if min(check, snapshot, reuse, own, launch) < 0 or not (
        check < snapshot < reuse < own < launch
    ):
        errors.append("CheckAsync must snapshot generation, reuse only same-generation work, then own and launch the new task")

    for forbidden in (
        "if (_inFlight != null && !_inFlight.IsCompleted) return _inFlight;",
    ):
        if forbidden in text:
            errors.append("generation-blind updater single-flight reuse returned: " + forbidden)

if errors:
    print("Updater lifecycle preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Updater lifecycle preflight PASS: stop/restart invalidates old single-flight ownership, only current-generation checks are reused, and stale results remain publish-blocked by generation.")
