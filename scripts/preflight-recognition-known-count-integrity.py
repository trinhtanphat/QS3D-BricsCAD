#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Recognition/RecognitionEngine.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/RecognitionBoundedEnumerationSmoke.cs"
errors = []

for path in (source_path, smoke_path):
    if not path.is_file():
        errors.append("missing recognition Count-integrity file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""

for token in (
    "var knownCount = ReadKnownCount(source, maxCount, label);",
    "EnsureKnownCountStable(source, knownCount, maxCount, label);",
    "changed its reported Count during enumeration",
    "enumerated more items than its reported Count",
    "reported Count \" + knownCount.Value + \" but enumerated",
):
    if token not in source:
        errors.append("RecognitionInputBounds missing Count-integrity token: " + token)

start = source.find("internal static List<T> Materialize<T>(IEnumerable<T> source, int maxCount, string label)")
end = source.find("private static void EnsureKnownCountStable", start)
materialize = source[start:end] if start >= 0 and end > start else ""
rebound = "EnsureKnownCountStable(source, knownCount, maxCount, label);"
move = "var moved = enumerator.MoveNext();"
known_guard = "materialized.Count >= knownCount.Value"
hard_guard = "materialized.Count >= maxCount"
current = "var current = enumerator.Current;"
if not materialize or "while (true)" not in materialize:
    errors.append("RecognitionInputBounds must use explicit traversal so Count can be rebound around caller-controlled boundaries.")
else:
    first = materialize.find(rebound)
    move_pos = materialize.find(move, first + len(rebound))
    second = materialize.find(rebound, move_pos + len(move))
    known_pos = materialize.find(known_guard, second + len(rebound))
    hard_pos = materialize.find(hard_guard, known_pos + len(known_guard))
    current_pos = materialize.find(current, hard_pos + len(hard_guard))
    third = materialize.find(rebound, current_pos + len(current))
    add_pos = materialize.find("materialized.Add(current);", third + len(rebound))
    final = materialize.find(rebound, add_pos + len("materialized.Add(current);"))
    if min(first, move_pos, second, known_pos, hard_pos, current_pos, third, add_pos, final) < 0 or not (
        first < move_pos < second < known_pos < hard_pos < current_pos < third < add_pos < final
    ):
        errors.append("Recognition traversal must enforce Count rebound -> MoveNext -> Count rebound -> admitted/hard-cap gates -> Current -> Count rebound -> retain -> final rebound.")

for token in (
    "KnownCountDriftFailsClosed",
    "DriftBoundary.MoveNext",
    "DriftBoundary.Current",
    "changed its reported Count",
    "StableCountCollection<RecognitionRule>",
    "StreamSingle(rule)",
):
    if token not in smoke:
        errors.append("Recognition bounded-enumeration smoke missing hostile/stable Count token: " + token)

print("QS3D recognition known-Count integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: recognition counted inputs rebound Count around MoveNext/Current and fail closed before retaining drifted items while streaming inputs remain supported.")
