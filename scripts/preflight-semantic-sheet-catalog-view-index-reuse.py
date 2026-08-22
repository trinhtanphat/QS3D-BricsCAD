#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetPlanner.cs"

source = SOURCE.read_text(encoding="utf-8")

required = [
    "var views = MaterializeAvailableViewsBounded(availableViews);",
    "var viewIndex = BuildUniqueViewIndex(views);",
    "return BuildValidated(definition, viewIndex, id, number, name, titleBlockName);",
    "var plan = BuildCore(definition, viewIndex);",
    "private static SemanticSheetPlan BuildCore(",
    "private static SemanticSheetPlan BuildValidated(",
]
for marker in required:
    if marker not in source:
        raise SystemExit(f"missing semantic sheet catalog view-index reuse contract: {marker}")

if "var plan = Build(definition, views);" in source:
    raise SystemExit("semantic sheet catalog still rebuilds the available-view index per sheet")

catalog_start = source.index("public static IReadOnlyList<SemanticSheetPlan> BuildCatalog(")
catalog_end = source.index("private static SemanticSheetPlan BuildCore(", catalog_start)
catalog = source[catalog_start:catalog_end]
if catalog.count("BuildUniqueViewIndex(views)") != 1:
    raise SystemExit("semantic sheet catalog must build its available-view index exactly once")
if catalog.count("MaterializeAvailableViewsBounded(availableViews)") != 1:
    raise SystemExit("semantic sheet catalog must materialize available views exactly once")

core_start = source.index("private static SemanticSheetPlan BuildCore(")
core_end = source.index("private static SemanticSheetPlan BuildValidated(", core_start)
core = source[core_start:core_end]
if "MaterializeAvailableViewsBounded" in core or "BuildUniqueViewIndex" in core:
    raise SystemExit("semantic sheet catalog core path must reuse the prevalidated view index")

print("semantic sheet catalog view-index reuse preflight: PASS")
