#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGETS = [
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticElementTableBuilder.cs",
        "public static string Build(Document document, ProjectState project, Point3d position)",
        "public static void Remove(Document document, ProjectState project)",
        "AuditTrail.ForProject(project).Record(\"BuildSemanticElementTable\"",
        "semantic element table build",
        True,
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticElementTableBuilder.cs",
        "public static void Remove(Document document, ProjectState project)",
        "public static SemanticDocumentationTable BuildSnapshot",
        "AuditTrail.ForProject(project).Record(\"RemoveSemanticElementTable\"",
        "semantic element table remove",
        False,
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs",
        "public static string Build(",
        "public static void Remove(Document document, ProjectState project, ProjectOwnedNativeTableDefinition definition)",
        "\"documentation.table.replace\"",
        "shared native table build",
        True,
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs",
        "public static void Remove(Document document, ProjectState project, ProjectOwnedNativeTableDefinition definition)",
        "public static Point3d StoredPosition",
        "AuditTrail.ForProject(project).Record(\"documentation.table.remove\"",
        "shared native table remove",
        False,
    ),
]

errors = []

for path, start_token, end_token, audit_token, label, requires_model_space in TARGETS:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    start = text.find(start_token)
    end = text.find(end_token, start + len(start_token)) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(path.name + ": cannot isolate " + label)
        continue

    method = text[start:end]
    snapshot = method.find("ProjectStateSnapshot.Capture(project)")
    audit = method.find(audit_token)
    touch = method.find("project.Touch();", audit + 1)
    commit = method.find("transaction.Commit();", touch + 1)
    committed = method.find("committed = true;", commit + 1)
    if committed < 0:
        committed = method.find("cadCommitted = true;", commit + 1)
    restore = method.find("snapshot.Restore(project)", committed + 1)
    if restore < 0:
        restore = method.find("rollback.Restore(project)", committed + 1)

    if min(snapshot, audit, touch, commit, committed, restore) < 0:
        errors.append(path.name + ": missing snapshot/audit/touch/commit/rollback token in " + label)
    elif not snapshot < audit < touch < commit < committed < restore:
        errors.append(path.name + ": expected snapshot -> audit -> Touch -> CAD commit -> committed flag -> rollback catch in " + label)

    if method.count("project.Touch();") != 1:
        errors.append(path.name + ": " + label + " must Touch project exactly once")
    if method.find("project.Touch();", commit + 1) >= 0:
        errors.append(path.name + ": " + label + " must not Touch project after CAD commit")
    if "if (!committed)" not in method and "if (!cadCommitted)" not in method:
        errors.append(path.name + ": " + label + " rollback must remain guarded by CAD commit state")
    if "if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))" not in method:
        errors.append(path.name + ": " + label + " must fail closed when the target DWG is no longer active")
    if requires_model_space and "if (!document.Database.TileMode)" not in method:
        errors.append(path.name + ": " + label + " must remain ModelSpace-only")
    if "catch (Exception operationError)" not in method or "AggregateException(operationError, restoreError)" not in method:
        errors.append(path.name + ": " + label + " must preserve both operation and rollback failures")

print("QS3D native table transaction-boundary preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: native table build/remove stays document-bound, build stays ModelSpace-only, metadata/audit/revision commit before CAD, and rollback preserves compound failures.")
