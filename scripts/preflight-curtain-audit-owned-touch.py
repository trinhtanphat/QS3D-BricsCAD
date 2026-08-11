#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"

BUILDERS = {
    "CurtainWallFrameSolidBuilder.cs": ("BuildSelectedLineWalls(", "CommitSemanticUpdate(project,", "geometry.curtain.frames"),
    "CurtainWallPanelSolidBuilder.cs": ("BuildSelectedLineWalls(", "Commit(project,", "geometry.curtain.panels"),
    "CurtainWallPathFrameSolidBuilder.cs": ("BuildSelectedOpenPolylines(", "CommitSemanticUpdate(project,", "geometry.curtain.path.frames"),
    "CurtainWallPathPanelSolidBuilder.cs": ("BuildSelectedOpenPolylines(", "Commit(project,", "geometry.curtain.path.panels"),
}

errors = []

for filename, (build_marker, commit_marker, audit_action) in BUILDERS.items():
    path = CAD / filename
    if not path.is_file():
        errors.append(f"missing {filename}")
        continue

    text = path.read_text(encoding="utf-8")
    build_start = text.find(build_marker)
    audit = text.find("AuditTrail.ForProject(project).Record(")
    if build_start < 0 or audit < 0:
        errors.append(f"{filename}: missing Build/AuditTrail lifecycle")
        continue

    # Isolate the public build lifecycle before its private semantic commit helper.
    helper_markers = [
        text.find("private static void CommitSemanticUpdate", build_start),
        text.find("private static void Commit(", build_start),
    ]
    helper_positions = [x for x in helper_markers if x >= 0]
    if not helper_positions:
        errors.append(f"{filename}: unable to isolate semantic commit helper")
        continue
    helper_start = min(helper_positions)
    build_body = text[build_start:helper_start]

    snapshot = build_body.find("ProjectStateSnapshot.Capture(project)")
    transaction = build_body.find("document.Database.TransactionManager.StartTransaction()")
    semantic_commit = build_body.find(commit_marker)
    cad_commit = build_body.find("transaction.Commit();")
    rollback = build_body.find("rollback.Restore(project)")
    if min(snapshot, transaction, semantic_commit, cad_commit, rollback) < 0:
        errors.append(f"{filename}: missing rollback/native/audited semantic/CAD commit boundary")
    elif not (snapshot < transaction < semantic_commit < cad_commit):
        errors.append(f"{filename}: required order is snapshot -> native transaction -> audited semantic update -> CAD commit")

    if "project.Touch();" in build_body:
        errors.append(f"{filename}: Build lifecycle must not explicitly Touch; AuditTrail owns revision advancement")

    if f'"{audit_action}"' not in text[audit:]:
        errors.append(f"{filename}: missing expected AuditTrail action {audit_action}")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: all four Curtain frame/panel builders keep audited semantic updates before CAD commit.")
print("PASS: Curtain generated-output revision advancement is audit-owned with no redundant Build-level project.Touch().")
print("PASS: rollback and native transaction boundaries remain present for straight and path Curtain builds.")
