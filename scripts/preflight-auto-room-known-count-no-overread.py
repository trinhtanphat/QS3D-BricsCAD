#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleKnownCountNoOverreadSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

for token, label in (
    ("foreach (var rawRoomId in activeRoomIds)", "active room ids"),
    ("foreach (var raw in selectedSourceHandles)", "selected source handles"),
):
    if token in source:
        errors.append(label + " still uses foreach and can observe Current before Count/cap decisions")

active_move = source.find("while (activeEnumerator.MoveNext())")
active_guard = source.find('RequireCanProcessNextKnownCount("Auto Room active room id set"', active_move)
active_current = source.find("var rawRoomId = activeEnumerator.Current;", active_move)
if not (active_move >= 0 and active_guard > active_move and active_current > active_guard):
    errors.append("active room traversal must enforce known Count before Current")

selected_move = source.find("while (selectedEnumerator.MoveNext())")
selected_cap = source.find("selectedInputCount >= MaxSourceHandleInputCount", selected_move)
selected_known = source.find("selectedInputCount >= knownSelectedSourceHandleCount", selected_move)
selected_current = source.find("var raw = selectedEnumerator.Current;", selected_move)
if not (selected_move >= 0 and selected_cap > selected_move and selected_known > selected_cap and selected_current > selected_known):
    errors.append("selected source traversal must enforce hard cap and known-Count discard before Current")

for token in (
    "CurrentReads",
    "ActiveOverrunDoesNotReadUnexpectedCurrent();",
    "SelectedCountDriftDoesNotReadIgnoredCurrent();",
    "SelectedHardCapRejectsBeforeCurrent5001();",
    "Equal(1, active.CurrentReads",
    "Equal(1, selected.CurrentReads",
    "Equal(5000, selected.CurrentReads",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("no-overread smoke missing token: " + token)

print("QS3D Auto Room known-Count no-overread preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Auto Room stale-selection boundaries decide Count/cap state before caller-controlled Current.")
