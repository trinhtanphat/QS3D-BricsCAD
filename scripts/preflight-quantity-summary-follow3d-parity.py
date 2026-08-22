#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
GUARD = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml"

errors = []


def read(path: Path) -> str:
    if not path.exists():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def between(text: str, start: str, end: str, label: str) -> str:
    start_pos = text.find(start)
    end_pos = text.find(end, start_pos + len(start)) if start_pos >= 0 else -1
    if start_pos < 0 or end_pos <= start_pos:
        errors.append("cannot isolate " + label)
        return ""
    return text[start_pos:end_pos]


source = read(SOURCE)
guard = read(GUARD)
xaml = read(XAML)

mode = between(
    source,
    "private void UpdateModePresentation()",
    "private void OnColumnVisibilityChanged",
    "UpdateModePresentation",
)
selection = between(
    source,
    "private void OnQuantityGridSelectionChanged",
    "private void UpdateExplanation",
    "OnQuantityGridSelectionChanged",
)
double_click = between(
    source,
    "private void OnQuantityGridDoubleClick",
    "private void OnEd2ExportClick",
    "OnQuantityGridDoubleClick",
)
resolve = between(
    source,
    "private QuantityReportRow ResolveCurrentRow",
    "private QuantityReportRow ResolveSourceHandleRow",
    "ResolveCurrentRow",
)
locate = between(
    source,
    "private void LocateCurrent()",
    "private QuantityReportRow ResolveCurrentRow",
    "LocateCurrent",
)

guard_selection = between(
    guard,
    "private static void OnSummaryLocateSelectionChangedClass",
    "private static void OnSummaryLocateDoubleClickClass",
    "Follow3D locate failure selection guard",
)
guard_double_click = between(
    guard,
    "private static void OnSummaryLocateDoubleClickClass",
    "private void TryClearLocateSelectionForCurrentDocument",
    "Follow3D locate failure double-click guard",
)

if mode:
    if "AutoRevealCheck.IsEnabled = true;" not in mode:
        errors.append("Bám 3D must remain enabled in both summary and detail modes")
    if "AutoRevealCheck.IsEnabled = _detailMode" in mode:
        errors.append("Bám 3D must not regress to detail-only enabled state")

if selection:
    for token in ("AutoRevealCheck?.IsChecked != true", "e.AddedItems.Count == 0", "LocateCurrent();"):
        if token not in selection:
            errors.append("selection auto-follow missing token: " + token)
    if "!_detailMode" in selection or "_detailMode &&" in selection:
        errors.append("selection auto-follow must not be restricted to detail mode")

if double_click:
    if "if (AutoRevealCheck?.IsChecked == true) return;" not in double_click:
        errors.append("double-click must avoid duplicate locate whenever Follow3D is already enabled")
    if "_detailMode && AutoRevealCheck" in double_click:
        errors.append("double-click suppression must not be detail-only")

if guard_selection:
    for token in ("AutoRevealCheck?.IsChecked != true", "e.AddedItems.Count == 0", "TryClearLocateSelectionForCurrentDocument();"):
        if token not in guard_selection:
            errors.append("selection failure guard missing token: " + token)
    if "!owner._detailMode" in guard_selection:
        errors.append("selection failure guard must clear stale CAD selection for summary Follow3D too")

if guard_double_click:
    if "if (owner.AutoRevealCheck?.IsChecked == true) return;" not in guard_double_click:
        errors.append("double-click failure guard must mirror Follow3D duplicate-locate suppression")
    if "owner._detailMode && owner.AutoRevealCheck" in guard_double_click:
        errors.append("double-click failure guard must not be detail-only")

if resolve and "_detailMode ? RecalculateDetailRows() : RecalculateSummaryRows(true);" not in resolve:
    errors.append("safe locate must keep mode-specific current-row revalidation")

for token in (
    "var currentRow = ResolveCurrentRow(row);",
    "var liveHandles = CanonicalIds(currentRow.SourceHandles);",
    "global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(_document)",
):
    if locate and token not in locate:
        errors.append("safe locate path missing token: " + token)

if locate and 'SendStringToExecute("QS3DZOOMSELECTED ' in locate:
    errors.append("safe locate must zoom the exact bound document without queued command re-entry")

if xaml:
    if 'x:Name="AutoRevealCheck"' not in xaml or 'Content="Bám 3D"' not in xaml:
        errors.append("Quantity Summary must expose the Bám 3D checkbox")
    if "Trong chế độ Diễn giải chi tiết, click một dòng" in xaml:
        errors.append("Bám 3D tooltip must not claim detail-only behavior")
    if "click dòng tổng hợp" not in xaml or "reveal cả nhóm" not in xaml:
        errors.append("Quantity Summary guidance must explain summary-group Follow3D behavior")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Quantity Summary Follow3D stays mode-independent, clears stale selection before auto-locate, preserves safe row/handle revalidation, and keeps double-click as the Follow3D-off fallback.")
