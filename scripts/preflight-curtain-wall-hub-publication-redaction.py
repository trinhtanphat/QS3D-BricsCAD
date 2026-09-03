#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
path = root / "src/QS3D.BricsCAD.V25/CurtainWallHubCommands.cs"
text = path.read_text(encoding="utf-8") if path.is_file() else ""
errors = []

for token in (
    "private static CurtainWallWindow? _pendingWindow;",
    "if (!ReservePendingWindow(candidate, document, nativeDatabaseIdentity))",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))",
    "if (ReferenceEquals(_pendingWindow, window))",
    "ReportFailure(document);",
    'const string message = "QS3DCURTAIN lỗi: không thể mở Vách Kính Hub; kiểm tra document/CAD state và thử lại.";',
    "try { PaletteCoordinator.SetStatus(message); } catch { }",
    "try { document.Editor.WriteMessage(message); } catch { }",
):
    if token not in text:
        errors.append("missing Curtain Wall publication/redaction token: " + token)

for forbidden in ("ex.Message", "exception.Message", "GetBaseException()", "StackTrace"):
    if forbidden in text:
        errors.append("Curtain Wall Hub user surface exposes raw exception detail: " + forbidden)

show_start = text.find("public void ShowCurtainWallHub()")
prepare_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
show = text[show_start:prepare_start] if show_start >= 0 and prepare_start > show_start else ""
reserve = show.find("ReservePendingWindow(candidate, document, nativeDatabaseIdentity)")
host_show = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", reserve + 1)
promote = show.find("PromotePendingWindow(candidate, document, nativeDatabaseIdentity)", host_show + 1)
if min(reserve, host_show, promote) < 0 or not (reserve < host_show < promote):
    errors.append("pending ownership must be reserved before host show and promoted only afterward")

catch_pos = show.find("catch (System.Exception)")
release_pos = show.find("ReleaseOwnedWindow(candidate);", catch_pos + 1)
close_pos = show.find("TryClose(candidate);", release_pos + 1)
report_pos = show.find("ReportFailure(document);", close_pos + 1)
if min(catch_pos, release_pos, close_pos, report_pos) < 0 or not (catch_pos < release_pos < close_pos < report_pos):
    errors.append("failure path must release exact ownership and close candidate before stable reporting")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: Curtain Wall Hub reserves publication before reentrant host show and redacts/fail-isolates command failure reporting.")
