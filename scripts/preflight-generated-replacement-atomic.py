#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs": (
        "public static int BuildSelectedLineWalls(Document document, ProjectState project, ElementCategory category)",
        "private static SourceBatchKind ValidateSourceBatch",
    ),
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": (
        "public static int BuildSelected(Document document, ProjectState project, ElementCategory category)",
        "private static void CommitWallPierPathSnapshot",
    ),
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs": (
        "public static int BuildSelected(Document document, ProjectState project, ElementCategory category)",
        "private static bool UsesLine",
    ),
    "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs": (
        "public static int BuildSelectedLinePiers(Document document, ProjectState project)",
        "private static void ClearPathProfileSnapshot",
    ),
}

for relative, (start_token, end_token) in contracts.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing generated replacement builder: " + relative)
        continue

    text = path.read_text(encoding="utf-8")
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append(relative + " missing Build method boundary for atomic replacement guard")
        continue
    body = text[start:end]

    required = (
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.MarkGenerated",
        "GeneratedGeometryService.CommitReplacement",
        "transaction.Commit();\n                    cadCommitted = true;",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
    )
    for needle in required:
        if needle not in body:
            errors.append(relative + " missing cross-layer atomicity contract: " + needle)

    semantic = body.find("GeneratedGeometryService.CommitReplacement")
    cad_commit = body.find("transaction.Commit();\n                    cadCommitted = true;")
    restore = body.find("rollback.Restore(project)")
    if min(semantic, cad_commit, restore) >= 0:
        if not semantic < cad_commit < restore:
            errors.append(relative + " must commit semantic replacement before CAD transaction commit and restore snapshot only on pre-commit failure")

    # The old unsafe pattern committed CAD first and only then iterated semantic pending updates.
    tail_after_commit = body[cad_commit + len("transaction.Commit();\n                    cadCommitted = true;"):] if cad_commit >= 0 else ""
    if "GeneratedGeometryService.CommitReplacement" in tail_after_commit:
        errors.append(relative + " still commits generated semantic ownership after CAD commit")

print("QS3D generated replacement cross-layer atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical Wall, polyline Wall/WallPier, Structural and WallPier-profile replacements commit semantic ownership while CAD is still rollback-capable and restore project state only when CAD has not committed.")
