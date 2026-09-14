#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticScheduleNativeTableBuilder.cs"
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
PROJECT_STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
errors = []


def read(path, label):
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


def has_revision_aware_audit_events(project_text):
    return all(
        token in project_text
        for token in (
            "AuditEvents = new CatalogOwnershipList<AuditEvent>(",
            "AttachAuditEvent",
            "DetachAuditEvent",
            "auditEvent.PersistenceMutationRequested += Touch",
            "auditEvent.PersistenceMutationRequested -= Touch",
        )
    )


builder = read(BUILDER, "SemanticScheduleNativeTableBuilder.cs")
audit = read(AUDIT, "AuditTrail.cs")
project_state = read(PROJECT_STATE, "ProjectState.cs")

if builder:
    for token in (
        '"BuildSemanticCustomScheduleTable"',
        '"RemoveSemanticCustomScheduleTable"',
        "table.SetSize(semanticTable.Rows.Count + 2, semanticTable.Headers.Count)",
        "ProjectStateSnapshot.Capture(project)",
        "transaction.Commit();",
    ):
        if token not in builder:
            errors.append("custom schedule native Table contract missing token: " + token)

    if "project.Touch();" in builder:
        errors.append("SemanticScheduleNativeTableBuilder must rely on the authoritative audit project revision owner.")

if audit:
    record_start = audit.find("public void Record(")
    clear_start = audit.find("public void Clear()", record_start + 1) if record_start >= 0 else -1
    if record_start < 0 or clear_start <= record_start:
        errors.append("could not isolate AuditTrail.Record")
    else:
        record = audit[record_start:clear_start]
        legacy_audit_touch = "_project?.Touch();" in record
        ownership_touch = has_revision_aware_audit_events(project_state)
        if legacy_audit_touch == ownership_touch:
            errors.append(
                "custom schedule Table audit revision contract must have exactly one project revision owner: "
                "legacy AuditTrail.Record Touch or revision-aware ProjectState.AuditEvents ownership"
            )
        if "_events.Add(item);" not in record:
            errors.append("AuditTrail.Record must continue appending the audit event.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: custom Semantic Schedule native Table Build/Remove preserve audit, rollback and header-only rendering with exactly one authoritative project revision owner.")
