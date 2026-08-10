#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
UI = ADAPTER / "UI"
errors = []

files = {
    "lifetime": UI / "DocumentBoundWindowLifetime.cs",
    "recognition": UI / "RecognitionWindow.xaml.cs",
    "revision": UI / "RevisionWindow.xaml.cs",
    "health": UI / "ModelHealthWindow.xaml.cs",
    "bq": UI / "QuantitySummaryWindow.xaml.cs",
    "bbs": UI / "RebarScheduleWindow.xaml.cs",
    "door_schedule": UI / "DoorOpeningScheduleWindow.xaml.cs",
    "room_schedule": UI / "RoomFinishScheduleWindow.xaml.cs",
    "commands": ADAPTER / "Commands.cs",
    "review": ADAPTER / "ReviewCommands.cs",
}
for key, path in files.items():
    if not path.is_file():
        errors.append("missing modeless lifetime source: " + str(path.relative_to(ROOT)))

if not errors:
    text = {key: path.read_text(encoding="utf-8") for key, path in files.items()}

    for needle in (
        "DocumentToBeDestroyed += OnDocumentToBeDestroyed",
        "DocumentToBeDestroyed -= OnDocumentToBeDestroyed",
        "ReferenceEquals(e.Document, _document)",
        "_window.Closed += OnWindowClosed",
        "_window.Closed -= OnWindowClosed",
        "_window.Dispatcher.CheckAccess()",
        "_window.Dispatcher.BeginInvoke(new Action(_window.Close))",
    ):
        if needle not in text["lifetime"]:
            errors.append("document-bound lifetime coordinator missing: " + needle)

    for key in ("recognition", "revision", "health", "bq", "bbs", "door_schedule", "room_schedule"):
        if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text[key]:
            errors.append(key + " window must auto-close when its source DWG is destroyed")

    for key, signature in (
        ("bq", "QuantitySummaryWindow(Document document"),
        ("bbs", "RebarScheduleWindow(Document document"),
        ("health", "ModelHealthWindow(Document document"),
        ("door_schedule", "DoorOpeningScheduleWindow(Document document"),
        ("room_schedule", "RoomFinishScheduleWindow(Document document"),
    ):
        if signature not in text[key]:
            errors.append(key + " must require an explicit source Document")

    if "public ModelHealthWindow(IReadOnlyList<ModelHealthIssue> issues" in text["health"]:
        errors.append("legacy ambient ModelHealthWindow constructor must not return")
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in text["bq"]:
        errors.append("QuantitySummaryWindow must not capture ambient MdiActiveDocument")
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in text["bbs"]:
        errors.append("RebarScheduleWindow must not capture ambient MdiActiveDocument")

    if "new QuantitySummaryWindow(doc, rows, locate, recalculate)" not in text["commands"]:
        errors.append("QS3DBQ launcher must pass its source Document to QuantitySummaryWindow")
    if "new RebarScheduleWindow(doc, rows, locate, fileName)" not in text["review"]:
        errors.append("QS3DBBSVIEW launcher must pass its source Document to RebarScheduleWindow")

print("QS3D document-bound modeless lifetime preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: core modeless review/health/BQ/BBS/Door/HT_Phòng windows are explicitly bound to their source Document and unregister/close when that DWG is destroyed, preventing stale Document/project retention after close.")
