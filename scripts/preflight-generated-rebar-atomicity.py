#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

families = {
    "column longitudinal": ("src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "beam longitudinal": ("src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "slab mesh": ("src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "structural wall mesh": ("src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "foundation mesh": ("src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "beam stirrup": ("src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "column tie": ("src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs", "CommitSemanticUpdate(project, update)"),
    "BBS shape": ("src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs", "CommitSemanticUpdate(item)"),
}

for label, (relative, semantic_call) in families.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(label + ": missing builder " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "cadCommitted = false",
        "transaction.Commit();",
        "cadCommitted = true",
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
        semantic_call,
    ):
        if token not in text:
            errors.append(label + ": missing generated replacement contract: " + token)

    semantic = text.find(semantic_call)
    touch = text.find("project.Touch()", semantic if semantic >= 0 else 0)
    commit = text.find("transaction.Commit();", semantic if semantic >= 0 else 0)
    flag = text.find("cadCommitted = true", commit if commit >= 0 else 0)
    restore = text.find("rollback.Restore(project)", flag if flag >= 0 else 0)
    if min(semantic, touch, commit, flag, restore) < 0:
        errors.append(label + ": cannot resolve semantic/touch/CAD/rollback ordering")
    elif not semantic < touch < commit < flag < restore:
        errors.append(label + ": generated ownership/revision must advance before CAD commit and rollback must remain reachable after failed pre-commit work")

    if commit >= 0 and semantic_call in text[commit + len("transaction.Commit();"):]:
        errors.append(label + ": generated semantic ownership is still mutated after CAD commit")
    if "Editor.Regen(" in text:
        errors.append(label + ": native generated-rebar builder must remain UI-free; Regen belongs to the command post-commit boundary")

commands = {
    "column longitudinal": "src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs",
    "beam longitudinal": "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs",
    "slab mesh": "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs",
    "structural wall mesh": "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs",
    "foundation mesh": "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs",
    "beam stirrup": "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs",
    "column tie": "src/QS3D.BricsCAD.V25/ColumnTieCommands.cs",
    "BBS shape": "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs",
}
for label, relative in commands.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(label + ": missing command layer " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for token in ("FinalizeUi", "Editor.Regen()", "UI sync warning: ", "TryWriteMessage"):
        if token not in text:
            errors.append(label + ": command post-commit UI isolation missing: " + token)

print("QS3D generated rebar atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: all eight generated rebar families advance ownership/revision while CAD is rollback-capable, pre-commit failures restore project state, native builders stay UI-free, and command-level UI synchronization cannot turn committed geometry into a false failure.")
