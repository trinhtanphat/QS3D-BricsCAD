#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleRootTransientKnownCountStabilitySmoke.cs"
legacy_guard_path = ROOT / "scripts/preflight-source-handle-root-known-count-integrity.py"
errors = []

for path in (source_path, smoke_path, legacy_guard_path):
    if not path.is_file():
        errors.append("missing transient Locate root Count-stability file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""
legacy_guard = legacy_guard_path.read_text(encoding="utf-8") if legacy_guard_path.is_file() else ""

for token in (
    "RequireStableKnownCountDuringTraversal(elementIds, knownCount);",
    "invalid negative known Count value during traversal",
    "conflicting known Count values during traversal",
    "known Count changed during traversal",
):
    if token not in source:
        errors.append("SourceHandleResolver missing transient Count contract token: " + token)

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

start = source.find("private static IReadOnlyList<string> MaterializeRootElementIds(IEnumerable<string> elementIds)")
end = source.find("private static void RevalidateKnownCountAfterTraversal", start)
materialize = source[start:end] if start >= 0 and end > start else ""
ordered = (
    "while (enumerator.MoveNext())",
    "RequireStableKnownCountDuringTraversal(elementIds, knownCount);",
    "inputCount >= knownCount.Value",
    "inputCount >= MaxRootElementIdInputCount",
    "var rawId = enumerator.Current;",
)
positions = [materialize.find(token) for token in ordered]
if not materialize or any(pos < 0 for pos in positions) or positions != sorted(positions):
    errors.append("Locate root traversal must enforce MoveNext -> Count rebound -> admitted/hard-cap gates -> Current.")

for token in (
    "Locate root selection must enforce MoveNext -> traversal Count rebound -> known-Count guard -> Current",
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

print("PASS: transient Locate root Count drift is rejected after MoveNext and before semantic Current.")
