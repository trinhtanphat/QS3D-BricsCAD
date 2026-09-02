#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "LINE curtain frames": {
        "path": "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
        "start": "public static CurtainFrameBuildResult BuildSelectedLineWalls(Document document, ProjectState project, bool allowInteractiveSelection = true)",
        "end": "private static void CommitSemanticUpdate",
        "audit": 'AuditTrail.ForProject(project).Record("geometry.curtain.frames"',
    },
    "path curtain frames": {
        "path": "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
        "start": "public static CurtainFrameBuildResult BuildSelectedOpenPolylines(Document document, ProjectState project, bool allowInteractiveSelection = true)",
        "end": "private static void CommitSemanticUpdate",
        "audit": 'AuditTrail.ForProject(project).Record("geometry.curtain.path.frames"',
    },
}

commit_token = "transaction.Commit();\n                    cadCommitted = true;"
semantic_token = "foreach (var update in pending) CommitSemanticUpdate(project, update);"
validate_token = "var previous = ValidatePrevious(document, transaction, project, element, ownership);"
erase_token = "ErasePrevious(transaction, project, element, previous);"

for label, contract in contracts.items():
    path = ROOT / contract["path"]
    if not path.is_file():
        errors.append(label + ": missing builder " + contract["path"])
        continue
    text = path.read_text(encoding="utf-8")
    if "using QS3D.Core.Persistence;" not in text:
        errors.append(label + ": missing ProjectStateSnapshot namespace")
    if 'private const string HandlesKey = "GeneratedCurtainFrameHandles";' not in text:
        errors.append(label + ": generated frame ownership key is missing or renamed")

    start = text.find(contract["start"])
    end = text.find(contract["end"], start + 1) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(label + ": cannot isolate build method")
        continue
    body = text[start:end]

    for token in (
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        validate_token,
        erase_token,
        semantic_token,
        commit_token,
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
    ):
        if token not in body:
            errors.append(label + ": missing atomic frame replacement contract: " + token)

    validate = body.find(validate_token)
    erase = body.find(erase_token)
    semantic = body.find(semantic_token)
    commit = body.find(commit_token)
    restore = body.find("rollback.Restore(project)")
    if min(validate, erase) >= 0 and not validate < erase:
        errors.append(label + ": complete previous handle set must be validated before destructive erase")
    if min(semantic, commit, restore) >= 0 and not semantic < commit < restore:
        errors.append(label + ": semantic ownership and audit-owned project revision must commit while CAD remains rollback-capable")
    if commit >= 0 and semantic_token in body[commit + len(commit_token):]:
        errors.append(label + ": semantic frame ownership is still mutated after CAD commit")
    if "project.Touch();" in body:
        errors.append(label + ": redundant project.Touch must stay removed because AuditTrail.Record owns revision advancement")

    helper = text[end:]
    for token in (
        "update.Element.Properties[HandlesKey]",
        "GeneratedCurtainFrameCount",
        "GeneratedCurtainFrameConfigFingerprint",
        "ClearGeneratedCurtainFrameStale()",
        "GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership",
        "Refusing destructive replacement before any frame is erased",
        contract["audit"],
    ):
        if token not in helper:
            errors.append(label + ": semantic/exact-set helper missing metadata, ownership or audit contract: " + token)

live_state = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveStateService.cs"
if not live_state.is_file():
    errors.append("missing CurtainWallFrameLiveStateService.cs")
else:
    text = live_state.read_text(encoding="utf-8")
    try_stamp_start = text.find("public static int TryStampSelected(Document document, ProjectState project, out string warning)")
    inspect_start = text.find("public static IReadOnlyList<ModelHealthIssue> Inspect", try_stamp_start + 1) if try_stamp_start >= 0 else -1
    if try_stamp_start < 0 or inspect_start < 0 or inspect_start <= try_stamp_start:
        errors.append("curtain live fingerprint best-effort contract missing: cannot isolate TryStampSelected")
        try_stamp = ""
    else:
        try_stamp = text[try_stamp_start:inspect_start]

    for token in (
        "public static int TryStampSelected(Document document, ProjectState project, out string warning)",
        "return StampSelected(document, project);",
        "catch (Exception)",
        'warning = "Live curtain fingerprint chưa được cập nhật; hãy chạy lại Curtain Frames 3D hoặc Health trước khi phát hành.";',
        "return 0;",
        "CURTAIN_FRAME_LIVE_FINGERPRINT_MISSING",
    ):
        if token not in text:
            errors.append("curtain live fingerprint best-effort contract missing: " + token)
    for forbidden in (
        'warning = "Không stamp được live curtain fingerprint: " + ex.Message;',
        "ex.Message",
        "exception.Message",
        "GetBaseException()",
        "StackTrace",
    ):
        if forbidden in try_stamp:
            errors.append("curtain live fingerprint warning must not expose caught host detail: " + forbidden)

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

print("PASS: LINE/path frame replacement validates the complete previous live set before erase; semantic ownership and AuditTrail-owned project revision commit before CAD commit inside the rollback boundary; live fingerprint stamping remains best-effort post-commit with stable redacted warning semantics.")
