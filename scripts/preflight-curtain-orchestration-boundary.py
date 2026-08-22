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
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "if (selected.Count == 0)",
        'ExistingProjectMutationContext.Require(document, "Curtain 3D")',
        "ProjectStateSnapshot.Capture(project)",
        "var nativeCommitted = false;",
        "new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)",
        "using (var commandTransaction = document.Database.TransactionManager.StartTransaction())",
        'phase = "LINE host replacement";',
        'phase = "open-POLYLINE host replacement";',
        'phase = "LINE frame replacement";',
        'phase = "open/bulged path frame replacement";',
        "WallSolidBuilder.BuildSelectedLineWalls(\n                            document,\n                            project,\n                            ElementCategory.GlassWall,\n                            allowPostCommitUi: false)",
        "PolylineWallSolidBuilder.BuildSelected(\n                            document,\n                            project,\n                            ElementCategory.GlassWall,\n                            allowPostCommitUi: false)",
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(\n                            document,\n                            project,\n                            allowInteractiveSelection: false)",
        "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(\n                            document,\n                            project,\n                            allowInteractiveSelection: false)",
        "commandTransaction.Commit();",
        "nativeCommitted = true;",
        "if (!nativeCommitted && rollback != null && project != null)",
        "rollback.Restore(project);",
        "ReportAtomicFailure(document, phase, nativeCommitted, ex)",
        "private static void ReportAtomicFailure",
        "ATOMIC ROLLBACK",
        "không có phase Curtain 3D nào được commit",
        'phase = "live fingerprint stamp";',
        "CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning)",
        "FinalizeUi(document, hostSolids, frameSolids, panelSolids, checked(stamped + panelsStamped), regenerated, stampWarning)",
        "UI sync warning: ",
    )
    for token in required:
        if token not in text:
            errors.append("Curtain atomic orchestration missing contract: " + token)

    forbidden = (
        "Curtain 3D PARTIAL COMMIT",
        "ReportPhaseFailure",
        "Các phase trước đã commit",
        "transaction riêng và không bị giả vờ rollback",
        "ProjectContextCoordinator.GetOrCreate(document)",
    )
    for token in forbidden:
        if token in text:
            errors.append("Curtain command still exposes obsolete/unsafe orchestration: " + token)

    order_tokens = (
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        'ExistingProjectMutationContext.Require(document, "Curtain 3D")',
        "ProjectStateSnapshot.Capture(project)",
        "RegenerateDirty(project)",
        "using (var commandTransaction = document.Database.TransactionManager.StartTransaction())",
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
        "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines",
        "commandTransaction.Commit();",
        "nativeCommitted = true;",
        'phase = "live fingerprint stamp";',
        "CurtainWallFrameLiveStateService.TryStampSelected",
    )
    positions = [text.find(token) for token in order_tokens]
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("Curtain input/bind/snapshot/native phases must remain ordered, with fingerprint work only after command commit")

    selection = text.find("EntitySnapshotReader.ReadCurrentSelection(document)")
    empty_return = text.find("if (selected.Count == 0)", selection)
    bind = text.find('ExistingProjectMutationContext.Require(document, "Curtain 3D")')
    if selection < 0 or empty_return < selection or bind < empty_return:
        errors.append("Curtain command must finish empty/cancel selection handling before canonical project binding")

    catch_start = text.find("catch (Exception ex)")
    reporter_start = text.find("private static void ReportAtomicFailure", catch_start + 1)
    catch = text[catch_start:reporter_start] if catch_start >= 0 and reporter_start > catch_start else ""
    restore = catch.find("rollback.Restore(project);")
    report = catch.find("ReportAtomicFailure(document, phase, nativeCommitted, ex)")
    if "if (!nativeCommitted && rollback != null && project != null)" not in catch or restore < 0 or report < 0 or restore > report:
        errors.append("Curtain failure path must restore semantic state before reporting an uncommitted native failure")

    if text.count("document.Database.TransactionManager.StartTransaction()") != 1:
        errors.append("Curtain command must own exactly one outer native transaction; canonical builders keep their own nested transactions")

    if "CurtainWallFrameLiveStateService.StampSelected" in text:
        errors.append("Curtain live fingerprint stamping must remain best-effort after the atomic native commit")

print("QS3D Curtain orchestration atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DCURTAIN3D handles selection before canonical binding, snapshots semantic state, encloses ordered host/frame builders in one outer native transaction, restores semantic state when that transaction aborts, removes obsolete partial-commit reporting, and keeps fingerprint/UI work post-commit and non-fatal.")
