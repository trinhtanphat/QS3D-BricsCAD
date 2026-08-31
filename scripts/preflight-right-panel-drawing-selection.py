#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    refresh = re.search(
        r"public void Refresh\(\)\s*\{(?P<body>.*?)\n        \}\n\n        private void ReloadLayers",
        text,
        re.DOTALL,
    )
    if not refresh:
        errors.append("missing bounded RightPanel.Refresh")
    else:
        body = refresh.group("body")
        for token in (
            "var doc = Application.DocumentManager.MdiActiveDocument;",
            "if (doc == null)",
            "_refreshingDrawings = true;",
            "_viewModel.Drawings.Clear();",
            "DrawingList?.UnselectAll();",
            "_refreshingLayers = true;",
            "_viewModel.Layers.Clear();",
            "LayerList?.UnselectAll();",
            "ApplyLayerFilter();",
            '"Không có bản vẽ BricsCAD đang active."',
            "_viewModel.Status = RefreshFailureStatus;",
        ):
            if token not in body:
                errors.append("RightPanel no-document/refresh contract missing: " + token)
        stale_return = "var doc = Application.DocumentManager.MdiActiveDocument;\n            if (doc == null) return;"
        if stale_return in body:
            errors.append("RightPanel.Refresh must clear stale drawings/layers instead of returning with prior-document UI")
        if "_layerSnapshots" in body:
            errors.append("RightPanel no-document reset must clear the canonical layer VM collection, not a removed duplicate snapshot cache")
        if "ex.Message" in body:
            errors.append("RightPanel.Refresh must not expose raw host exception detail")

    refresh_drawings = re.search(
        r"private void RefreshDrawingsOnly\(\)\s*\{(?P<body>.*?)\n        \}\n\n        private void RefreshAfterXrefMutation",
        text,
        re.DOTALL,
    )
    if not refresh_drawings:
        errors.append("missing bounded RefreshDrawingsOnly")
    else:
        body = refresh_drawings.group("body")
        for token in (
            "var selectedDrawing = DrawingList?.SelectedItem as DrawingItemViewModel;",
            "var restored = _viewModel.Drawings.FirstOrDefault",
            "DrawingList.SelectedItem = restored;",
            "if (selectedDrawing.IsXref && restored == null)",
            "doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());",
        ):
            if token not in body:
                errors.append("RightPanel drawing refresh selection cleanup missing: " + token)
        restored_at = body.find("DrawingList.SelectedItem = restored;")
        vanished_guard = body.find("if (selectedDrawing.IsXref && restored == null)")
        cad_clear = body.find("doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());", vanished_guard)
        if restored_at < 0 or vanished_guard < restored_at or cad_clear < vanished_guard:
            errors.append("A previously selected Xref that disappears during refresh must clear its stale CAD implied selection")

    refresh_after_mutation = re.search(
        r"private void RefreshAfterXrefMutation\(string successStatus\)\s*\{(?P<body>.*?)\n        \}\n\n        private void OnRefreshClick",
        text,
        re.DOTALL,
    )
    if not refresh_after_mutation:
        errors.append("missing bounded RefreshAfterXrefMutation helper")
    else:
        body = refresh_after_mutation.group("body")
        for token in (
            "RefreshDrawingsOnly();",
            "ReloadLayers();",
            "_viewModel.Status = successStatus;",
            "_viewModel.Status = successStatus + RefreshWarningSuffix;",
        ):
            if token not in body:
                errors.append("Xref post-mutation refresh feedback missing: " + token)
        if "Refresh();" in body:
            errors.append("Xref post-mutation helper must use throwing refresh primitives so refresh failures cannot be silently masked as success")
        if "ex.Message" in body:
            errors.append("Xref post-mutation refresh warning must not expose raw host exception detail")

    clear_click = re.search(
        r"private void OnClearDrawingSelectionClick\(object sender, RoutedEventArgs e\)\s*\{(?P<body>.*?)\n        \}\n\n        private void OnDrawingSelectionChanged",
        text,
        re.DOTALL,
    )
    if not clear_click:
        errors.append("missing bounded OnClearDrawingSelectionClick handler")
    else:
        body = clear_click.group("body")
        for token in (
            "_refreshingDrawings = true;",
            "DrawingList.UnselectAll();",
            "_refreshingDrawings = false;",
            "doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());",
            "_viewModel.Status = ClearSelectionFailureStatus;",
        ):
            if token not in body:
                errors.append("RightPanel explicit clear-selection contract missing: " + token)
        unselect = body.find("DrawingList.UnselectAll();")
        cad_clear = body.find("doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());")
        if unselect < 0 or cad_clear < unselect:
            errors.append("RightPanel clear button must suppress list selection callbacks before one explicit CAD implied-selection clear")
        if "ex.Message" in body:
            errors.append("RightPanel clear-selection failure must not expose raw host exception detail")

    match = re.search(
        r"private void OnDrawingSelectionChanged\(object sender, SelectionChangedEventArgs e\)\s*\{(?P<body>.*?)\n        \}\n\n        private void OnLayerChecked",
        text,
        re.DOTALL,
    )
    if not match:
        errors.append("missing bounded OnDrawingSelectionChanged handler")
    else:
        body = match.group("body")
        for token in (
            "if (_refreshingDrawings) return;",
            "var doc = Application.DocumentManager.MdiActiveDocument;",
            "if (doc == null) return;",
            "var item = DrawingList.SelectedItem as DrawingItemViewModel;",
            "if (item == null || !item.IsXref)",
            "doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());",
            '"Bản vẽ chính " + item.Name + " • đã bỏ chọn Xref trong CAD."',
            "_viewModel.Status = ClearSelectionFailureStatus;",
            "var count = XrefService.SelectInstances(doc, item.Name);",
            "_viewModel.Status = XrefSelectionFailureStatus;",
        ):
            if token not in body:
                errors.append("RightPanel drawing-selection contract missing: " + token)

        clear_branch = body.find("if (item == null || !item.IsXref)")
        clear_selection = body.find("doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());", clear_branch)
        xref_select = body.find("XrefService.SelectInstances(doc, item.Name)")
        if clear_branch < 0 or clear_selection < clear_branch or xref_select < 0 or clear_selection > xref_select:
            errors.append("MODEL/null drawing selection must clear stale CAD implied selection before the Xref selection path")

        stale = "if (doc == null || item == null || !item.IsXref) return;"
        if stale in body:
            errors.append("RightPanel must not leave stale implied Xref selection when MODEL/null is selected")
        if "ex.Message" in body:
            errors.append("RightPanel drawing selection failure must not expose raw host exception detail")

    for method, action, status, failure_status in (
        ("OnReloadXrefClick", "XrefService.Reload(doc, item.Name);", 'RefreshAfterXrefMutation("Đã nạp lại Xref " + item.Name);', "XrefReloadFailureStatus"),
        ("OnDeleteDrawingClick", "XrefService.Detach(doc, item.Name);", 'RefreshAfterXrefMutation("Đã gỡ Xref " + item.Name);', "XrefDetachFailureStatus"),
    ):
        method_match = re.search(
            r"private void " + re.escape(method) + r"\(object sender, RoutedEventArgs e\)\s*\{(?P<body>.*?)\n        \}",
            text,
            re.DOTALL,
        )
        if not method_match:
            errors.append("missing bounded " + method + " handler")
            continue
        body = method_match.group("body")
        action_at = body.find(action)
        feedback_at = body.find(status, action_at)
        if action_at < 0 or feedback_at < action_at:
            errors.append(method + " must mutate first, then use the warning-aware live refresh helper")
        if "Refresh();" in body:
            errors.append(method + " must not use swallow-and-overwrite Refresh for mutation feedback")
        if "_viewModel.Status = " + failure_status + ";" not in body:
            errors.append(method + " must use stable redacted failure status " + failure_status)
        if "ex.Message" in body:
            errors.append(method + " must not expose raw host exception detail")

print("QS3D RightPanel drawing-selection preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RightPanel clears canonical drawing/layer UI state when no document exists, removes vanished-Xref implied selection, maps drawing selection cleanly to CAD state, distinguishes Xref mutation success from refresh warnings, and redacts host failure details.")