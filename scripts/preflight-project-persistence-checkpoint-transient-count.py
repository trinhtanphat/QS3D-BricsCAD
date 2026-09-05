from pathlib import Path

SOURCE = Path("src/QS3D.Core/Persistence/ProjectPersistenceCheckpoint.cs")
STABILITY = Path("tests/QS3D.Core.SmokeTests/ProjectPersistenceCheckpointCountStabilitySmoke.cs")
TRAVERSAL = Path("tests/QS3D.Core.SmokeTests/ProjectPersistenceCheckpointCountTraversalSmoke.cs")

source = SOURCE.read_text(encoding="utf-8")
stability = STABILITY.read_text(encoding="utf-8")
traversal = TRAVERSAL.read_text(encoding="utf-8")

for token in [
    "RequireStableKnownCount",
    "Persistence checkpoint known element count changed during enumeration.",
    "var movedNext = enumerator.MoveNext();",
    "var rawId = enumerator.Current;",
    "RejectMalformedKnownCounts(elementIds)",
]:
    if token not in source:
        raise SystemExit(f"Persistence checkpoint transient Count guard missing production contract: {token}")

capture = source.index("public static ProjectPersistenceCheckpoint Capture")
helper = source.index("private static void RequireStableKnownCount", capture)
window = source[capture:helper]
move = window.index("var movedNext = enumerator.MoveNext();")
post_move_fence = window.index("RequireStableKnownCount(elementIds, expectedKnownCount.Value);", move)
current = window.index("var rawId = enumerator.Current;", move)
n_plus_one = window.index("observed >= expectedKnownCount.Value", move)
post_current_fence = window.index("RequireStableKnownCount(elementIds, expectedKnownCount.Value);", current)
if not move < post_move_fence < n_plus_one < current < post_current_fence:
    raise SystemExit("Persistence checkpoint must fence Count after MoveNext, refuse N+1 before Current, then fence Count after Current.")

get_enumerator = window.index("using var enumerator = elementIds.GetEnumerator();")
first_fence = window.index("RequireStableKnownCount(elementIds, expectedKnownCount.Value);", get_enumerator)
loop = window.index("while (true)", get_enumerator)
if not get_enumerator < first_fence < loop:
    raise SystemExit("Persistence checkpoint must fence authoritative Count immediately after enumerator acquisition.")

helper_end = source.index("private static int? RejectMalformedKnownCounts", helper)
helper_text = source[helper:helper_end]
if "var currentKnownCount = RejectMalformedKnownCounts(elementIds);" not in helper_text:
    raise SystemExit("Persistence checkpoint Count fence must preserve malformed Count diagnostics on every observation.")
if "currentKnownCount.Value != expectedKnownCount" not in helper_text:
    raise SystemExit("Persistence checkpoint Count fence must compare every observation to the admitted Count.")

for token in [
    "RejectsTransientCountDriftThatReturnsBeforeTraversalEnds",
    "TransientMoveNextDriftCollection",
    "Equal(1, source.MoveNextCalls",
    "Equal(0, source.CurrentReads",
]:
    if token not in stability:
        raise SystemExit(f"Persistence checkpoint transient Count regression missing: {token}")

for token in [
    "Equal(7, source.CountReads",
    "Equal(10, source.CountReads",
    "Equal(1, source.EnumerationCount",
]:
    if token not in traversal:
        raise SystemExit(f"Persistence checkpoint traversal compatibility contract missing: {token}")

print("PASS persistence checkpoint transient Count stability source guard")
