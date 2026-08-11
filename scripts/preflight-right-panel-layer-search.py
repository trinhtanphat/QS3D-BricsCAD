#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
VIEWMODEL = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs"
SHORTCUTS = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.SearchShortcuts.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "using System.Windows.Data;",
        "private const int MaxLayerSearchTokens = 8;",
        "private void ReloadLayers()",
        "foreach (var item in DrawingCatalogReader.ReadLayers(doc))",
        "var brush = new SolidColorBrush(Color.FromRgb(item.Red, item.Green, item.Blue));",
        "brush.Freeze();",
        "private void ApplyLayerFilter()",
        "CollectionViewSource.GetDefaultView(_viewModel.Layers)",
        ".Take(MaxLayerSearchTokens)",
        "new Predicate<object>(item => item is LayerItemViewModel layer && tokens.All(token => MatchesLayerToken(layer, token)))",
        "_viewModel.SetLayerCounts(view.Cast<object>().Count(), _viewModel.Layers.Count);",
        "private void RestoreLayerSelection(IEnumerable<string> names)",
        "foreach (var item in LayerList.Items.Cast<LayerItemViewModel>())",
        "private static bool MatchesLayerToken(LayerItemViewModel layer, string token)",
        'AliasContains("hiện visible on", token)',
        'AliasContains("ẩn hidden off", token)',
        'AliasContains("khóa locked lock", token)',
        'AliasContains("mở unlocked unlock", token)',
        "var visible = LayerList.Items.Cast<LayerItemViewModel>().ToList();",
        "private void OnLayerSearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) ApplyLayerFilter(); }",
        "ReloadLayers();",
    )
    for token in required:
        if token not in text:
            errors.append("RightPanel view-filter layer-search contract missing: " + token)

    if "_layerSnapshots" in text:
        errors.append("RightPanel must not retain a duplicate layer snapshot cache after moving filtering onto the collection view")

    search_handler = re.search(
        r"private void OnLayerSearchChanged\(object sender, TextChangedEventArgs e\)\s*\{(?P<body>.*?)\}",
        text,
        re.DOTALL,
    )
    if not search_handler:
        errors.append("missing RightPanel layer search handler")
    else:
        body = search_handler.group("body")
        if "ApplyLayerFilter()" not in body:
            errors.append("layer search must filter the existing WPF collection view")
        for forbidden in ("DrawingCatalogReader.ReadLayers", "ReloadLayers()", "Refresh()", "SolidColorBrush", "_viewModel.Layers.Clear"):
            if forbidden in body:
                errors.append("layer-search keystrokes must not reload CAD or rebuild layer rows: " + forbidden)

    filter_method = re.search(
        r"private void ApplyLayerFilter\(\)\s*\{(?P<body>.*?)\n        \}\n\n        private void RestoreLayerSelection",
        text,
        re.DOTALL,
    )
    if not filter_method:
        errors.append("missing bounded ApplyLayerFilter method")
    else:
        body = filter_method.group("body")
        for forbidden in ("DrawingCatalogReader.ReadLayers", "_viewModel.Layers.Clear", "new SolidColorBrush", "LayerVisibilityService"):
            if forbidden in body:
                errors.append("ApplyLayerFilter must remain allocation-light/presentation-only: " + forbidden)
        for token in ("view.Filter =", "view.Refresh();", "_viewModel.SetLayerCounts"):
            if token not in body:
                errors.append("ApplyLayerFilter must operate through the WPF collection view and update result counts: " + token)

    reload_method = re.search(
        r"private void ReloadLayers\(\)\s*\{(?P<body>.*?)\n        \}\n\n        private void ApplyLayerFilter",
        text,
        re.DOTALL,
    )
    if not reload_method:
        errors.append("missing bounded ReloadLayers method")
    else:
        body = reload_method.group("body")
        if "DrawingCatalogReader.ReadLayers(doc)" not in body:
            errors.append("real layer refresh must still re-read current CAD state")
        if "RestoreLayerSelection(selectedNames);" not in body:
            errors.append("real layer refresh must restore visible user selection by stable layer name")

