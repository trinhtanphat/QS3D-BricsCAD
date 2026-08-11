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
        errors.append("RightPanel XAML must own the single PreviewKeyDown route for keyboard shortcuts")

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

print("QS3D RightPanel cached layer-search preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: layer search is bounded multi-term filtering over cached CAD snapshots; Ctrl+F/Escape stay presentation-only, F5 reuses the canonical panel Refresh path, XAML owns one key route, and real layer mutations explicitly reload live CAD state.")
