#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticSheetKnownCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL Semantic Sheet known-Count integrity: " + message)


def require_order(body: str, anchors: list[str], label: str) -> None:
    cursor = 0
    for anchor in anchors:
        found = body.find(anchor, cursor)
        require(found >= 0, label + " missing ordered anchor: " + anchor)
        cursor = found + len(anchor)


placement_start = source.index("private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements")
placement_end = source.index("private static void RevalidatePlacementKnownCount", placement_start)
placement = source[placement_start:placement_end]
require("while (true)" in placement, "placement traversal must remain explicit")
require_order(
    placement,
    [
        "var moved = enumerator.MoveNext();",
        "if (!moved)",
        "if (result.Count >= SemanticSheetPlanner.MaxPlacements)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var placement = enumerator.Current;",
        "result.Add(placement);",
    ],
    "placement traversal",
)
require("RevalidatePlacementKnownCount(placements, knownCount.Value);" in placement,
        "placement Count surfaces must be rebound during traversal")
require("known Count does not match the number of placements traversed" in placement,
        "placement exact-cardinality diagnostic missing")

placement_helper_start = source.index("private static void RevalidatePlacementKnownCount", placement_start)
placement_helper_end = source.index("private static int? ResolvePlacementKnownCount", placement_helper_start)
placement_helper = source[placement_helper_start:placement_helper_end]
require("known Count changed during traversal" in placement_helper,
        "placement Count drift diagnostic missing")

catalog_start = source.index("private static List<SemanticSheetDefinition> MaterializeCatalogBounded")
catalog_end = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded", catalog_start)
catalog = source[catalog_start:catalog_end]
require_order(
    catalog,
    [
        "var moved = enumerator.MoveNext();",
        "if (!moved)",
        "if (result.Count >= MaxCatalogSheets)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var definition = enumerator.Current;",
        "result.Add(definition);",
    ],
    "catalog traversal",
)
require(catalog.count("RevalidateKnownCount(definitions") >= 4,
        "catalog Count surfaces must be rebound around traversal and after Current")
require("traversal count does not match its known count for sheets" in catalog,
        "catalog exact-cardinality diagnostic missing")

views_start = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded")
views_end = source.index("private static void RevalidateKnownCount", views_start)
views = source[views_start:views_end]
require_order(
    views,
    [
        "var moved = enumerator.MoveNext();",
        "if (!moved)",
        "if (result.Count >= MaxAvailableViews)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var view = enumerator.Current;",
        "result.Add(view);",
    ],
    "available-view traversal",
)
require(views.count("RevalidateKnownCount(availableViews") >= 4,
        "available-view Count surfaces must be rebound around traversal and after Current")
require("traversal count does not match its known count for available views" in views,
        "available-view exact-cardinality diagnostic missing")

helper_start = source.index("private static void RevalidateKnownCount", views_start)
helper_end = source.index("private static int? RequireKnownCountsWithinLimit", helper_start)
helper = source[helper_start:helper_end]
require("known count changed during traversal for" in helper,
        "shared post-admission Count drift diagnostic missing")

required_smoke = [
    "PlacementOverrunRejectsBeforeCurrent();",
    "PlacementPostTraversalCountDriftRejects();",
    "CatalogOverrunRejectsBeforeCurrent();",
    "CatalogPostTraversalCountDriftRejects();",
    "AvailableViewOverrunRejectsBeforeCurrent();",
    "AvailableViewPostTraversalCountDriftRejects();",
    "source.MoveNextCalls == 2",
    "source.CurrentReads == 1",
    "source.CountReads >= 7",
]
for marker in required_smoke:
    require(marker in smoke, "missing adversarial smoke marker: " + marker)

print("PASS Semantic Sheet known-Count Current no-overread and rebound guard")
