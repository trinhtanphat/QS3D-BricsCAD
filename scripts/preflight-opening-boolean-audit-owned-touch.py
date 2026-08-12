#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"

SERVICES = {
    "OpeningBooleanService.cs": "geometry.opening.boolean",
    "CurvedOpeningBooleanService.cs": "geometry.opening.boolean.curved",
}

errors = []

for filename, audit_action in SERVICES.items():
    path = CAD / filename
    if not path.is_file():
        errors.append(f"missing {filename}")
        continue

    text = path.read_text(encoding="utf-8")
    build_start = text.find("CutLinkedOpenings(")
    helper_start = text.find("private static void CommitSemanticUpdate", build_start)
    if build_start < 0 or helper_start < 0:
        errors.append(f"{filename}: unable to isolate CutLinkedOpenings/CommitSemanticUpdate")
        continue

    build_body = text[build_start:helper_start]
    helper_body = text[helper_start:]

    snapshot = build_body.find("ProjectStateSnapshot.Capture(project)")
    transaction = build_body.find("document.Database.TransactionManager.StartTransaction()")
    semantic_commit = build_body.find("CommitSemanticUpdate(project,")
    cad_commit = build_body.find("transaction.Commit();")
    rollback = build_body.find("rollback.Restore(project)")
    regen = build_body.find("TryRegen(document)")

    if min(snapshot, transaction, semantic_commit, cad_commit, rollback, regen) < 0:
        errors.append(f"{filename}: missing rollback/native/audited semantic/CAD commit/regen boundary")
    elif not (snapshot < transaction < semantic_commit < cad_commit < regen):
        errors.append(f"{filename}: required order is snapshot -> native transaction -> audited semantic update -> CAD commit -> best-effort regen")

    if "project.Touch();" in build_body:
        errors.append(f"{filename}: CutLinkedOpenings must not explicitly Touch; AuditTrail owns revision advancement")

    audit = helper_body.find("AuditTrail.ForProject(project).Record(")
    action = helper_body.find(f'"{audit_action}"', audit)
    target_state = helper_body.find("PhysicalOpeningCutTargetState.Write(")
    if audit < 0 or action < audit:
        errors.append(f"{filename}: missing expected AuditTrail action {audit_action}")
    if target_state < 0 or (audit >= 0 and target_state > audit):
        errors.append(f"{filename}: physical opening target-state must be written before its audit record")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: straight and curved opening booleans keep semantic target-state/audit before CAD commit.")
print("PASS: physical-opening revision advancement is audit-owned with no redundant CutLinkedOpenings-level project.Touch().")
print("PASS: rollback, native transaction and post-commit viewport regen boundaries remain present.")
