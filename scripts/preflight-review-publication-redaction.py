#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


for token in (
    "_bbsPending",
    "_recognitionPending",
    "_revisionPending",
    "SetPending(surface, owner);",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!candidate.IsLoaded)",
    "SetPublished(surface, owner);",
    "SetPending(surface, null);",
    "pending.MatchesNativeDatabase(document)",
    "pending.MatchesManagedWrapper(document)",
):
    require(token in source, "review pending/publication contract missing: " + token)

reserve = source.find("SetPending(surface, owner);")
show = source.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", reserve)
publish = source.find("SetPublished(surface, owner);", show)
release = source.find("SetPending(surface, null);", publish)
require(min(reserve, show, publish, release) >= 0 and reserve < show < publish < release,
        "review publication must reserve pending before host show and release pending only after loaded publication")

for forbidden in (
    '" + uiError.Message',
    '" + ex.Message',
    'operation + " error: " + ex.Message',
    'operation + " lỗi: " + ex.Message',
):
    require(forbidden not in source, "raw exception detail remains on review user surface: " + forbidden)

for stable in (
    "Recognition batch đã commit; UI refresh không hoàn tất. Dữ liệu đã commit vẫn được giữ nguyên.",
    "Recognition auto batch đã commit; UI refresh không hoàn tất. Dữ liệu đã commit vẫn được giữ nguyên.",
    "Recognition auto batch đã rollback an toàn; không giữ partial semantic capture.",
    "thao tác đã dừng an toàn.",
):
    require(stable in source, "stable review failure/warning message missing: " + stable)

for semantic in (
    "RecognitionApplyBatchService.PrepareStrict",
    "RecognitionApplyBatchService.PrepareBestEffort",
    "RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan)",
    "RecognitionApplyBatchService.Commit(doc, reviewProjectId, autoPlan)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RevisionCoordinator.CaptureCurrent(doc)",
):
    require(semantic in source, "review product semantic lost while redacting failures: " + semantic)

if errors:
    print("Review publication/redaction preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS review modeless publication is pending-first and recognition failures are stable/redacted")
