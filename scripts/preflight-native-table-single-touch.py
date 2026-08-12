#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
AUDIT = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
errors = []


def read(path, label):
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


service = read(SERVICE, "ProjectOwnedNativeTableArtifactService.cs")
audit = read(AUDIT, "AuditTrail.cs")

if service:
    for token in (
        'AuditTrail.ForProject(project).Record(\n                        "documentation.table.replace"',
        'AuditTrail.ForProject(project).Record("documentation.table.remove"',
        "transaction.Commit();",
    ):
        if token not in service:
            errors.append("native Table mutation contract missing token: " + token)

    if "project.Touch();" in service:
        errors.append("ProjectOwnedNativeTableArtifactService must not explicitly Touch after audit-backed Table mutations.")

if audit:
    record_start = audit.find("public void Record(")
    clear_start = audit.find("public void Clear()", record_start + 1) if record_start >= 0 else -1
    if record_start < 0 or clear_start <= record_start:
        errors.append("could not isolate AuditTrail.Record")
    else:
        record = audit[record_start:clear_start]
        if "_project?.Touch();" not in record:
            errors.append("AuditTrail.Record must remain the single project Touch owner for this contract.")
        if "_events.Add(item);" not in record:
            errors.append("AuditTrail.Record must continue appending the audit event.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: project-owned native Table Build/Remove retain audit events and rely on AuditTrail.Record as the single ChangeVersion touch.")
