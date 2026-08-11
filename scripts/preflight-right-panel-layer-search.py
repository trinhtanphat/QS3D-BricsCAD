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

print("QS3D RightPanel cached layer-search preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: layer search is bounded multi-term filtering over cached CAD snapshots; keystrokes do not reopen the layer table, while real layer mutations explicitly reload live CAD state.")
