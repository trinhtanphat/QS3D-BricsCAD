#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/CurtainWallSchedule.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallScheduleSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing curtain clear-panel envelope file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "RequireClearPanelEnvelope(",
        'minimumWidthM = Q(element, "CurtainMinClearPanelWidthM");',
        'maximumWidthM = Q(element, "CurtainMaxClearPanelWidthM");',
        'minimumHeightM = Q(element, "CurtainMinClearPanelHeightM");',
        'maximumHeightM = Q(element, "CurtainMaxClearPanelHeightM");',
        "if (minimumWidthM > maximumWidthM)",
        "if (minimumHeightM > maximumHeightM)",
        '"/CurtainClearPanelWidthM minimum cannot exceed maximum."',
        '"/CurtainClearPanelHeightM minimum cannot exceed maximum."',
        "Math.Min(row.MinimumClearPanelWidthM, minimumClearPanelWidthM)",
        "Math.Max(row.MaximumClearPanelWidthM, maximumClearPanelWidthM)",
        "Math.Min(row.MinimumClearPanelHeightM, minimumClearPanelHeightM)",
        "Math.Max(row.MaximumClearPanelHeightM, maximumClearPanelHeightM)",
    ):
        if token not in source:
            errors.append("CurtainWallSchedule source missing envelope integrity token: " + token)

    loop = source.find("foreach (var element in project.Elements")
    validate = source.find("RequireClearPanelEnvelope(", loop)
    row_create = source.find("if (!rows.TryGetValue(key, out var row))", validate)
    wall_count = source.find("row.WallCount = checked", row_create)
    aggregate_width = source.find("row.MinimumClearPanelWidthM = Math.Min", wall_count)
    if min(loop, validate, row_create, wall_count, aggregate_width) < 0 or not (
        loop < validate < row_create < wall_count < aggregate_width
    ):
        errors.append(
            "CurtainWallSchedule must validate each element clear-panel envelope before grouped row creation/mutation and aggregation."
        )

    helper = source.find("private static void RequireClearPanelEnvelope(")
    min_width = source.find('minimumWidthM = Q(element, "CurtainMinClearPanelWidthM");', helper)
    max_width = source.find('maximumWidthM = Q(element, "CurtainMaxClearPanelWidthM");', min_width)
    min_height = source.find('minimumHeightM = Q(element, "CurtainMinClearPanelHeightM");', max_width)
    max_height = source.find('maximumHeightM = Q(element, "CurtainMaxClearPanelHeightM");', min_height)
    width_check = source.find("if (minimumWidthM > maximumWidthM)", max_height)
    height_check = source.find("if (minimumHeightM > maximumHeightM)", width_check)
    group_key = source.find("private static string GroupKey", height_check)
    if min(helper, min_width, max_width, min_height, max_height, width_check, height_check, group_key) < 0 or not (
        helper < min_width < max_width < min_height < max_height < width_check < height_check < group_key
    ):
        errors.append("Clear-panel envelope helper must read all four validated Q values once before width/height semantic checks.")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsInvertedClearPanelWidth();",
        "RejectsInvertedClearPanelHeight();",
        'Contains("g-width/CurtainClearPanelWidthM minimum cannot exceed maximum", error.Message);',
        'Contains("g-height/CurtainClearPanelHeightM minimum cannot exceed maximum", error.Message);',
        "Near(1.3d, row.MinimumClearPanelWidthM);",
        "Near(1.45d, row.MaximumClearPanelWidthM);",
        "Near(1.35d, row.MinimumClearPanelHeightM);",
        "Near(1.45d, row.MaximumClearPanelHeightM);",
    ):
        if token not in smoke:
            errors.append("CurtainWallSchedule smoke missing envelope regression/control: " + token)

print("QS3D curtain wall clear-panel envelope preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain schedule rejects inverted per-element clear-panel width/height envelopes before grouped row mutation.")
