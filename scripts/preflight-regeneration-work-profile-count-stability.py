from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfileCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var knownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);",
    "result.Count >= knownCount.Value",
    "RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);",
    "var observedKnownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);",
    "observedKnownCount != knownCount",
    "known Count changed during traversal",
]
required_smoke = [
    "TargetCountDriftFailsClosed();",
    "WorkItemCountDriftFailsClosed();",
    "CategoryCountDriftFailsClosed();",
    "StableCountedAndStreamingSourcesRemainAccepted();",
    "DriftingCountCollection<T>",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Regeneration profile Count-stability preflight failed; missing: " + ", ".join(missing))

materialize_start = source.find("private static IReadOnlyList<T> MaterializeBounded<T>")
helper_start = source.find("private static void RequireStableKnownCountContract<T>", materialize_start)
validate_start = source.find("private static int? ValidateKnownCountContract<T>", helper_start)
if materialize_start < 0 or helper_start < 0 or validate_start < 0:
    raise SystemExit("Regeneration profile Count-stability preflight failed; missing materialization/stability helper boundary")

materialize = source[materialize_start:helper_start]
stable = "RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);"
move_next = materialize.find("if (!enumerator.MoveNext()) break;")
current = materialize.find("var value = enumerator.Current;")
first_stable = materialize.find(stable)
second_stable = materialize.find(stable, move_next + 1) if move_next >= 0 else -1
final_stable = materialize.rfind(stable)
if min(first_stable, move_next, second_stable, current, final_stable) < 0 or not (
    first_stable < move_next < second_stable < current < final_stable
):
    raise SystemExit(
        "Regeneration profile Count-stability preflight failed; stable Count evidence must be rebound before MoveNext, after successful MoveNext before Current, and after traversal"
    )

print("PASS regeneration work profile known-Count stability source guard")