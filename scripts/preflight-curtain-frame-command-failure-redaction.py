#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs"
LIVE = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveStateService.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing CurtainWallFrameCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DCURTAINFRAMES3D", CommandFlags.UsePickSet)]',
        'EntitySnapshotReader.ReadCurrentSelection(document)',
        'ExistingProjectMutationContext.Require(document, "Curtain Frames 3D")',
        'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project)',
        'CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project)',
        'CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning)',
        'FinalizeUi(document, message, stampWarning)',
        'QS3DCURTAINFRAMES3D không thể hoàn tất.',
        'var uiSyncFailed = false;',
        'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
        'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
        'try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }',
        'TryWriteMessage(document, "\\nQS3D warning: " + stampWarning)',
        'Curtain Frames 3D: native update đã hoàn tất; một phần UI không thể đồng bộ.',
    )
    for token in required:
        if token not in text:
            errors.append("missing command lifecycle/redaction token: " + token)

    for forbidden in ("ex.Message", "exception.Message", "GetBaseException()", "StackTrace", "UI sync warning: ", "catch (Exception ex)"):
        if forbidden in text:
            errors.append("Curtain Frame command exposes caught host/UI detail: " + forbidden)

    selection = text.find("EntitySnapshotReader.ReadCurrentSelection(document)")
    admission = text.find('ExistingProjectMutationContext.Require(document, "Curtain Frames 3D")')
    line = text.find("CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project)")
    path = text.find("CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project)")
    stamp = text.find("CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning)")
    finalize = text.find("FinalizeUi(document, message, stampWarning)")
    if min(selection, admission, line, path, stamp, finalize) < 0 or not selection < admission < line < path < stamp < finalize:
        errors.append("Curtain Frame ordering must remain selection -> project admission -> line/path native build -> fingerprint stamp -> post-commit UI")

if not LIVE.is_file():
    errors.append("missing CurtainWallFrameLiveStateService.cs")
else:
    text = LIVE.read_text(encoding="utf-8")
    start = text.find("public static int TryStampSelected")
    end = text.find("public static IReadOnlyList<ModelHealthIssue> Inspect")
    if min(start, end) < 0 or start >= end:
        errors.append("cannot resolve TryStampSelected block")
    else:
        block = text[start:end]
        for token in (
            "return StampSelected(document, project);",
            "catch (Exception)",
            "Live curtain fingerprint chưa được cập nhật; hãy chạy lại Curtain Frames 3D hoặc Health trước khi phát hành.",
            "return 0;",
        ):
            if token not in block:
                errors.append("fingerprint warning boundary missing: " + token)
        for forbidden in ("ex.Message", "exception.Message", "GetBaseException()", "StackTrace"):
            if forbidden in block:
                errors.append("TryStampSelected exposes caught host detail: " + forbidden)

print("QS3D Curtain Frame command failure-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: standalone Curtain Frame orchestration preserves native ordering/fingerprint semantics while command, fingerprint-pending, and post-commit UI surfaces redact host detail and fail-isolate presentation.")
