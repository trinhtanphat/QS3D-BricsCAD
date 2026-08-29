#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
source_path = root / "src/QS3D.Core/Rebar/RebarSchedule.cs"
smoke_path = root / "tests/QS3D.Core.SmokeTests/RebarScheduleKnownCountIntegritySmoke.cs"

source = source_path.read_text(encoding="utf-8")
smoke = smoke_path.read_text(encoding="utf-8")

start = source.index("public static IReadOnlyList<RebarScheduleRow> Build(IEnumerable<RebarScheduleInput> inputs)")
end = source.index("private static void Append", start)
build = source[start:end]

errors = []
if "foreach (var input in inputs)" in build:
    errors.append("Build must not foreach caller-controlled rebar schedule inputs")

required_build = [
    "using (var enumerator = inputs.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "observedInputCount >= expectedInputCount.Value",
    "rows.Count >= MaxRowCount",
    "var input = enumerator.Current;",
    "var finalInputCount = ValidateKnownInputCount(inputs, nameof(inputs));",
    "Rebar schedule input known Count changed during traversal.",
]
for needle in required_build:
    if needle not in build:
        errors.append("missing Build contract: " + needle)

move = build.find("while (enumerator.MoveNext())")
count_guard = build.find("observedInputCount >= expectedInputCount.Value")
row_guard = build.find("rows.Count >= MaxRowCount")
current = build.find("var input = enumerator.Current;")
if not (move >= 0 and count_guard > move and row_guard > count_guard and current > row_guard):
    errors.append("Build ordering must be MoveNext -> known Count guard -> row guard -> Current")

required_smoke = [
    "RejectKnownCountOverEnumerationBeforeCurrent();",
    "RejectPostTraversalCountDrift();",
    "RejectPostTraversalNegativeCount();",
    "RejectPostTraversalCountConflict();",
    "PreserveStreamingRowBoundaryBeforeCurrent();",
    "CurrentReads != 1",
    "CurrentReads != MaxRowCount",
]
for needle in required_smoke:
    if needle not in smoke:
        errors.append("missing deterministic smoke contract: " + needle)

if errors:
    for error in errors:
        print("ERROR: " + error, file=sys.stderr)
    raise SystemExit(1)

print("PASS rebar schedule known-Count no-overread/stability preflight")