if not VIEWMODEL.is_file():
    errors.append("missing RightPanelViewModel.cs")
else:
    text = VIEWMODEL.read_text(encoding="utf-8")
    for token in (
        "private int _visibleLayerCount;",
        "private int _totalLayerCount;",
        "public string LayerCountText => _visibleLayerCount == _totalLayerCount",
        '" lớp"',
        "public void SetLayerCounts(int visible, int total)",
        "visible = Math.Max(0, Math.Min(visible, total));",
        "OnChanged(nameof(LayerCountText));",
    ):
        if token not in text:
            errors.append("RightPanel filtered/total layer count VM contract missing: " + token)

if not XAML.is_file():
    errors.append("missing RightPanel.xaml")
else:
    text = XAML.read_text(encoding="utf-8")
    for token in (
        'PreviewKeyDown="OnRightPanelPreviewKeyDown"',
        'Text="{Binding LayerCountText}"',
        'ToolTip="Số lớp đang hiển thị / tổng số lớp"',
    ):
        if token not in text:
            errors.append("RightPanel layer count/keyboard XAML contract missing: " + token)
    if 'Text="{Binding Layers.Count, StringFormat={}{0} lớp}"' in text:
        errors.append("RightPanel layer badge must show filtered/total count rather than only the backing collection count")

if not SHORTCUTS.is_file():
    errors.append("missing RightPanel.SearchShortcuts.cs")
else:
    text = SHORTCUTS.read_text(encoding="utf-8")
    for token in (
        "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)",
        "modifiers == ModifierKeys.Control && e.Key == Key.F",
        "LayerSearchBox?.Focus();",
        "LayerSearchBox?.SelectAll();",
        "modifiers == ModifierKeys.None && e.Key == Key.F5",
        "Refresh();",
        "e.Key == Key.Escape",
        "LayerSearchBox.IsKeyboardFocusWithin",
        "LayerSearchBox.Clear();",
    ):
        if token not in text:
            errors.append("RightPanel keyboard shortcut contract missing: " + token)
    f5_guard = text.find("modifiers == ModifierKeys.None && e.Key == Key.F5")
    f5_refresh = text.find("Refresh();", f5_guard)
    escape_guard = text.find("e.Key == Key.Escape")
    if f5_guard < 0 or f5_refresh < f5_guard or (escape_guard >= 0 and f5_refresh > escape_guard):
        errors.append("RightPanel F5 must route directly to Refresh before the Escape search-clear branch")
    for forbidden in (
        "DrawingCatalogReader.ReadLayers",
        "LayerVisibilityService",
        "SendStringToExecute",
        "PreviewKeyDown += OnRightPanelPreviewKeyDown",
        "OnInitialized(",
    ):
        if forbidden in text:
            errors.append("RightPanel keyboard shortcuts must not duplicate CAD internals or double-register the XAML key handler: " + forbidden)

right_panel_code = "\n".join(
    path.read_text(encoding="utf-8")
    for path in sorted((ROOT / "src/QS3D.BricsCAD.V25/UI").glob("RightPanel*.cs"))
)
handler_signature = "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)"
if right_panel_code.count(handler_signature) != 1:
    errors.append("RightPanel must define exactly one XAML PreviewKeyDown handler across all partial class files")
if right_panel_code.count("e.Key == Key.F5") != 1:
    errors.append("RightPanel must preserve exactly one F5 refresh shortcut implementation")

print("QS3D RightPanel layer-search preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: layer rows/brushes rebuild only on real CAD refresh, search keystrokes filter the existing WPF view without row churn, the badge shows filtered/total results, visible selection restores by stable layer name, and Ctrl+F/F5/Escape use one XAML key route.")
