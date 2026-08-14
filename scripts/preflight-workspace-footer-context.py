#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FOOTER = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs"
errors = []

if not FOOTER.is_file():
    errors.append("missing WorkspacePanel.FooterContext.cs")
else:
    text = FOOTER.read_text(encoding="utf-8")
    for token in (
        "protected override void OnInitialized(EventArgs e)",
        "Loaded += OnFooterContextLoaded;",
        'string.Equals(text.Text, "LIVE SEMANTIC", StringComparison.Ordinal)',
        "footer.Children.Add(context);",
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "project.FindZone(project.ActiveZoneId)?.Name",
        "var floor = project.FindFloor(project.ActiveFloorId);",
        "floorName = NormalizeFooterName(floor?.Name);",
        "FormatFooterElevation(floor.ElevationM)",
        'elevationM.ToString("0.000", CultureInfo.InvariantCulture) + " m"',
        "ZoneCombo.SelectionChanged",
        "FloorCombo.SelectionChanged",
        "DataContextChanged",
        "IsVisibleChanged",
        '"PROJECT  " + projectName',
        '"   •   ZONE  " + zoneName',
        '"   •   FLOOR  " + floorName',
        '"   •   CAO ĐỘ  " + floorElevation',
    ):
        if token not in text:
            errors.append("Workspace footer missing live-context token: " + token)

    for forbidden in (
        "ProjectZoneService.SetActiveZone",
        "ProjectFloorService.SetActiveFloor",
        "ProjectFloorService.Update",
        ".Touch(",
        "ChangeVersion",
        ".ActiveZoneId =",
        ".ActiveFloorId =",
        ".ElevationM =",
    ):
        if forbidden in text:
            errors.append("Workspace footer must remain read-only; found: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Workspace footer resolves live Project / Zone / Floor / Floor.ElevationM from ExistingProjectMutationContext, formats elevation as 0.000 m, and remains presentation-only.")
