#!/usr/bin/env python3
from pathlib import Path

SOURCE = Path("src/QS3D.Core/Documentation/SemanticSheetPlanner.cs")
text = SOURCE.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("semantic-sheet-planner preflight failed: " + message)


require("private const int MaxAvailableViews = 10000;" in text, "missing 10,000 available-view ceiling")
require("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded(" in text, "missing bounded available-view materializer")
require(text.count("var views = MaterializeAvailableViewsBounded(availableViews);") >= 2, "Build and BuildCatalog must both materialize availableViews through the bounded helper")
require("availableViews.ToArray()" not in text, "unbounded availableViews.ToArray() must not return")
require("Semantic sheet planner supports at most \" + MaxAvailableViews + \" available views." in text, "missing explicit overflow failure")

helper_start = text.index("private static List<SemanticViewPlan> MaterializeAvailableViewsBounded(")
index_start = text.index("private static Dictionary<string, SemanticViewPlan> BuildUniqueViewIndex(", helper_start)
helper = text[helper_start:index_start]
check_pos = helper.find("if (result.Count >= MaxAvailableViews)")
add_pos = helper.find("result.Add(enumerator.Current);")
require(check_pos >= 0, "bounded helper must test the ceiling")
require(add_pos >= 0, "bounded helper must materialize accepted views")
require(check_pos < add_pos, "overflow must fail before buffering the 10,001st view")
require("while (enumerator.MoveNext())" in helper, "bounded helper must enumerate incrementally")
require("throw new InvalidOperationException(" in helper, "overflow must fail closed")

build_start = text.index("public static SemanticSheetPlan Build(")
build_catalog_start = text.index("public static IReadOnlyList<SemanticSheetPlan> BuildCatalog(")
build = text[build_start:build_catalog_start]
require("var views = MaterializeAvailableViewsBounded(availableViews);" in build, "Build bypasses bounded available-view materialization")
require("var viewIndex = BuildUniqueViewIndex(views);" in build, "Build must index only the bounded materialization")

catalog_end = text.index("private static List<SemanticSheetDefinition> MaterializeCatalogBounded(", build_catalog_start)
catalog = text[build_catalog_start:catalog_end]
require("var views = MaterializeAvailableViewsBounded(availableViews);" in catalog, "BuildCatalog bypasses bounded available-view materialization")
require("BuildUniqueViewIndex(views);" in catalog, "BuildCatalog must validate bounded available views before sheet planning")

validated_start = text.index("private static SemanticSheetPlan BuildValidated(")
materialize_start = text.index("private static List<SemanticSheetDefinition> MaterializeCatalogBounded(", validated_start)
validated = text[validated_start:materialize_start]
require("viewIndex.TryGetValue(viewId, out var view)" in validated, "sheet placements must resolve the referenced view plan")
require("view.Kind == SemanticViewKind.Schedule" in validated, "sheet placements must reject Schedule-kind views")
require(
    "Semantic sheet cannot place schedule view id as a sheet view: " in validated,
    "missing explicit Schedule-kind sheet-placement diagnostic",
)

print("semantic-sheet-planner preflight passed")
