#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.xaml.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"


def require(text, tokens, label):
    missing = [token for token in tokens if token not in text]
    if not missing:
        return []
    return [label + " missing contract token: " + token for token in missing]


def main():
    errors = []
    xaml = XAML.read_text(encoding="utf-8")
    code = CODE.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")

    errors += require(xaml, (
        'x:Name="SummaryModeRadio"',
        'x:Name="DetailModeRadio"',
        'Content="Diễn giải chi tiết"',
        'Checked="OnViewModeChanged"',
        'x:Name="AutoRevealCheck"',
        'Content="Bám 3D"',
        'SelectionChanged="OnQuantityGridSelectionChanged"',
        'x:Name="ExplanationTitleText"',
        'x:Name="ExplanationConcreteText"',
        'x:Name="ExplanationFormworkText"',
        'x:Name="ExplanationGeometryText"',
        'x:Name="ExplanationProvenanceText"',
    ), "QuantitySummaryWindow.xaml")

    errors += require(code, (
        "private bool _detailMode;",
        "private void OnViewModeChanged(object sender, RoutedEventArgs e)",
        "private IReadOnlyList<QuantityReportRow> RecalculateDetailRows()",
        "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(currentProject);",
        "ProjectQuantityReportBuilder.Detail(previewProject)",
        "private void OnQuantityGridSelectionChanged(object sender, SelectionChangedEventArgs e)",
        "if (!_initialized || !_detailMode || AutoRevealCheck?.IsChecked != true || row == null || e.AddedItems.Count == 0) return;",
        "LocateCurrent();",
        "var currentRow = ResolveCurrentRow(row);",
        "_locate(currentRow);",
        "var currentRows = _detailMode ? RecalculateDetailRows() : RecalculateSummaryRows(true);",
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "EnsureCurrentProject(\"định vị BQ\");",
        "SameElementIdentity(displayedIds, x)",
        "if (!SameRow(displayedRow, matches[0]))",
        "ExplanationConcreteText.Text =",
        "ExplanationFormworkText.Text =",
        "ExplanationGeometryText.Text =",
        "ExplanationProvenanceText.Text =",
    ), "QuantitySummaryWindow.xaml.cs")

    errors += require(commands, (
        '[CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]',
        "SourceHandleResolver.Resolve(currentProject, row.ElementIds)",
        "Cad.CadHandleService.Select(doc, handles)",
        'doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
        "new QuantitySummaryWindow(doc, rows, locate, recalculate)",
    ), "Commands.cs")

    if "_locate(row);" in code:
        errors.append("QuantitySummaryWindow.xaml.cs must revalidate a displayed row before invoking the locate callback")
    if "ProjectContextCoordinator.GetOrCreate" in code:
        errors.append("BQ detail/review window must not bootstrap a replacement project")
    if "ExistingProjectMutationContext.Require(_document" in code:
        errors.append("BQ read-only detail/reveal path must not require a mutation bind")

    selection_pos = code.find("private void OnQuantityGridSelectionChanged")
    locate_call_pos = code.find("LocateCurrent();", selection_pos)
    resolve_pos = code.find("var currentRow = ResolveCurrentRow(row);", locate_call_pos)
    callback_pos = code.find("_locate(currentRow);", resolve_pos)
    if min(selection_pos, locate_call_pos, resolve_pos, callback_pos) < 0 or not (selection_pos < locate_call_pos < resolve_pos < callback_pos):
        errors.append("detail row click must route through LocateCurrent -> ResolveCurrentRow -> current-row locate callback")

    detail_pos = code.find("private IReadOnlyList<QuantityReportRow> RecalculateDetailRows()")
    detached_pos = code.find("ProjectStateSnapshot.CreateDetachedCopy(currentProject)", detail_pos)
    detail_builder_pos = code.find("ProjectQuantityReportBuilder.Detail(previewProject)", detached_pos)
    if min(detail_pos, detached_pos, detail_builder_pos) < 0 or not (detail_pos < detached_pos < detail_builder_pos):
        errors.append("detail rows must be built from a detached current-project snapshot")

    if errors:
        print("ERROR: BQ detail/viewport reveal contract is incomplete:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: BQ detail rows use detached quantity reporting and click-through revalidation before native CAD selection/zoom.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
