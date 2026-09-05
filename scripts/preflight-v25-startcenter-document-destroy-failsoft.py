#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
errors = []

if not SOURCE.is_file():
    print("ERROR: missing BltStartCenterWindow source")
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
start = source.find("private void OnHostDocumentToBeDestroyed")
end = source.find("private void QueueHomeRefresh", start + 1)
handler = source[start:end] if start >= 0 and end > start else ""

if not handler:
    errors.append("OnHostDocumentToBeDestroyed block not found")
else:
    for token, label in (
        ("e.Document", "destroy callback must compare the destroying document"),
        ("Application.DocumentManager.MdiActiveDocument", "destroy callback must query current active document only inside the callback"),
        ("ReferenceEquals(destroyingDocument, activeDocument)", "safe active-document identity must preserve background-close semantics"),
        ("ActiveDrawingRecordIntent.Suppress", "destroy callback must support fail-closed record suppression"),
    ):
        if token not in handler:
            errors.append(label + ": missing " + token)

    lookup = handler.find("Application.DocumentManager.MdiActiveDocument")
    try_pos = handler.find("try")
    catch_pos = handler.find("catch", lookup + 1 if lookup >= 0 else 0)
    suppress_pos = handler.find("QueueHomeRefresh(ActiveDrawingRecordIntent.Suppress)", catch_pos + 1 if catch_pos >= 0 else 0)
    return_pos = handler.find("return;", catch_pos + 1 if catch_pos >= 0 else 0)

    if lookup < 0:
        pass
    elif try_pos < 0 or try_pos > lookup:
        errors.append("active-document getter must execute inside a fail-soft try block")
    if lookup >= 0 and catch_pos < 0:
        errors.append("active-document getter must have a teardown catch boundary")
    if catch_pos >= 0 and suppress_pos < 0:
        errors.append("getter failure must suppress queued active-drawing recording")
    if catch_pos >= 0 and return_pos < 0:
        errors.append("getter failure must return before evaluating stale document identity")
    if catch_pos >= 0 and suppress_pos >= 0 and return_pos >= 0 and suppress_pos > return_pos:
        errors.append("fail-closed suppression must occur before returning")

# Do not paper over the native event with retry: teardown identity is a one-shot observation.
if "MdiActiveDocument" in handler and handler.count("MdiActiveDocument") != 1:
    errors.append("destroy handler must perform exactly one active-document observation; retry can cross document generations")

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: Start Center document-destroy handling fails soft and suppresses stale active-drawing recording when host identity lookup fails.")
