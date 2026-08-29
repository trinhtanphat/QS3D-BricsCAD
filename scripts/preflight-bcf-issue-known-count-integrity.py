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
    "while (enumerator.MoveNext())",
    "if (corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (!corroboratedKnownCount && values is ICollection<T> && knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= maximumCount)",
    "var value = enumerator.Current;",
    "items.Add(value);",
    "if (knownCount.HasValue && observedCount != knownCount.Value)",
    "var currentKnownCount = ValidateKnownCounts(",
]
for token in required:
    if token not in body:
        raise SystemExit(f"BCF known-Count integrity guard missing required token: {token}")

if "foreach (var value in values)" in body:
    raise SystemExit("BCF known-Count integrity guard found unsafe caller-controlled foreach traversal")

move_next = body.index("while (enumerator.MoveNext())")
corroborated_guard = body.index(
    "if (corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value)"
)
mutable_single_guard = body.index(
    "if (!corroboratedKnownCount && values is ICollection<T> && knownCount.HasValue && observedCount >= knownCount.Value)"
)
cap_guard = body.index("if (observedCount >= maximumCount)")
current_read = body.index("var value = enumerator.Current;")
append = body.index("items.Add(value);")
rebind = body.index("var currentKnownCount = ValidateKnownCounts(")

if not (
    move_next < corroborated_guard < mutable_single_guard < cap_guard <
    current_read < append < rebind
):
    raise SystemExit(
        "BCF known-Count integrity requires MoveNext -> corroborated/mutable admission -> cap -> Current -> append -> rebind ordering"
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
