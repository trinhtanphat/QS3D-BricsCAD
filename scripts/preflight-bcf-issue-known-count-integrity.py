#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfIssueExchange.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueKnownCountIntegritySmoke.cs"
LEGACY_BOUND_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeCollectionBoundSmoke.cs"

for path, label in (
    (SOURCE, "BCF production source"),
    (SMOKE, "BCF current-read integrity smoke"),
    (LEGACY_BOUND_SMOKE, "BCF legacy streaming-bound smoke"),
):
    if not path.is_file():
        raise SystemExit(f"BCF known-Count integrity guard missing {label}: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
legacy = LEGACY_BOUND_SMOKE.read_text(encoding="utf-8")

start = source.index("internal static List<T> MaterializeBounded<T>(")
end = source.index("private static int? ValidateKnownCounts<T>(", start)
body = source[start:end]

required = [
    "using (var enumerator = values.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCounts(",
    "if (!enumerator.MoveNext())",
    "if (corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (!corroboratedKnownCount && values is ICollection<T> && knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= maximumCount)",
    "var value = enumerator.Current;",
    "items.Add(value);",
    "if (knownCount.HasValue && observedCount != knownCount.Value)",
    "expectedKnownCountSources != currentKnownCountSources",
    "expectedCorroboratedKnownCount != currentCorroboratedKnownCount",
    "expectedKnownCount != currentKnownCount",
]
for token in required:
    if token not in body:
        raise SystemExit(f"BCF known-Count integrity guard missing required token: {token}")

if "foreach (var value in values)" in body:
    raise SystemExit("BCF known-Count integrity guard found unsafe caller-controlled foreach traversal")

loop = body.index("while (true)")
pre_move_rebind = body.index("RequireStableKnownCounts(", loop)
move_next = body.index("if (!enumerator.MoveNext())", pre_move_rebind)
post_move_rebind = body.index("RequireStableKnownCounts(", move_next + len("if (!enumerator.MoveNext())"))
corroborated_guard = body.index(
    "if (corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value)",
    post_move_rebind,
)
mutable_single_guard = body.index(
    "if (!corroboratedKnownCount && values is ICollection<T> && knownCount.HasValue && observedCount >= knownCount.Value)",
    corroborated_guard,
)
cap_guard = body.index("if (observedCount >= maximumCount)", mutable_single_guard)
current_read = body.index("var value = enumerator.Current;", cap_guard)
append = body.index("items.Add(value);", current_read)
final_cardinality = body.index("if (knownCount.HasValue && observedCount != knownCount.Value)", append)
final_rebind = body.index("RequireStableKnownCounts(", final_cardinality)

if not (
    loop < pre_move_rebind < move_next < post_move_rebind <
    corroborated_guard < mutable_single_guard < cap_guard <
    current_read < append < final_cardinality < final_rebind
):
    raise SystemExit(
        "BCF known-Count integrity requires rebound -> MoveNext -> rebound -> "
        "known-count/cap admission -> Current -> append -> final cardinality -> final rebound ordering"
    )

# New mutable single-interface contract: an ICollection<T> Count is authoritative
# enough to prevent exposing IEnumerator.Current beyond the admitted cardinality.
for token in (
    "CurrentTrackingCollection<T> : ICollection<T>",
    "TopicOverrunRejectsBeforeCurrent",
    "ComponentOverrunRejectsBeforeCurrent",
    "CurrentReads == 1",
    "TopicPostTraversalCountDriftRejects",
):
    if token not in smoke:
        raise SystemExit(f"BCF current-read smoke contract missing token: {token}")

# Preserve #4349: a lone IReadOnlyCollection<T> witness does not suppress the
# independent package streaming maximum. Corroborated Count evidence still fails
# before Current, while this read-only single-witness control reaches limit + 1.
for token in (
    "DishonestCountCollection<T> : IReadOnlyCollection<T>",
    "BCF topic streaming bound did not stop on item 257.",
    "BCF comment streaming bound did not stop on item 1025.",
    "BCF component streaming bound did not stop on item 1001.",
):
    if token not in legacy:
        raise SystemExit(f"BCF legacy streaming-bound contract missing token: {token}")

print("PASS BCF bounded collection known-Count Current no-overread/rebind compatibility guard")
