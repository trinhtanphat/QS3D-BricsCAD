#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/GridCommands.cs"
text = path.read_text(encoding="utf-8")

required = [
    'ReportOperationFailure(document, "QS3DGRID lỗi: không thể hoàn tất Grid capture.")',
    'ReportOperationFailure(document, "QS3DGRIDINTERSECTIONHEALTH lỗi: không thể kiểm tra Grid intersection markers.")',
    '"QS3DGRIDINTERSECTIONSSEL lỗi: không thể refresh Grid intersection markers cho selection."',
    '"QS3DGRIDINTERSECTIONS lỗi: không thể refresh Grid intersection markers."',
    'var uiSyncFailed = false;',
    'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
    'try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }',
    'TryWriteMessage(document, "\\nQS3D Grid: semantic capture đã hoàn tất; một phần UI không thể đồng bộ.");',
    'try { PaletteCoordinator.SetStatus(message); } catch { }',
    'try { document.Editor.WriteMessage(message); } catch { }',
]

failures = []
for token in required:
    if token not in text:
        failures.append("missing stable/fail-isolated token: " + token)

for forbidden in ["ex.Message", "exception.Message", "Exception.Message"]:
    if forbidden in text:
        failures.append("raw exception detail remains in Grid command surface: " + forbidden)

capture = text.find("public void CaptureGrid()")
health = text.find("public void InspectIntersectionMarkers()")
refresh = text.find("private static void RefreshIntersectionMarkers(bool selectedOnly)")
finalize = text.find("private static void FinalizeUi(Document document, int count, string subtype)")
report = text.find("private static void ReportOperationFailure", finalize)
if min(capture, health, refresh, finalize, report) < 0:
    failures.append("Grid command lifecycle boundaries are incomplete")
else:
    finalize_body = text[finalize:report]
    if "SemanticCaptureService.Capture" in finalize_body:
        failures.append("FinalizeUi must remain post-commit/UI-only")
    if finalize_body.count("catch") < 2:
        failures.append("post-commit palette refresh and status must fail independently")

health_body = text[health:refresh] if health >= 0 and refresh >= 0 else ""
if "GetOrCreate(document)" in health_body:
    failures.append("health path must stay read-only/non-creating")

if failures:
    print("Grid command failure redaction guard FAILED:")
    for failure in failures:
        print(" - " + failure)
    sys.exit(1)

print("Grid command failure redaction guard PASS")
