#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticViewPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewDefinitionBoundedSnapshotSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewDefinitionBoundedSnapshotSmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "internal const int MaxFilterIds = 100000;",
    "internal static class SemanticViewEnumerableContract",
    "SnapshotBounded<T>",
    "TryGetKnownCount(values, countChangedMessage, out var knownCount)",
    "RequireStableKnownCount(values, knownCount, countChangedMessage);",
    "var moved = enumerator.MoveNext();",
    "var item = enumerator.Current;",
    "result.Add(item);",
    "observedCount != knownCount",
    "values is ICollection<T>",
    "values is IReadOnlyCollection<T>",
    "values is ICollection nonGenericCollection",
    "Semantic view category source Count changed during snapshot.",
    "Semantic view catalog source Count changed during snapshot.",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing source contract: {marker}")

current = source.index("var item = enumerator.Current;")
post_current = source.index("RequireStableKnownCount(values, knownCount, countChangedMessage);", current)
retain = source.index("result.Add(item);", current)
if not current < post_current < retain:
    raise SystemExit("Semantic View counted snapshot must rebind Count after Current before retaining the item")

move = source.index("var moved = enumerator.MoveNext();")
post_move = source.index("RequireStableKnownCount(values, knownCount, countChangedMessage);", move)
if not move < post_move < current:
    raise SystemExit("Semantic View counted snapshot must rebind Count after MoveNext before Current")

legacy = [
    "while (enumerator.MoveNext())",
    "result.Add(enumerator.Current);",
]
for marker in legacy:
    if marker in source:
        raise SystemExit(f"legacy traversal without explicit Count rebound remains: {marker}")

required_smoke = [
    "CategoriesStopAtFirstOverBoundItem();",
    "IncludeIdsStopAtFirstOverBoundItem();",
    "ExcludeIdsStopAtFirstOverBoundItem();",
    "CurrentInducedKnownCountDriftFailsBeforeRetention();",
    "CurrentCountDriftingCollection<string>",
    "Semantic view includeElementIds source Count changed during snapshot.",
    "Equal(1, source.CurrentReads);",
    "AcceptedCollectionsRemainDefensiveSnapshots();",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing smoke contract: {marker}")

if "SemanticViewDefinitionBoundedSnapshotSmoke.Run();" not in registration:
    raise SystemExit("Semantic View bounded snapshot smoke is not registered")

print("semantic view definition bounds preflight: PASS")
