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
    "if (result.Count >= maxCount) throw new InvalidOperationException(capacityError);",
    "result.Add(enumerator.Current);",
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

guard = source.index("if (result.Count >= maxCount) throw new InvalidOperationException(capacityError);")
add = source.index("result.Add(enumerator.Current);", guard)
if guard > add:
    raise SystemExit("bounded snapshot guard must execute before adding the over-bound item")

required_smoke = [
    "CategoriesAcceptExactLimit();",
    "CategoriesStopAtFirstOverBoundItem();",
    "IncludeIdsStopAtFirstOverBoundItem();",
    "ExcludeIdsStopAtFirstOverBoundItem();",
    "ColumnsStopAtFirstOverBoundItem();",
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

print("semantic schedule definition bounds preflight: PASS")
