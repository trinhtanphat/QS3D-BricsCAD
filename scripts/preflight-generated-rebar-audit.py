#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "column longitudinal": (
        "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs",
        'geometry.rebar.column',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "beam longitudinal": (
        "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs",
        'geometry.rebar.beam',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "shape rebar": (
        "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs",
        'geometry.rebar.shape',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "column ties": (
        "src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs",
        'geometry.rebar.column.tie',
        "BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)",
    ),
    "beam stirrups": (
        "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs",
        'geometry.rebar.beam.stirrup',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "slab mesh": (
        "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs",
        'geometry.rebar.slab.mesh',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "structural-wall mesh": (
        "src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs",
        'geometry.rebar.wall.mesh',
        "BuildSelected(Document document, ProjectState project)",
    ),
    "foundation mesh": (
        "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs",
        'geometry.rebar.foundation.mesh',
        "BuildSelected(Document document, ProjectState project)",
    ),
}

for label, (relative, event_name, build_signature) in contracts.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(f"{label}: missing {relative}")
        continue
    text = path.read_text(encoding="utf-8")
    if "using QS3D.Core.Audit;" not in text:
        errors.append(f"{label}: missing QS3D.Core.Audit import")
    audit_call_token = "AuditTrail.ForProject(project).Record("
    event_token = f'"{event_name}"'
    if audit_call_token not in text or event_token not in text:
        errors.append(f"{label}: missing canonical audit event {event_name}")

    build_start = text.find(build_signature)
    if build_start < 0:
        errors.append(f"{label}: missing canonical BuildSelected method signature: {build_signature}")
        continue
    helper_start = text.find("private static void CommitSemanticUpdate", build_start)
    if helper_start < 0:
        errors.append(f"{label}: missing CommitSemanticUpdate helper")
        continue
    build_body = text[build_start:helper_start]
    commit_index = build_body.find("transaction.Commit();")
    semantic_index = build_body.find("CommitSemanticUpdate(project,")
    if semantic_index < 0:
        errors.append(f"{label}: BuildSelected must pass project into CommitSemanticUpdate")
    if commit_index < 0:
        errors.append(f"{label}: missing CAD transaction commit")
    if semantic_index >= 0 and commit_index >= 0 and semantic_index > commit_index:
        errors.append(f"{label}: semantic/audit publish moved after CAD commit")

    next_helper = text.find("\n        private ", helper_start + 1)
    helper_body = text[helper_start: next_helper if next_helper >= 0 else len(text)]
    if "ProjectState project" not in helper_body:
        errors.append(f"{label}: CommitSemanticUpdate no longer receives project for transactional audit")
    if audit_call_token not in helper_body or event_token not in helper_body:
        errors.append(f"{label}: audit event is not owned by CommitSemanticUpdate")

shape_command = ROOT / "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs"
if not shape_command.is_file():
    errors.append("shape rebar: missing command surface")
else:
    command_text = shape_command.read_text(encoding="utf-8")
    if "geometry.rebar3d.shape" in command_text:
        errors.append("shape rebar: legacy post-commit geometry.rebar3d.shape audit must not return")
    if "AuditTrail.ForProject(" in command_text:
        errors.append("shape rebar: command/UI layer must not publish replacement audit after the builder CAD commit")

print("QS3D generated rebar/mesh audit preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: every generated rebar/mesh replacement family records its canonical audit event through the pre-CAD-commit semantic update path, with Column Tie consuming its admitted PICKFIRST snapshot and no duplicate Shape Rebar audit in the post-commit UI layer.")
