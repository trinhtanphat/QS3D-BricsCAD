#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

builders = {
    "LINE wall": ROOT / "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
    "POLYLINE wall": ROOT / "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
    "WallPier profile": ROOT / "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs",
    "structural": ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
}

for label, path in builders.items():
    if not path.is_file():
        errors.append(label + ": missing builder " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    required = (
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "cadCommitted = false",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.MarkGenerated",
        "GeneratedGeometryService.CommitReplacement",
        "transaction.Commit();",
        "cadCommitted = true",
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
    )
    for token in required:
        if token not in text:
            errors.append(label + ": missing atomic replacement contract: " + token)

    semantic_index = text.find("GeneratedGeometryService.CommitReplacement")
    cad_commit_index = text.find("transaction.Commit();", semantic_index if semantic_index >= 0 else 0)
    committed_flag_index = text.find("cadCommitted = true", cad_commit_index if cad_commit_index >= 0 else 0)
    rollback_index = text.find("rollback.Restore(project)")

    if semantic_index < 0 or cad_commit_index < 0 or semantic_index > cad_commit_index:
        errors.append(label + ": semantic generated-handle ownership must be applied while the CAD transaction is still rollback-capable")
    if cad_commit_index < 0 or committed_flag_index < 0 or committed_flag_index < cad_commit_index:
        errors.append(label + ": cadCommitted must become true only after transaction.Commit succeeds")
    if rollback_index < 0:
        errors.append(label + ": project snapshot rollback is missing")

    # A second semantic ownership update after the DB transaction would reintroduce the
    # original split-brain window. Additional CommitReplacement calls are therefore forbidden.
    if semantic_index >= 0 and text.find("GeneratedGeometryService.CommitReplacement", semantic_index + 1) >= 0:
        errors.append(label + ": multiple CommitReplacement phases found; semantic ownership must have one in-transaction commit phase")

snapshot = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
if not snapshot.is_file():
    errors.append("missing ProjectStateSnapshot rollback primitive")
else:
    text = snapshot.read_text(encoding="utf-8")
    for token in (
        "public static ProjectStateSnapshot Capture(ProjectState project)",
        "public void Restore(ProjectState project)",
        "target.Elements.Clear()",
        "target.AuditEvents.Clear()",
        "target.Metadata.Clear()",
        "RestorePersistenceState",
    ):
        if token not in text:
            errors.append("ProjectStateSnapshot missing deep rollback contract: " + token)

print("QS3D generated replacement atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: LINE/POLYLINE wall, WallPier profile and structural generated-solid replacements apply semantic ownership before CAD commit and restore the full project snapshot if that cross-layer commit fails.")
