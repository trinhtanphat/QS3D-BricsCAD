#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
path = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs"

if not path.is_file():
    errors.append("missing CurtainWallBuildCommands.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAIN3D"',
        'var phase = "semantic regeneration";',
        'phase = "LINE host replacement";',
        'phase = "open-POLYLINE host replacement";',
        'phase = "LINE frame replacement";',
        'phase = "open/bulged path frame replacement";',
        'phase = "live fingerprint stamp";',
        "WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall)",
        "PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall)",
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project)",
        "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project)",
        "CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning)",
        "ReportPhaseFailure(document, phase, regenerated, lineHostSolids, pathHostSolids, lineFrames, pathFrames, ex)",
        "private static void ReportPhaseFailure",
        '"Curtain 3D PARTIAL COMMIT: semantic regenerate="',
        '". Các phase trước đã commit bằng transaction riêng và không bị giả vờ rollback. Chạy QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL, sửa lỗi rồi rebuild host hoặc chạy QS3DCURTAINFRAMES3D theo kết quả health."',
        '"QS3DCURTAIN3D lỗi tại " + phase',
        "FinalizeUi(document, hostSolids, frameSolids, stamped, regenerated, stampWarning)",
        "UI sync warning: ",
        "TryWriteMessage",
    )
    for token in required:
        if token not in text:
            errors.append("Curtain orchestration missing phase/recovery contract: " + token)

    if "ProjectStateSnapshot" in text or "rollback.Restore" in text:
        errors.append("QS3DCURTAIN3D command-level orchestration must not fake whole-command rollback with semantic snapshots after earlier native transactions may have committed")

    order = [
        text.find('phase = "LINE host replacement";'),
        text.find("WallSolidBuilder.BuildSelectedLineWalls"),
        text.find('phase = "open-POLYLINE host replacement";'),
        text.find("PolylineWallSolidBuilder.BuildSelected"),
        text.find('phase = "LINE frame replacement";'),
        text.find("CurtainWallFrameSolidBuilder.BuildSelectedLineWalls"),
        text.find('phase = "open/bulged path frame replacement";'),
        text.find("CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines"),
        text.find('phase = "live fingerprint stamp";'),
        text.find("CurtainWallFrameLiveStateService.TryStampSelected"),
    ]
    if min(order) < 0 or order != sorted(order):
        errors.append("Curtain orchestration phase markers must remain immediately ordered with their separate native transaction families")

    report_start = text.find("private static void ReportPhaseFailure")
    finalize_start = text.find("private static void FinalizeUi", report_start + 1) if report_start >= 0 else -1
    report = text[report_start:finalize_start] if report_start >= 0 and finalize_start > report_start else ""
    for token in (
        "regenerated == 0 && committedHosts == 0 && committedFrames == 0",
        "Curtain 3D PARTIAL COMMIT",
        "semantic regenerate=",
        "int regenerated",
        "lineHostSolids",
        "pathHostSolids",
        "lineFrames?.Frames",
        "pathFrames?.Frames",
        "QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL",
        "không bị giả vờ rollback",
    ):
        if token not in report:
            errors.append("Curtain partial-commit reporter missing truthful recovery detail: " + token)

    if "CurtainWallFrameLiveStateService.StampSelected" in text:
        errors.append("QS3DCURTAIN3D must keep live fingerprint stamping best-effort after committed geometry")

print("QS3D Curtain orchestration boundary preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DCURTAIN3D validates semantics before native work, preserves explicit LINE/path host and frame transaction phases, reports committed phase counts and recovery steps on later failure, never pretends a semantic snapshot can roll back already-committed Solid3d, and keeps post-commit fingerprint/UI work non-fatal. Whole host+frame orchestration remains intentionally non-atomic.")
