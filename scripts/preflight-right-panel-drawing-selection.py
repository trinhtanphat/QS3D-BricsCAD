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
            '"Không thể bỏ chọn Xref trong CAD: " + ex.Message',
            "var count = XrefService.SelectInstances(doc, item.Name);",
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

print("QS3D RightPanel drawing-selection preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: selecting an Xref maps to its live CAD instances, while selecting MODEL or clearing the row removes stale implied Xref selection instead of leaving old CAD state active.")
