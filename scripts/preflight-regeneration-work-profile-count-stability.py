from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfileCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var knownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);",
    "result.Count >= knownCount.Value",
    "var postTraversalKnownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);",
    "knownCount != postTraversalKnownCount",
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

print("PASS regeneration work profile known-Count stability source guard")
