#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
errors = []

for path in (XAML, CODE):
    if not path.is_file():
        errors.append("missing Quantity Insight reveal source: " + str(path.relative_to(ROOT)))

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    required = (
        'x:Name="AutoRevealCheck"',
        'Content="Click = 3D"',
        'IsChecked="True"',
        'SelectedItemChanged="OnQuantityTreeSelectedItemChanged"',
        'MouseDoubleClick="OnQuantityTreeDoubleClick"',
        'Click="OnLocateClick"',
        'mặc định click dòng cấu kiện sẽ định vị và zoom ngược trong View 3D',
    )
    for needle in required:
        if needle not in text:
            errors.append("QuantityInsightPanel.xaml missing click-reveal UI contract: " + needle)

if CODE.is_file():
    text = CODE.read_text(encoding="utf-8")
    required = (
        "private void OnQuantityTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)",
        "if (AutoRevealCheck?.IsChecked != true) return;",
        "if (!(e.NewValue is QuantityInsightItemViewModel)) return;",
        "LocateSelected();",
        "private void OnQuantityTreeDoubleClick(object sender, MouseButtonEventArgs e)",
        "if (AutoRevealCheck?.IsChecked == true) return;",
        "if (QuantityTree.SelectedItem is QuantityInsightItemViewModel) LocateSelected();",
        "if (!(QuantityTree.SelectedItem is QuantityInsightItemViewModel item))",
        "!ReferenceEquals(document, _boundDocument)",
        "if (!SameProjectIdentity(project))",
        "var currentRow = ResolveCurrentRow(item, project);",
        "var currentRows = BuildPreviewRows(project, out _);",
        "SourceHandleResolver.Resolve(project, currentRow.ElementIds)",
        "Cad.CadHandleService.Select(document, handles)",
        'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
    )
    for needle in required:
        if needle not in text:
            errors.append("QuantityInsightPanel code-behind missing single-click/native reveal contract: " + needle)

    selection_handler = text.find("private void OnQuantityTreeSelectedItemChanged")
    toggle_guard = text.find("if (AutoRevealCheck?.IsChecked != true) return;", selection_handler)
    leaf_guard = text.find("if (!(e.NewValue is QuantityInsightItemViewModel)) return;", toggle_guard)
    auto_locate = text.find("LocateSelected();", leaf_guard)
    if min(selection_handler, toggle_guard, leaf_guard, auto_locate) < 0 or not (
        selection_handler < toggle_guard < leaf_guard < auto_locate
    ):
        errors.append("single-click reveal must check opt-in state and leaf type before locating")

    double_handler = text.find("private void OnQuantityTreeDoubleClick")
    duplicate_guard = text.find("if (AutoRevealCheck?.IsChecked == true) return;", double_handler)
    manual_leaf = text.find("if (QuantityTree.SelectedItem is QuantityInsightItemViewModel) LocateSelected();", duplicate_guard)
    if min(double_handler, duplicate_guard, manual_leaf) < 0 or not (
        double_handler < duplicate_guard < manual_leaf
    ):
        errors.append("double-click fallback must be disabled while auto-reveal is active and remain leaf-only otherwise")

    locate = text.find("private void LocateSelected()")
    document_guard = text.find("!ReferenceEquals(document, _boundDocument)", locate)
    project_guard = text.find("if (!SameProjectIdentity(project))", document_guard)
    current_row = text.find("var currentRow = ResolveCurrentRow(item, project);", project_guard)
    handles = text.find("SourceHandleResolver.Resolve(project, currentRow.ElementIds)", current_row)
    select = text.find("Cad.CadHandleService.Select(document, handles)", handles)
    zoom = text.find('document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)', select)
    if min(locate, document_guard, project_guard, current_row, handles, select, zoom) < 0 or not (
        locate < document_guard < project_guard < current_row < handles < select < zoom
    ):
        errors.append("single-click reveal must reuse fail-closed current-row -> Handle -> native select -> zoom ordering")

    forbidden = (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        "SourceHandleResolver.Resolve(project, item.ElementIds)",
    )
    for needle in forbidden:
        if needle in text:
            errors.append("click reveal must stay read-only and must not resolve stale item IDs directly: " + needle)

print("QS3D Quantity Insight single-click reveal preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Quantity leaf selection defaults to fail-closed native CAD reveal, floor/group clicks remain passive, and double-click remains a non-duplicating manual fallback when auto-reveal is disabled.")
