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


placement_start = source.index("private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements")
placement_end = source.index("private static int? ResolvePlacementKnownCount", placement_start)
placement = source[placement_start:placement_end]
require("while (enumerator.MoveNext())" in placement, "placement traversal must remain explicit")
require("if (knownCount.HasValue && result.Count >= knownCount.Value)" in placement,
        "placement known Count must guard traversal")
require("result.Add(enumerator.Current);" in placement, "placement traversal must retain admitted Current")
require(placement.index("if (knownCount.HasValue && result.Count >= knownCount.Value)") < placement.index("result.Add(enumerator.Current);"),
        "placement known Count admission must precede Current")
require("var reboundKnownCount = ResolvePlacementKnownCount(placements);" in placement,
        "placement Count surfaces must be rebound")
require("known Count changed during traversal" in placement,
        "placement Count drift diagnostic missing")

catalog_start = source.index("private static List<SemanticSheetDefinition> MaterializeCatalogBounded")
catalog_end = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded", catalog_start)
catalog = source[catalog_start:catalog_end]
require("if (knownCount.HasValue && result.Count >= knownCount.Value)" in catalog,
        "catalog known Count must guard traversal")
require(catalog.index("if (knownCount.HasValue && result.Count >= knownCount.Value)") < catalog.index("result.Add(enumerator.Current);"),
        "catalog known Count admission must precede Current")
require(catalog.count("RequireKnownCountsWithinLimit(definitions") >= 2,
        "catalog Count surfaces must be rebound after traversal")
require("known count changed during traversal for sheets" in catalog,
        "catalog Count drift diagnostic missing")

views_start = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded")
views_end = source.index("private static int? RequireKnownCountsWithinLimit", views_start)
views = source[views_start:views_end]
require("if (knownCount.HasValue && result.Count >= knownCount.Value)" in views,
        "available-view known Count must guard traversal")
require(views.index("if (knownCount.HasValue && result.Count >= knownCount.Value)") < views.index("result.Add(enumerator.Current);"),
        "available-view known Count admission must precede Current")
require(views.count("RequireKnownCountsWithinLimit(availableViews") >= 2,
        "available-view Count surfaces must be rebound after traversal")
require("known count changed during traversal for available views" in views,
        "available-view Count drift diagnostic missing")

required_smoke = [
    "PlacementOverrunRejectsBeforeCurrent();",
    "PlacementPostTraversalCountDriftRejects();",
    "CatalogOverrunRejectsBeforeCurrent();",
    "CatalogPostTraversalCountDriftRejects();",
    "AvailableViewOverrunRejectsBeforeCurrent();",
    "AvailableViewPostTraversalCountDriftRejects();",
    "source.MoveNextCalls == 2",
    "source.CurrentReads == 1",
    "source.CountReads >= 2",
]
for marker in required_smoke:
    require(marker in smoke, "missing adversarial smoke marker: " + marker)

print("PASS Semantic Sheet known-Count Current no-overread and rebound guard")
