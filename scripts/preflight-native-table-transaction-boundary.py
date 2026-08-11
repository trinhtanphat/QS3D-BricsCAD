#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SNAPSHOT_STATE = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
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
    commit = method.find("transaction.Commit();", audit + 1)
    committed = method.find("committed = true;", commit + 1)
    if committed < 0:
        committed = method.find("cadCommitted = true;", commit + 1)
    restore = method.find("snapshot.Restore(project)", committed + 1)
    if restore < 0:
        restore = method.find("rollback.Restore(project)", committed + 1)

    if min(snapshot, audit, commit, committed, restore) < 0:
        errors.append(path.name + ": missing snapshot/audit/commit/rollback token in " + label)
    elif not snapshot < audit < commit < committed < restore:
        errors.append(path.name + ": expected snapshot -> audit/revision -> CAD commit -> committed flag -> rollback catch in " + label)

    if "project.Touch();" in method:
        errors.append(path.name + ": " + label + " must not duplicate the audit-owned project Touch")
    if "if (!committed)" not in method and "if (!cadCommitted)" not in method:
        errors.append(path.name + ": " + label + " rollback must remain guarded by CAD commit state")
    if "if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))" not in method:
        errors.append(path.name + ": " + label + " must fail closed when the target DWG is no longer active")
    if requires_model_space and "if (!document.Database.TileMode)" not in method:
        errors.append(path.name + ": " + label + " must remain ModelSpace-only")
    if "catch (Exception operationError)" not in method or "AggregateException(operationError, restoreError)" not in method:
        errors.append(path.name + ": " + label + " must preserve both operation and rollback failures")

if not AUDIT.is_file():
    errors.append("missing src/QS3D.Core/Audit/AuditTrail.cs")
else:
    audit_text = AUDIT.read_text(encoding="utf-8")
    record_start = audit_text.find("public void Record(")
    clear_start = audit_text.find("public void Clear()", record_start + 1) if record_start >= 0 else -1
    if record_start < 0 or clear_start <= record_start:
        errors.append("could not isolate AuditTrail.Record")
    else:
        record = audit_text[record_start:clear_start]
        if "_project?.Touch();" not in record:
            errors.append("AuditTrail.Record must remain the native Table project revision owner")
        if "_events.Add(item);" not in record:
            errors.append("AuditTrail.Record must continue appending the audit event")

if not SNAPSHOT_STATE.is_file():
    errors.append("missing src/QS3D.Core/Persistence/ProjectStateSnapshot.cs")
else:
    snapshot_text = SNAPSHOT_STATE.read_text(encoding="utf-8")
    for token in (
        "target.AuditEvents.Clear();",
        "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);",
    ):
        if token not in snapshot_text:
            errors.append("ProjectStateSnapshot must restore audit/revision state: " + token)

print("QS3D native table transaction-boundary preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: native table build/remove stays document-bound and rollback-safe while AuditTrail.Record owns the single project revision touch before CAD commit.")
