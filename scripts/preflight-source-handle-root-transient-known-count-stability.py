#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleRootTransientKnownCountStabilitySmoke.cs"
current_smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleRootCurrentCountStabilitySmoke.cs"
legacy_guard_path = ROOT / "scripts/preflight-source-handle-root-known-count-integrity.py"
errors = []

for path in (source_path, smoke_path, current_smoke_path, legacy_guard_path):
    if not path.is_file():
        errors.append("missing Locate root Count-stability file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""
current_smoke = current_smoke_path.read_text(encoding="utf-8") if current_smoke_path.is_file() else ""
legacy_guard = legacy_guard_path.read_text(encoding="utf-8") if legacy_guard_path.is_file() else ""

for token in (
    "RequireStableKnownCountDuringTraversal(elementIds, knownCount);",
    "invalid negative known Count value during traversal",
    "conflicting known Count values during traversal",
    "known Count changed during traversal",
):
    if token not in source:
        errors.append("SourceHandleResolver missing traversal Count contract token: " + token)

for token in (
    "[ModuleInitializer]",
    "TransientGrowthRejectsBeforeCurrent",
    "TransientShrinkRejectsBeforeCurrent",
    "TransientNegativeRejectsBeforeCurrent",
    "TransientConflictRejectsBeforeCurrent",
    "StableCountStillResolves",
    "StreamingInputStillResolves",
    "MoveNextCalls == 1 && source.CurrentReads == 0",
):
    if token not in smoke:
        errors.append("Transient Locate root smoke missing token: " + token)

for token in (
    "[ModuleInitializer]",
    "CurrentGrowthRejectsBeforeNextMoveNext",
    "CurrentShrinkRejectsBeforeNextMoveNext",
    "CurrentNegativeRejectsBeforeNextMoveNext",
    "CurrentConflictRejectsBeforeNextMoveNext",
    "MoveNextCalls == 1 && source.CurrentReads == 1",
    "StableCountStillResolves",
):
    if token not in current_smoke:
        errors.append("Current-induced Locate root smoke missing token: " + token)

start = source.find("private static IReadOnlyList<string> MaterializeRootElementIds(IEnumerable<string> elementIds)")
end = source.find("private static void RevalidateKnownCountAfterTraversal", start)
materialize = source[start:end] if start >= 0 and end > start else ""
rebound = "RequireStableKnownCountDuringTraversal(elementIds, knownCount);"
move = "if (!enumerator.MoveNext())"
current = "var rawId = enumerator.Current;"
if not materialize or "while (true)" not in materialize:
    errors.append("Locate root traversal must use explicit loop control so Count can be rebound before MoveNext.")
else:
    first_rebound = materialize.find(rebound)
    move_pos = materialize.find(move, first_rebound + len(rebound))
    second_rebound = materialize.find(rebound, move_pos + len(move))
    known_guard = materialize.find("inputCount >= knownCount.Value", second_rebound + len(rebound))
    hard_guard = materialize.find("inputCount >= MaxRootElementIdInputCount", known_guard)
    current_pos = materialize.find(current, hard_guard)
    third_rebound = materialize.find(rebound, current_pos + len(current))
    if min(first_rebound, move_pos, second_rebound, known_guard, hard_guard, current_pos, third_rebound) < 0 or not (
        first_rebound < move_pos < second_rebound < known_guard < hard_guard < current_pos < third_rebound
    ):
        errors.append("Locate root traversal must enforce Count rebound -> MoveNext -> Count rebound -> admitted/hard-cap gates -> Current -> Count rebound.")

for token in (
    "Locate root selection must enforce Count rebound -> MoveNext -> Count rebound -> known-Count guard -> Current -> Count rebound",
    "RevalidateKnownCountAfterTraversal(elementIds, knownCount);",
):
    if token not in legacy_guard:
        errors.append("Historical Locate root Count-integrity guard must pin stronger ordering without losing final rebound: " + token)

print("QS3D SourceHandleResolver transient root known-Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Locate root Count is rebound before/after MoveNext and immediately after Current, rejecting transient drift before further caller traversal.")
