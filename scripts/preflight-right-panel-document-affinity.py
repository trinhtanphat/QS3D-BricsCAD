#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required = [
    "private Document? _publishedDocument;",
    "private bool TryGetPublishedActiveDocument(out Document document)",
    "ReferenceEquals(active, _publishedDocument)",
    "private void InvalidatePublishedCadState",
    "_publishedDocument = null;",
    "RefreshDrawingsOnly(doc)",
    "ReloadLayers(doc)",
]
for needle in required:
    if needle not in text:
        errors.append("RightPanel missing document-affinity token: " + needle)


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

# Every CAD-mutating action that consumes panel row semantics must acquire the
# exact published owner instead of resolving an arbitrary new active document.
mutating_methods = [
    ("private void OnClearDrawingSelectionClick", "private void OnDrawingSelectionChanged"),
    ("private void OnDrawingSelectionChanged", "private void OnLayerChecked"),
    ("private void SetLayerFromCheckBox", "private void SetSelectedLayers"),
    ("private void SetSelectedLayers", "private void SetSelectedLayerLocks"),
    ("private void SetSelectedLayerLocks", "private void TryReloadLayersAfterFailure"),
    ("private void OnReloadXrefClick", "private void OnMoveDrawingClick"),
    ("private void OnMoveDrawingClick", "private void OnDeleteDrawingClick"),
    ("private void OnDeleteDrawingClick", "private void OnAttachXrefClick"),
]
for signature, next_signature in mutating_methods:
    body = method_body(signature, next_signature)
    if body and "TryGetPublishedActiveDocument(out var doc)" not in body:
        errors.append(signature + " must execution-time fence the published RightPanel owner")

refresh = method_body("public void Refresh()", "private void ReloadLayers")
if refresh:
    if "_publishedDocument = doc;" not in refresh:
        errors.append("Refresh must publish the exact document only after its rows are read")
    if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, doc)" not in refresh:
        errors.append("Refresh must reject an MDI switch before publishing row ownership")

reload_layers = method_body("private void ReloadLayers(Document doc)", "private void ApplyLayerFilter")
if reload_layers and "MdiActiveDocument" in reload_layers:
    errors.append("ReloadLayers must use the caller-pinned owner document, not re-resolve MdiActiveDocument")

refresh_drawings = method_body("private void RefreshDrawingsOnly(Document doc)", "private void RefreshAfterXrefMutation")
if refresh_drawings and "MdiActiveDocument" in refresh_drawings:
    errors.append("RefreshDrawingsOnly must use the caller-pinned owner document, not re-resolve MdiActiveDocument")

print("QS3D RightPanel document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: RightPanel publishes rows for one exact owner document and every CAD mutation fails closed if those rows are stale after an MDI switch.")
