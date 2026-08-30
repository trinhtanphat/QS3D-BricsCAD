from pathlib import Path
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
    "while (true)",
    "RequireStableKnownTargetIdCounts(elementIds, knownCount)",
    "if (!enumerator.MoveNext()) break",
    "if (knownCount.HasValue && index >= knownCount.Value)",
    "var value = enumerator.Current",
    "if (result.Contains(raw))",
    "if (result.Count >= maxCount)",
]
for token in required_source:
    if token not in method:
        errors.append("missing source integrity token: " + token)

if "foreach (var value in elementIds)" in method:
    errors.append("CanonicalTargetIds regressed to caller-controlled foreach")
if "while (enumerator.MoveNext())" in method:
    errors.append("CanonicalTargetIds regressed to MoveNext without a pre-move Count rebound")
if method.count("RequireStableKnownTargetIdCounts(elementIds, knownCount)") < 3:
    errors.append("CanonicalTargetIds must rebind Count before MoveNext, after successful MoveNext, and at finalization")

ordered = [
    "RequireStableKnownTargetIdCounts(elementIds, knownCount)",
    "if (!enumerator.MoveNext()) break",
]
positions = [method.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions):
    errors.append("required Count rebound -> MoveNext ordering is not preserved")

move = method.find("if (!enumerator.MoveNext()) break")
post_rebound = method.find("RequireStableKnownTargetIdCounts(elementIds, knownCount)", move + 1)
overrun = method.find("if (knownCount.HasValue && index >= knownCount.Value)", move + 1)
current = method.find("var value = enumerator.Current", move + 1)
if min(move, post_rebound, overrun, current) < 0 or not (move < post_rebound < overrun < current):
    errors.append("required MoveNext -> Count rebound -> known Count overrun -> Current ordering is not preserved")

required_smoke = [
    "KnownCountOverrunRejectsBeforeUnexpectedCurrent",
    "KnownCountUnderYieldStillFails",
    "PostTraversalCountDriftFailsClosed",
    "MoveNextTransientGrowthRejectsBeforeCurrent",
    "MoveNextTransientShrinkRejectsBeforeCurrent",
    "MoveNextTransientNegativeRejectsBeforeCurrent",
    "MoveNextTransientCrossInterfaceConflictRejectsBeforeCurrent",
    "ExactKnownCountRemainsAccepted",
    "PureStreamingRemainsAccepted",
    "MoveNextCalls",
    "CurrentReads",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic smoke token: " + token)

for hostile in [
    "MoveNextTransientGrowthRejectsBeforeCurrent",
    "MoveNextTransientShrinkRejectsBeforeCurrent",
    "MoveNextTransientNegativeRejectsBeforeCurrent",
    "MoveNextTransientCrossInterfaceConflictRejectsBeforeCurrent",
]:
    start = smoke.find("private static void " + hostile)
    end = smoke.find("private static void ", start + 1)
    body = smoke[start:end if end >= 0 else len(smoke)] if start >= 0 else ""
    if "Equal(0, source.CurrentReads)" not in body:
        errors.append(hostile + " must prove rejection before caller Current")

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS regeneration subset known-Count Current integrity source guard")