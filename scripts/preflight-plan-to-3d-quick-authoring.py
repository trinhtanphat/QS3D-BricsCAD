#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs"
DOC = ROOT / "docs/PLAN-TO-3D-WORKFLOW.md"
errors = []

if not SOURCE.is_file():
    errors.append("missing PlanTo3DCommands.cs")
if not DOC.is_file():
    errors.append("missing PLAN-TO-3D-WORKFLOW.md")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    for token in (
        '[CommandMethod("QS3DCONVERT2D", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DCONVERT2D", promptStyle: false)',
        '[CommandMethod("QS3DPLAN2WALLS", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DPLAN2WALLS", promptStyle: false)',
        '[CommandMethod("QS3DCONVERT2DADV", CommandFlags.Modal)]',
        'ConvertPlanWalls("QS3DCONVERT2DADV", promptStyle: true)',
        'private static void ConvertPlanWalls(string operation, bool promptStyle)',
        'var defaultThicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject, "ThicknessM", 0.2d) : 0.2d;',
        'var defaultHeightM = hasDefaultsProject ? FamilyNumber(defaultsProject, "HeightM", 3.0d) : 3.0d;',
        'var defaultBottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject, "BottomOffsetM", 0d) : 0d;',
        'promptStyle\n                    ? PromptPositiveMeters',
        'promptStyle\n                    ? PromptFiniteMeters',
        'RegenerateDirtySubset(project, new[] { element.Id })',
        'RequireSameSources(sources, refreshedSources)',
    ):
        if token not in source:
            errors.append("PlanTo3D quick-authoring contract missing: " + token)

    quick_start = source.find('[CommandMethod("QS3DCONVERT2D", CommandFlags.Modal)]')
    adv_start = source.find('[CommandMethod("QS3DCONVERT2DADV", CommandFlags.Modal)]')
    convert_start = source.find("private static void ConvertPlanWalls", adv_start + 1)
    if min(quick_start, adv_start, convert_start) < 0 or not (quick_start < adv_start < convert_start):
        errors.append("PlanTo3D quick/advanced command split is missing or ordered unexpectedly")

    for token in (
        "QS3DCONVERT2DADV",
        "không mở ba numeric prompt",
        "ThicknessM=0.2 m",
        "HeightM=3.0 m",
        "BottomOffsetM=0 m",
        "RegenerateDirtySubset",
        "quick/no-prompt path",
        "LOCAL-008",
    ):
        if token not in doc:
            errors.append("PlanTo3D docs missing quick-authoring token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: 2D-plan wall conversion defaults to active/preferred Family values without three repeated numeric prompts, preserves an explicit ADV override path, and retains freshness/scoped-regeneration safety.")
