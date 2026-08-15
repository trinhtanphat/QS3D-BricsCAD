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
    "Categories = SnapshotCategories(categories);",
    'IncludeElementIds = SnapshotFilterIds(includeElementIds, "includeElementIds");',
    'ExcludeElementIds = SnapshotFilterIds(excludeElementIds, "excludeElementIds");',
    "private static IReadOnlyList<ElementCategory> SnapshotCategories",
    "private static IReadOnlyList<string> SnapshotFilterIds",
    "SemanticViewPlanner.MaxFilterIds",
    "if (result.Count >= SemanticViewPlanner.MaxFilterIds)",
    "result.Add(enumerator.Current);",
    "return result.AsReadOnly();",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing source contract: {marker}")

legacy = [
    "Categories = categories == null ? new List<ElementCategory>().AsReadOnly() : new List<ElementCategory>(categories).AsReadOnly();",
    "IncludeElementIds = includeElementIds == null ? new List<string>().AsReadOnly() : new List<string>(includeElementIds).AsReadOnly();",
    "ExcludeElementIds = excludeElementIds == null ? new List<string>().AsReadOnly() : new List<string>(excludeElementIds).AsReadOnly();",
]
for marker in legacy:
    if marker in source:
        raise SystemExit(f"legacy unbounded constructor materialization remains: {marker}")

category_helper_start = source.index("private static IReadOnlyList<ElementCategory> SnapshotCategories")
category_guard = source.index("if (result.Count >= SemanticViewPlanner.MaxFilterIds)", category_helper_start)
category_add = source.index("result.Add(enumerator.Current);", category_helper_start)
if category_guard >= category_add:
    raise SystemExit("Semantic View category snapshot capacity guard must execute before adding the over-bound item")

helper_start = source.index("private static IReadOnlyList<string> SnapshotFilterIds")
guard = source.index("if (result.Count >= SemanticViewPlanner.MaxFilterIds)", helper_start)
add = source.index("result.Add(enumerator.Current);", helper_start)
if guard >= add:
    raise SystemExit("Semantic View snapshot capacity guard must execute before adding the over-bound item")

required_smoke = [
    "CategoriesStopAtFirstOverBoundItem();",
    "IncludeIdsStopAtFirstOverBoundItem();",
    "ExcludeIdsStopAtFirstOverBoundItem();",
    "for (var i = 0; i <= 100000; i++) yield return \"E\";",
    "Include source enumerated beyond the first over-bound id.",
    "Exclude source enumerated beyond the first over-bound id.",
    "Category source enumerated beyond the first over-bound item.",
    "Semantic view supports at most 100000 categories.",
    "Semantic view supports at most 100000 includeElementIds.",
    "Semantic view supports at most 100000 excludeElementIds.",
    "AcceptedCollectionsRemainDefensiveSnapshots();",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing smoke contract: {marker}")

if "SemanticViewDefinitionBoundedSnapshotSmoke.Run();" not in registration:
    raise SystemExit("Semantic View bounded snapshot smoke is not registered")

print("semantic view definition bounds preflight: PASS")
