from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationEngine.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RegenerationSubsetKnownCountCurrentIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

errors = []

start = source.find("private static HashSet<string> CanonicalTargetIds")
end = source.find("private static int? ValidateKnownTargetIdCounts", start)
if start < 0 or end < 0:
    errors.append("cannot locate CanonicalTargetIds boundary")
    method = ""
else:
    method = source[start:end]

required_source = [
    "using (var enumerator = elementIds.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && index >= knownCount.Value)",
    "if (index >= maxCount)",
    "var value = enumerator.Current",
    "var reboundCount = ValidateKnownTargetIdCounts(elementIds)",
    "if (reboundCount != knownCount)",
]
for token in required_source:
    if token not in method:
        errors.append("missing source integrity token: " + token)

if "foreach (var value in elementIds)" in method:
    errors.append("CanonicalTargetIds regressed to caller-controlled foreach")

ordered = [
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && index >= knownCount.Value)",
    "if (index >= maxCount)",
    "var value = enumerator.Current",
]
positions = [method.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions):
    errors.append("required MoveNext -> known Count -> project bound -> Current ordering is not preserved")

required_smoke = [
    "KnownCountOverrunRejectsBeforeUnexpectedCurrent",
    "ProjectBoundRejectsStreamingInputBeforeUnexpectedCurrent",
    "KnownCountUnderYieldStillFails",
    "PostTraversalCountDriftFailsClosed",
    "ExactKnownCountRemainsAccepted",
    "MoveNextCalls",
    "CurrentReads",
    "CountReads",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic smoke token: " + token)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS regeneration subset known-Count Current integrity source guard")
