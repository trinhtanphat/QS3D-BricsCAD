from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationPreviewService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RegenerationPreviewTargetCountCurrentIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
errors = []

start = source.find("private static IReadOnlyList<string> CanonicalPreviewTargets")
end = source.find("private static RegenerationEngine NewEngine", start)
if start < 0 or end < 0:
    errors.append("cannot locate CanonicalPreviewTargets boundary")
    method = ""
else:
    method = source[start:end]

required_source = [
    "using (var enumerator = elementIds.GetEnumerator())",
    "while (true)",
    "RequireStableKnownPreviewTargetCount(elementIds, knownCount)",
    "if (!enumerator.MoveNext()) break",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= maxCount)",
    "var value = enumerator.Current",
]
for token in required_source:
    if token not in method:
        errors.append("missing source integrity token: " + token)

if "foreach (var value in elementIds)" in method:
    errors.append("CanonicalPreviewTargets regressed to caller-controlled foreach")
if "while (enumerator.MoveNext())" in method:
    errors.append("CanonicalPreviewTargets regressed to MoveNext without a pre-move Count rebound")
if method.count("RequireStableKnownPreviewTargetCount(elementIds, knownCount)") < 3:
    errors.append("CanonicalPreviewTargets must rebind Count before MoveNext, after successful MoveNext, and at finalization")

move = method.find("if (!enumerator.MoveNext()) break")
post_rebound = method.find("RequireStableKnownPreviewTargetCount(elementIds, knownCount)", move + 1)
known_overrun = method.find("if (knownCount.HasValue && result.Count >= knownCount.Value)", move + 1)
project_overrun = method.find("if (result.Count >= maxCount)", move + 1)
current = method.find("var value = enumerator.Current", move + 1)
if min(move, post_rebound, known_overrun, project_overrun, current) < 0 or not (move < post_rebound < known_overrun < project_overrun < current):
    errors.append("required MoveNext -> Count rebound -> known/project bounds -> Current ordering is not preserved")

required_smoke = [
    "KnownCountOverrunRejectsBeforeUnexpectedCurrent",
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

print("PASS regeneration preview target known-Count Current integrity source guard")
