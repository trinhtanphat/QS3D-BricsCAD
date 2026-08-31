#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticScheduleCatalog.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleDefinitionBoundedSnapshotSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleDefinitionBoundedSnapshotSmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "internal const int MaxIds = 5000;",
    "internal const int MaxColumns = 32;",
    "Categories = SnapshotBounded(",
    "IncludeElementIds = SnapshotBounded(",
    "ExcludeElementIds = SnapshotBounded(",
    "Columns = SnapshotBounded(",
    "SemanticScheduleCatalog.MaxIds",
    "SemanticScheduleCatalog.MaxColumns",
    "private static IReadOnlyList<T> SnapshotBounded<T>",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "before MoveNext");',
    "var moved = enumerator.MoveNext();",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after MoveNext");',
    "if (!moved) break;",
    "if (result.Count >= maxCount) throw new InvalidOperationException(capacityError);",
    "var current = enumerator.Current;",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after Current");',
    "result.Add(current);",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after traversal");',
    "return result.AsReadOnly();",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing source contract: {marker}")

legacy = [
    "Categories = new List<ElementCategory>(categories ?? Array.Empty<ElementCategory>()).AsReadOnly();",
    "IncludeElementIds = new List<string>(includeElementIds ?? Array.Empty<string>()).AsReadOnly();",
    "ExcludeElementIds = new List<string>(excludeElementIds ?? Array.Empty<string>()).AsReadOnly();",
    "Columns = new List<SemanticDocumentationColumn>(columns ?? throw new ArgumentNullException(nameof(columns))).AsReadOnly();",
]
for marker in legacy:
    if marker in source:
        raise SystemExit(f"legacy unbounded constructor materialization remains: {marker}")

snapshot_start = source.index("private static IReadOnlyList<T> SnapshotBounded<T>")
snapshot_end = source.index("private static void ValidateKnownCountEvidence<T>", snapshot_start)
snapshot = source[snapshot_start:snapshot_end]
if "while (enumerator.MoveNext())" in snapshot:
    raise SystemExit("semantic schedule snapshot must expose Count rebound points around MoveNext")

pre_move = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "before MoveNext");')
move = snapshot.index("var moved = enumerator.MoveNext();", pre_move)
post_move = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after MoveNext");', move)
break_guard = snapshot.index("if (!moved) break;", post_move)
capacity_guard = snapshot.index("if (result.Count >= maxCount) throw new InvalidOperationException(capacityError);", break_guard)
count_guard = snapshot.index("if (knownCount.HasValue && result.Count >= knownCount.Value)", capacity_guard)
current = snapshot.index("var current = enumerator.Current;", count_guard)
post_current = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after Current");', current)
add = snapshot.index("result.Add(current);", post_current)
if not (pre_move < move < post_move < break_guard < capacity_guard < count_guard < current < post_current < add):
    raise SystemExit("semantic schedule Count/capacity/Current ordering is not fail-closed")

required_smoke = [
    "CategoriesAcceptExactLimit();",
    "CategoriesStopAtFirstOverBoundItem();",
    "IncludeIdsStopAtFirstOverBoundItem();",
    "ExcludeIdsStopAtFirstOverBoundItem();",
    "ColumnsStopAtFirstOverBoundItem();",
    "MoveNextCountDriftFailsBeforeCurrent();",
    "CurrentCountDriftFailsBeforeRetention();",
    "StableKnownCountRemainsAccepted();",
    "DriftKnownCountCollection",
    '"after MoveNext"',
    '"after Current"',
    "Equal(0, source.CurrentReads);",
    "for (var i = 0; i <= 5000; i++)",
    "RepeatCategories(5000)",
    "Category source enumerated beyond the first over-bound item.",
    "Semantic schedule category list exceeds 5000 entries.",
    "for (var i = 0; i <= 32; i++)",
    "Include source enumerated beyond the first over-bound id.",
    "Exclude source enumerated beyond the first over-bound id.",
    "Column source enumerated beyond the first over-bound column.",
    "Semantic schedule include list exceeds 5000 ids.",
    "Semantic schedule exclude list exceeds 5000 ids.",
    "Semantic schedule requires 1..32 columns.",
    "AcceptedCollectionsRemainDefensiveSnapshots();",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing smoke contract: {marker}")

if "SemanticScheduleDefinitionBoundedSnapshotSmoke.Run();" not in registration:
    raise SystemExit("bounded snapshot smoke is not registered")

print("semantic schedule definition bounds/count-stability preflight: PASS")