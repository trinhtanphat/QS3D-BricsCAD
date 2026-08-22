#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
SHORTCUTS = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.SearchShortcuts.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private const int MaxLayerSearchTokens = 8;",
        "private IReadOnlyList<LayerSnapshot> _layerSnapshots = Array.Empty<LayerSnapshot>();",
        "private void ReloadLayers()",
        "_layerSnapshots = DrawingCatalogReader.ReadLayers(doc);",
        "private void ApplyLayerFilter()",
        ".Take(MaxLayerSearchTokens)",
        "tokens.All(token => MatchesLayerToken(x, token))",
        "private static bool MatchesLayerToken(LayerSnapshot layer, string token)",
        'AliasContains("hiện visible on", token)',
        'AliasContains("ẩn hidden off", token)',
        'AliasContains("khóa locked lock", token)',
        'AliasContains("mở unlocked unlock", token)',
        "private void OnLayerSearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) ApplyLayerFilter(); }",
        "ReloadLayers();",
    )
    for token in required:
        if token not in text:
            errors.append("RightPanel cached layer-search contract missing: " + token)

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
            errors.append("layer search must filter the cached snapshot list")
        for forbidden in ("DrawingCatalogReader.ReadLayers", "ReloadLayers()", "Refresh()"):
            if forbidden in body:
                errors.append("layer search keystrokes must not reopen the CAD layer table: " + forbidden)

    filter_method = re.search(
        r"private void ApplyLayerFilter\(\)\s*\{(?P<body>.*?)\n        \}",
        text,
        re.DOTALL,
    )
    if not filter_method:
        errors.append("missing ApplyLayerFilter method")
    elif "DrawingCatalogReader.ReadLayers" in filter_method.group("body"):
        errors.append("ApplyLayerFilter must remain presentation-only and use _layerSnapshots")

if not XAML.is_file():
    errors.append("missing RightPanel.xaml")
else:
    text = XAML.read_text(encoding="utf-8")
    if 'PreviewKeyDown="OnRightPanelPreviewKeyDown"' not in text:
        errors.append("RightPanel XAML must own the single PreviewKeyDown route for layer-search shortcuts")

if not SHORTCUTS.is_file():
    errors.append("missing RightPanel.SearchShortcuts.cs")
else:
    text = SHORTCUTS.read_text(encoding="utf-8")
    for token in (
        "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)",
        "modifiers == ModifierKeys.Control && e.Key == Key.F",
        "LayerSearchBox?.Focus();",
        "LayerSearchBox?.SelectAll();",
        "e.Key == Key.Escape",
        "LayerSearchBox.IsKeyboardFocusWithin",
        "LayerSearchBox.Clear();",
        "if (TryHandleRightPanelInteractionShortcut(e, modifiers)) return;",
    ):
        if token not in text:
            errors.append("RightPanel layer-search shortcut contract missing: " + token)
    for forbidden in (
        "DrawingCatalogReader.ReadLayers",
        "LayerVisibilityService",
        "SendStringToExecute",
        "PreviewKeyDown += OnRightPanelPreviewKeyDown",
        "OnInitialized(",
    ):
        if forbidden in text:
            errors.append("RightPanel search shortcuts must remain presentation-only and must not double-register the XAML key handler: " + forbidden)

right_panel_code = "\n".join(
    path.read_text(encoding="utf-8")
    for path in sorted((ROOT / "src/QS3D.BricsCAD.V25/UI").glob("RightPanel*.cs"))
)
handler_signature = "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)"
if right_panel_code.count(handler_signature) != 1:
    errors.append("RightPanel must define exactly one XAML PreviewKeyDown handler across all partial class files")
if right_panel_code.count("e.Key == Key.F5") != 1:
    errors.append("RightPanel must preserve exactly one F5 refresh shortcut implementation")

print("QS3D RightPanel cached layer-search preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: layer search is bounded multi-term filtering over cached CAD snapshots; keystrokes do not reopen the layer table, XAML owns one Ctrl+F/Escape route, and real layer mutations explicitly reload live CAD state.")
