#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "LINE curtain frames": {
        "path": "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
        "start": "public static CurtainFrameBuildResult BuildSelectedLineWalls(Document document, ProjectState project)",
        "end": "private static void CommitSemanticUpdate",
        "audit": 'AuditTrail.ForProject(project).Record("geometry.curtain.frames"',
    },
    "path curtain frames": {
        "path": "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
        "start": "public static CurtainFrameBuildResult BuildSelectedOpenPolylines(Document document, ProjectState project)",
        "end": "private static void CommitSemanticUpdate",
        "audit": 'AuditTrail.ForProject(project).Record("geometry.curtain.path.frames"',
    },
}

commit_token = "transaction.Commit();\n                    cadCommitted = true;"
semantic_token = "foreach (var update in pending) CommitSemanticUpdate(project, update);"

for label, contract in contracts.items():
    path = ROOT / contract["path"]
    if not path.is_file():
        errors.append(label + ": missing builder " + contract["path"])
        continue
    text = path.read_text(encoding="utf-8")
    if "using QS3D.Core.Persistence;" not in text:
        errors.append(label + ": missing ProjectStateSnapshot namespace")

    start = text.find(contract["start"])
    end = text.find(contract["end"], start + 1) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(label + ": cannot isolate build method")
        continue
    body = text[start:end]

    for token in (
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "ErasePrevious(document, transaction, element, ownership)",
        semantic_token,
        commit_token,
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
    ):
        if token not in body:
            errors.append(label + ": missing atomic frame replacement contract: " + token)

    semantic = body.find(semantic_token)
    commit = body.find(commit_token)
    restore = body.find("rollback.Restore(project)")
    if min(semantic, commit, restore) >= 0 and not semantic < commit < restore:
        errors.append(label + ": semantic frame ownership must be committed while CAD is rollback-capable")
    if commit >= 0 and semantic_token in body[commit + len(commit_token):]:
        errors.append(label + ": semantic frame ownership is still mutated after CAD commit")

    helper = text[end:]
    for token in (
        "GeneratedCurtainFrameHandles",
        "GeneratedCurtainFrameCount",
        "GeneratedCurtainFrameConfigFingerprint",
        "ClearGeneratedCurtainFrameStale()",
        contract["audit"],
    ):
        if token not in helper:
            errors.append(label + ": semantic commit helper missing metadata/audit contract: " + token)

    touch = body.find("if (pending.Count > 0) project.Touch();")
    if touch >= 0 and commit >= 0 and touch < commit:
        errors.append(label + ": project.Touch must remain post-CAD-commit")

live_state = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveStateService.cs"
if not live_state.is_file():
    errors.append("missing CurtainWallFrameLiveStateService.cs")
else:
    text = live_state.read_text(encoding="utf-8")
    for token in (
        "public static int TryStampSelected(Document document, ProjectState project, out string warning)",
        "return StampSelected(document, project);",
        'warning = "Không stamp được live curtain fingerprint: " + ex.Message;',
        "return 0;",
        "CURTAIN_FRAME_LIVE_FINGERPRINT_MISSING",
    ):
        if token not in text:
            errors.append("curtain live fingerprint best-effort contract missing: " + token)

for relative in (
    "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
):
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing curtain command surface: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    if "CurtainWallFrameLiveStateService.TryStampSelected" not in text:
        errors.append(relative + " must use best-effort live fingerprint stamping after geometry commit")
    if "CurtainWallFrameLiveStateService.StampSelected" in text:
        errors.append(relative + " must not let direct StampSelected failure convert valid geometry commit into command failure")
    if "fingerprint pending" not in text or "stampWarning" not in text:
        errors.append(relative + " must surface missing live fingerprint as a warning/pending health state")

print("QS3D curtain frame cross-layer atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: LINE and path curtain frame builders replace old owned frames and write handles/count/fingerprint/stale/audit state before CAD commit with deep snapshot rollback; post-commit live fingerprint stamping is best-effort and cannot misreport valid geometry as failed. Whole QS3DCURTAIN3D host+frame orchestration remains a separate transaction-boundary concern.")
