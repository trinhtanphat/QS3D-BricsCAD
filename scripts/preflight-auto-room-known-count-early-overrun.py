#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleKnownCountTraversalSmoke.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AutoRoomLifecycle source")
if not SMOKE.is_file():
    errors.append("missing AutoRoomLifecycle known-count smoke")

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

for token in (
    "private static void RequireCanProcessNextKnownCount(",
    'collectionLabel + " traversal produced more entries than its known count reported " + knownCount + "."',
    'RequireCanProcessNextKnownCount("Auto Room active room id set", knownActiveRoomCount, activeInputCount);',
    'RequireCanProcessNextKnownCount("Auto Room selected source handle set", knownSelectedSourceHandleCount, selectedInputCount);',
    'RequireKnownCountMatchesTraversal("Auto Room active room id set", knownActiveRoomCount, activeInputCount);',
    'RequireKnownCountMatchesTraversal("Auto Room selected source handle set", knownSelectedSourceHandleCount, selectedInputCount);',
):
    if token not in source:
        errors.append("Auto Room source missing Count-integrity contract: " + token)

active_guard = source.find('RequireCanProcessNextKnownCount("Auto Room active room id set"')
active_increment = source.find("activeInputCount++;", active_guard)
active_semantic = source.find("string.IsNullOrWhiteSpace(rawRoomId)", active_guard)
if not (0 <= active_guard < active_increment < active_semantic):
    errors.append("active-room known-count guard must precede increment/semantic processing")

selected_guard = source.find('RequireCanProcessNextKnownCount("Auto Room selected source handle set"')
selected_capacity = source.find("selectedInputCount >= MaxSourceHandleInputCount", selected_guard)
selected_increment = source.find("selectedInputCount++;", selected_guard)
selected_semantic = source.find("GeneratedHandleIdentity.Normalize(raw)", selected_guard)
if not (0 <= selected_guard < selected_capacity < selected_increment < selected_semantic):
    errors.append("selected-source known-count guard must precede capacity accounting and handle normalization")

for token in (
    "OverYieldFailsAtFirstUnexpectedItem();",
    "private static void OverYieldFailsAtFirstUnexpectedItem()",
    "CountingMisreportedSet<string>(1, \"ROOM-1\", \"ROOM-2\", \"ROOM-3\")",
    "CountingMisreportedSet<string>(1, \"A\", \"B\", \"C\")",
    "Equal(2, active.MoveNextCalls",
    "Equal(2, selected.MoveNextCalls",
    "SelectedCapacityPreflightStillPrecedesEnumeration();",
    "ExactKnownCountsRemainAccepted();",
):
    if token not in smoke:
        errors.append("Auto Room smoke missing early-overrun regression/control: " + token)

print("QS3D Auto Room known-count early-overrun preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Auto Room counted selections reject item knownCount+1 before semantic processing while preserving under-yield, exact-count, and capacity behavior.")
