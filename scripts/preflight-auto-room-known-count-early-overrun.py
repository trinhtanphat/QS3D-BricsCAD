#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleKnownCountTraversalSmoke.cs"
BOUND_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomStaleSelectionBoundSmoke.cs"
errors = []

for path, label in ((SOURCE, "AutoRoomLifecycle source"), (SMOKE, "known-count smoke"), (BOUND_SMOKE, "stale-selection bound smoke")):
    if not path.is_file():
        errors.append("missing " + label)

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
bound_smoke = BOUND_SMOKE.read_text(encoding="utf-8") if BOUND_SMOKE.is_file() else ""

for token in (
    "private static void RequireCanProcessNextKnownCount(",
    'RequireCanProcessNextKnownCount("Auto Room active room id set", knownActiveRoomCount, activeInputCount);',
    'RequireKnownCountMatchesTraversal("Auto Room active room id set", knownActiveRoomCount, activeInputCount);',
    'if (selectedInputCount >= knownSelectedSourceHandleCount)',
    'RequireKnownCountMatchesTraversal("Auto Room selected source handle set", knownSelectedSourceHandleCount, selectedInputCount);',
):
    if token not in source:
        errors.append("Auto Room source missing Count-integrity contract: " + token)

active_move = source.find("while (activeEnumerator.MoveNext())")
active_guard = source.find('RequireCanProcessNextKnownCount("Auto Room active room id set"', active_move)
active_current = source.find("var rawRoomId = activeEnumerator.Current;", active_move)
active_increment = source.find("activeInputCount++;", active_move)
active_semantic = source.find("string.IsNullOrWhiteSpace(rawRoomId)", active_move)
if not (0 <= active_move < active_guard < active_current < active_increment < active_semantic):
    errors.append("active-room known-count guard must precede Current/increment/semantic processing")

selected_loop = source.find("while (selectedEnumerator.MoveNext())")
selected_capacity = source.find("selectedInputCount >= MaxSourceHandleInputCount", selected_loop)
selected_drift = source.find("selectedInputCount >= knownSelectedSourceHandleCount", selected_loop)
selected_continue = source.find("continue;", selected_drift)
selected_current = source.find("var raw = selectedEnumerator.Current;", selected_loop)
selected_normalize = source.find("GeneratedHandleIdentity.Normalize(raw)", selected_loop)
selected_final = source.find('RequireKnownCountMatchesTraversal("Auto Room selected source handle set"', selected_loop)
if not (0 <= selected_loop < selected_capacity < selected_drift < selected_continue < selected_current < selected_normalize < selected_final):
    errors.append("selected-source drift quarantine must follow hard-bound enforcement and precede Current/handle normalization/final Count mismatch")

for token in (
    "ActiveOverYieldFailsAtFirstUnexpectedItem();",
    "SelectedOverYieldRemainsCardinalityOnlyAfterKnownCount();",
    "CountingMisreportedSet<string>(1, \"ROOM-1\", \"ROOM-2\", \"ROOM-3\")",
    "CountingMisreportedSet<string>(1, \"A\", \"B\", \"C\")",
    "Equal(2, active.MoveNextCalls",
    "Equal(4, selected.MoveNextCalls",
    "SelectedCapacityPreflightStillPrecedesEnumeration();",
    "ExactKnownCountsRemainAccepted();",
):
    if token not in smoke:
        errors.append("Auto Room smoke missing Count-drift regression/control: " + token)

for token in (
    "DishonestCountStopsAtFirstDisallowedEntry();",
    'Contains("cannot exceed 5000", error.Message',
    "Equal(MaximumSourceHandles + 1, handles.ObservedEntries",
):
    if token not in bound_smoke:
        errors.append("existing Auto Room hard-bound regression must remain authoritative: " + token)

print("QS3D Auto Room known-count processing preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Auto Room active-room overrun fails before Current processing; selected-source drift is quarantined before Current/normalization while the independent 5000-entry hard bound retains precedence.")
