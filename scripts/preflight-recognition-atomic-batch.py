#!/usr/bin/env python3
"""Source-safe regression guard for V25 recognition batch atomicity.

This guard intentionally avoids BricsCAD runtime dependencies. It pins the transaction
shape that prevents RecognitionWindow, QS3DRECOGNIZEAUTO and QS3DB4D from regressing
back to independent per-row semantic commits.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/RecognitionApplyBatchService.cs"


def fail(message: str) -> None:
    print("FAIL: " + message)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(label + " missing required token: " + token)


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(label + " contains forbidden per-row regression token: " + token)


def main() -> int:
    window = WINDOW.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
    service = SERVICE.read_text(encoding="utf-8")

    require(window, "Func<IReadOnlyList<RecognitionResult>, bool, int>? _apply", "RecognitionWindow")
    require(window, "var applied = _apply(batch, requireLiveConfidence);", "RecognitionWindow")
    require(window, "requireLiveConfidence: false", "RecognitionWindow")
    require(window, "requireLiveConfidence: true", "RecognitionWindow")
    require(window, "RefreshStatus(0, batch.Count, \"Apply batch: \" + ex.Message);", "RecognitionWindow")
    forbid(window, "_apply(row)", "RecognitionWindow")

    require(commands, "Func<IReadOnlyList<RecognitionResult>, bool, int> apply", "ReviewCommands")
    require(commands, "RecognitionApplyBatchService.PrepareStrict", "ReviewCommands")
    require(commands, "requireAutoAcceptance: requireLiveConfidence", "ReviewCommands")
    require(commands, "RecognitionApplyBatchService.PrepareBestEffort", "ReviewCommands")
    require(commands, "RecognitionApplyBatchService.Commit", "ReviewCommands")
    require(commands, "batch đã rollback, không giữ partial semantic capture", "ReviewCommands")
    forbid(commands, "foreach (var result in batch.AutoAccepted)\n                    {\n                        try { apply(result); }", "ReviewCommands")

    require(service, "bool requireAutoAcceptance = false", "RecognitionApplyBatchService")
    require(service, "var rollback = ProjectStateSnapshot.Capture(project);", "RecognitionApplyBatchService")
    require(service, "SemanticCaptureService.CaptureSnapshot(document, item.Snapshot, item.Category)", "RecognitionApplyBatchService")
    require(service, "rollback.Restore(project);", "RecognitionApplyBatchService")
    require(service, "project.ChangeVersion != plan.ProjectChangeVersion", "RecognitionApplyBatchService")
    require(service, "EntitySnapshotReader.ReadHandles", "RecognitionApplyBatchService")
    require(service, "SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner", "RecognitionApplyBatchService")
    require(service, "AuditTrail.ForProject(project)", "RecognitionApplyBatchService")
    require(service, "private const double AutoAcceptConfidence = 0.92d;", "RecognitionApplyBatchService")
    require(service, "private const double AutoAcceptMinimumMargin = 0.15d;", "RecognitionApplyBatchService")
    require(service, "candidate.Confidence < AutoAcceptConfidence || refreshed.Margin < AutoAcceptMinimumMargin", "RecognitionApplyBatchService")
    require(service, "PrepareOne(document, project, result, requireAutoAcceptance)", "RecognitionApplyBatchService")
    require(service, "PrepareOne(document, project, result, requireAutoAcceptance: true)", "RecognitionApplyBatchService")

    commit_at = service.index("public static int Commit")
    rollback_at = service.index("var rollback = ProjectStateSnapshot.Capture(project);", commit_at)
    capture_at = service.index("SemanticCaptureService.CaptureSnapshot", rollback_at)
    audit_at = service.index("AuditTrail.ForProject(project)", capture_at)
    restore_at = service.index("rollback.Restore(project);", audit_at)
    if not (commit_at < rollback_at < capture_at < audit_at < restore_at):
        fail("RecognitionApplyBatchService transaction ordering is no longer rollback -> capture -> audit -> restore-on-error")

    prepare_region = service[:commit_at]
    if "SemanticCaptureService.CaptureSnapshot" in prepare_region:
        fail("recognition preflight must remain mutation-free")

    print("PASS: recognition batch apply is source-guarded as preflight-first, live-gated and all-or-nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
