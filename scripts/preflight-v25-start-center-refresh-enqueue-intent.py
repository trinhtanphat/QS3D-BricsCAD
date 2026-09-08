from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

start = text.find("private void QueueHomeRefresh(ActiveDrawingRecordIntent intent)")
end = text.find("private void DrainQueuedHomeRefresh()", start)
if start < 0 or end < 0:
    print("ERROR: cannot locate QueueHomeRefresh/DrainQueuedHomeRefresh boundary")
    sys.exit(1)

body = text[start:end]
required = [
    "Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(DrainQueuedHomeRefresh));",
    "catch",
    "_hostRefreshQueued = false;",
    "_queuedActiveDrawingRecordIntent = ActiveDrawingRecordIntent.Preserve;",
]
missing = [needle for needle in required if needle not in body]
if missing:
    print("ERROR: Start Center deferred refresh enqueue failure must clear both queue ownership and active-drawing intent")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)

begin = body.find("Dispatcher.BeginInvoke")
catch = body.find("catch", begin)
queue_clear = body.find("_hostRefreshQueued = false;", catch)
intent_clear = body.find("_queuedActiveDrawingRecordIntent = ActiveDrawingRecordIntent.Preserve;", catch)
if min(begin, catch, queue_clear, intent_clear) < 0 or not (begin < catch < queue_clear < intent_clear):
    print("ERROR: enqueue failure cleanup must occur in catch after BeginInvoke and clear queue ownership before stale intent")
    sys.exit(1)

if "Bricscad.ApplicationServices.Document" in body or "MdiActiveDocument" in body:
    print("ERROR: QueueHomeRefresh must not capture/retain native Document wrappers while scheduling deferred UI work")
    sys.exit(1)

print("PASS: Start Center failed deferred enqueue abandons stale active-drawing intent")
