#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

math_path = ROOT / "src/QS3D.Core/Reporting/QuantityReportMath.cs"
if not math_path.is_file():
    errors.append("missing shared schedule arithmetic helper: src/QS3D.Core/Reporting/QuantityReportMath.cs")
else:
    text = math_path.read_text(encoding="utf-8")
    for needle in ("public static class QuantityReportMath", "public static double Add(", "public static int AddCount(", "Quantity report total overflow"):
        if needle not in text: errors.append("shared schedule arithmetic guard missing: " + needle)

bq = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
if not bq.is_file():
    errors.append("missing BQ builder")
else:
    text = bq.read_text(encoding="utf-8")
    for needle in (
        "QFirst(ProjectElement",
        "QFirstOrFallback",
        'QFirst(element, "GrossConcreteM3", "GrossVolumeM3")',
        'QFirst(element, "NetFinishAreaM2", "SideAreaM2")',
    ):
        if needle not in text: errors.append("BQ lazy/preferred-quantity guard missing: " + needle)
    for forbidden in (
        'Q(element, "GrossConcreteM3", Q(',
        'Q(element, "NetConcreteM3", Q(',
        'QFirst(element, "SideAreaM2", "NetFinishAreaM2")',
    ):
        if forbidden in text: errors.append("BQ still eagerly evaluates or prefers a legacy fallback: " + forbidden)

quantity_smoke = ROOT / "tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs"
if not quantity_smoke.is_file():
    errors.append("missing ProjectQuantity smoke")
else:
    text = quantity_smoke.read_text(encoding="utf-8")
    for needle in ("WallFinishPrefersRegeneratedNetArea", 'finish.Quantities["SideAreaM2"] = 99d', 'finish.Quantities["NetFinishAreaM2"] = 12.5d'):
        if needle not in text: errors.append("WallFinish BQ precedence smoke missing: " + needle)

safe_summary_files = [
    "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs",
]
for relative in safe_summary_files:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing schedule summary file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    if "QuantityReportMath" not in text:
        errors.append(relative + " must use shared checked schedule arithmetic")
    if ".Sum(" in text:
        errors.append(relative + " must not use unchecked LINQ Sum for user-visible/export schedule totals")

room_finish = ROOT / "src/QS3D.Core/Reporting/RoomFinishSchedule.cs"
if room_finish.is_file():
    text = room_finish.read_text(encoding="utf-8")
    for needle in ("AutoRoomLifecycle.ResolveRoomReferenceId(project, element)", "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)"):
        if needle not in text: errors.append("room-finish provenance/exclusion guard missing: " + needle)

bq_window = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
if bq_window.is_file():
    text = bq_window.read_text(encoding="utf-8")
    for needle in (
        "QuantityReportTotals.FromRows",
        'EnsureCurrentProject("xuất BQ XLSX")',
        "RefreshRowsForCurrentMode(false);",
        "_rows = RecalculateRowsForCurrentMode(requireLiveSummarySource);",
        "var currentRows = _recalculate() ?? Array.Empty<QuantityReportRow>();",
    ):
        if needle not in text: errors.append("BQ review/export consistency guard missing: " + needle)

print("QS3D schedule arithmetic/provenance preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: BQ fallbacks are lazy; WallFinish BQ prefers regenerated net finish area; BQ export verifies its bound current project before refresh; schedule/Curtain/BBS UI-export totals use shared checked finite arithmetic with room provenance exclusion.")
