#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MODEL_REVIEW = ROOT / "src/QS3D.BricsCAD.V25/ModelReviewCommands.cs"
VIEWPORT = ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs"
errors = []


def read(path, label):
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


model = read(MODEL_REVIEW, "ModelReviewCommands.cs")
viewport = read(VIEWPORT, "ViewportCommands.cs")

if model:
    required = (
        '[CommandMethod("QS3DFOCUS", CommandFlags.UsePickSet)]',
        "var count = ModelReviewService.HighlightSelection(document, true);",
        "if (!ViewportCommands.TryZoomSelection(document))",
    )
    for token in required:
        if token not in model:
            errors.append("QS3DFOCUS missing exact-focus token: " + token)

    for forbidden in (
        'SendStringToExecute("QS3DZOOMSELECTED',
        "new ViewportCommands().ZoomSelected()",
    ):
        if forbidden in model:
            errors.append("QS3DFOCUS must not re-enter or ambiently re-resolve zoom through: " + forbidden)

if viewport:
    for token in (
        '[CommandMethod("QS3DZOOMSELECTED", CommandFlags.Modal)]',
        "if (!TryZoomSelection(doc))",
        "internal static bool TryZoomSelection(Document document)",
        "var result = document.Editor.SelectImplied();",
        "document.Editor.SetCurrentView(view);",
    ):
        if token not in viewport:
            errors.append("Viewport exact zoom contract missing token: " + token)

    helper_start = viewport.find("internal static bool TryZoomSelection(Document document)")
    helper_end = viewport.find("private static Matrix3d WorldToDisplay", helper_start + 1) if helper_start >= 0 else -1
    if helper_start >= 0 and helper_end > helper_start:
        helper = viewport[helper_start:helper_end]
        for forbidden in ("MdiActiveDocument", "Active()", "SendStringToExecute"):
            if forbidden in helper:
                errors.append("Exact zoom helper must stay bound to its supplied Document; forbidden token: " + forbidden)
    elif helper_start >= 0:
        errors.append("Could not isolate exact zoom helper body for ambient-document guard.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DFOCUS zooms the resolved implied selection through the exact document-bound viewport helper without queued QS3D command re-entry; QS3DZOOMSELECTED shares the same canonical zoom routine.")
