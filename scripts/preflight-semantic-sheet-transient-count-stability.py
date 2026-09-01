#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticSheetTransientCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL Semantic Sheet transient Count stability: " + message)


def require_order(body: str, anchors: list[str], label: str) -> None:
    cursor = 0
    for anchor in anchors:
        found = body.find(anchor, cursor)
        require(found >= 0, label + " missing ordered anchor: " + anchor)
        cursor = found + len(anchor)


placement_start = source.index("private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements")
placement_end = source.index("private static int? ResolvePlacementKnownCount", placement_start)
placement = source[placement_start:placement_end]
require_order(
    placement,
    [
        "RevalidatePlacementKnownCount(placements, knownCount.Value);",
        "var moved = enumerator.MoveNext();",
        "RevalidatePlacementKnownCount(placements, knownCount.Value);",
        "if (!moved)",
        "if (result.Count >= SemanticSheetPlanner.MaxPlacements)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var placement = enumerator.Current;",
        "RevalidatePlacementKnownCount(placements, knownCount.Value);",
        "result.Add(placement);",
    ],
    "placement traversal",
)
require("while (true)" in placement, "placement traversal must expose a pre-MoveNext Count boundary")

catalog_start = source.index("private static List<SemanticSheetDefinition> MaterializeCatalogBounded")
catalog_end = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded", catalog_start)
catalog = source[catalog_start:catalog_end]
require_order(
    catalog,
    [
        "RevalidateKnownCount(definitions, knownCount.Value, MaxCatalogSheets, \"Semantic sheet catalog\", \"sheets\");",
        "var moved = enumerator.MoveNext();",
        "RevalidateKnownCount(definitions, knownCount.Value, MaxCatalogSheets, \"Semantic sheet catalog\", \"sheets\");",
        "if (!moved)",
        "if (result.Count >= MaxCatalogSheets)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var definition = enumerator.Current;",
        "RevalidateKnownCount(definitions, knownCount.Value, MaxCatalogSheets, \"Semantic sheet catalog\", \"sheets\");",
        "result.Add(definition);",
    ],
    "catalog traversal",
)
require("while (true)" in catalog, "catalog traversal must expose a pre-MoveNext Count boundary")

views_start = source.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded")
views_end = source.index("private static int? RequireKnownCountsWithinLimit", views_start)
views = source[views_start:views_end]
require_order(
    views,
    [
        "RevalidateKnownCount(availableViews, knownCount.Value, MaxAvailableViews, \"Semantic sheet planner\", \"available views\");",
        "var moved = enumerator.MoveNext();",
        "RevalidateKnownCount(availableViews, knownCount.Value, MaxAvailableViews, \"Semantic sheet planner\", \"available views\");",
        "if (!moved)",
        "if (result.Count >= MaxAvailableViews)",
        "if (knownCount.HasValue && result.Count >= knownCount.Value)",
        "var view = enumerator.Current;",
        "RevalidateKnownCount(availableViews, knownCount.Value, MaxAvailableViews, \"Semantic sheet planner\", \"available views\");",
        "result.Add(view);",
    ],
    "available-view traversal",
)
require("while (true)" in views, "available-view traversal must expose a pre-MoveNext Count boundary")

required_smoke = [
    "PlacementMoveNextDriftFailsBeforeCurrent();",
    "PlacementCurrentDriftFailsBeforeRetention();",
    "CatalogMoveNextDriftFailsBeforeCurrent();",
    "CatalogCurrentDriftFailsBeforeRetention();",
    "AvailableViewMoveNextDriftFailsBeforeCurrent();",
    "AvailableViewCurrentDriftFailsBeforeRetention();",
    "StableCountedControlsRemainAccepted();",
    "DriftMode.MoveNext",
    "DriftMode.Current",
    "source.CurrentReads == 0",
    "source.MoveNextCalls == 1",
]
for marker in required_smoke:
    require(marker in smoke, "missing deterministic smoke marker: " + marker)

print("PASS Semantic Sheet transient known-Count traversal stability")
