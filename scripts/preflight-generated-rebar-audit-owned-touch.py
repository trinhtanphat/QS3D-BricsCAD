#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"

BUILDERS = {
    "BeamRebarSolidBuilder.cs": "geometry.rebar.beam",
    "BeamStirrupSolidBuilder.cs": "geometry.rebar.beam.stirrup",
    "ColumnRebarSolidBuilder.cs": "geometry.rebar.column",
    "ColumnTieSolidBuilder.cs": "geometry.rebar.column.tie",
    "ShapeRebarSolidBuilder.cs": "geometry.rebar.shape",
    "SlabMeshSolidBuilder.cs": "geometry.rebar.slab.mesh",
    "FoundationMeshSolidBuilder.cs": "geometry.rebar.foundation.mesh",
    "StructuralWallMeshSolidBuilder.cs": "geometry.rebar.wall.mesh",
}

errors = []

for filename, audit_action in BUILDERS.items():
    path = CAD / filename
    if not path.is_file():
        errors.append(f"missing {filename}")
        continue

    text = path.read_text(encoding="utf-8")
    build_start = text.find("BuildSelected(")
    method_def = text.find("private static void CommitSemanticUpdate", build_start)
    if build_start < 0 or method_def < 0:
        errors.append(f"{filename}: unable to isolate BuildSelected/CommitSemanticUpdate")
        continue

    build_body = text[build_start:method_def]
    audit_body = text[method_def:]

    if "ProjectStateSnapshot.Capture(project)" not in build_body:
        errors.append(f"{filename}: missing rollback snapshot")
    if "document.Database.TransactionManager.StartTransaction()" not in build_body:
        errors.append(f"{filename}: missing native transaction")

    invocation = build_body.find("CommitSemanticUpdate(project,")
    tx_commit = build_body.find("transaction.Commit();")
    if invocation < 0 or tx_commit < 0 or invocation > tx_commit:
        errors.append(f"{filename}: audited semantic update must precede CAD commit")

    if "project.Touch();" in build_body:
        errors.append(f"{filename}: BuildSelected must not explicitly Touch; AuditTrail owns revision advancement")

    audit = audit_body.find("AuditTrail.ForProject(project).Record(")
    action = audit_body.find(f'"{audit_action}"', audit)
    if audit < 0 or action < audit:
        errors.append(f"{filename}: missing expected AuditTrail action {audit_action}")

    rollback_restore = build_body.find("rollback.Restore(project)")
    if rollback_restore < 0:
        errors.append(f"{filename}: missing semantic rollback restore")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: all eight generated-rebar builders keep audited semantic updates before CAD commit.")
print("PASS: generated-rebar BuildSelected revision advancement is audit-owned; no redundant project.Touch().")
print("PASS: rollback/native transaction boundaries remain present across the generated-rebar batch.")
