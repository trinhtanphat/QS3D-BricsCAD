#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
path = ROOT / "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs"
command_path = ROOT / "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs"

if not path.is_file():
    errors.append("missing ShapeRebarSolidBuilder.cs")
else:
    text = path.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "ErasePrevious(document, transaction, project, element, ownership)",
        "foreach (var item in pending) CommitSemanticUpdate(project, item);",
        "if (pending.Count > 0) project.Touch();",
        "transaction.Commit();\n                    cadCommitted = true;",
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
    ):
        if token not in text:
            errors.append("Shape rebar missing atomicity contract: " + token)

    start = text.find("public static ShapeRebarBuildResult BuildSelected(Document document, ProjectState project)")
    end = text.find("private static void CommitSemanticUpdate", start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate Shape Rebar BuildSelected method")
    else:
        body = text[start:end]
        semantic_token = "foreach (var item in pending) CommitSemanticUpdate(project, item);"
        touch_token = "if (pending.Count > 0) project.Touch();"
        commit_token = "transaction.Commit();\n                    cadCommitted = true;"
        semantic = body.find(semantic_token)
        touch = body.find(touch_token, semantic if semantic >= 0 else 0)
        commit = body.find(commit_token)
        restore = body.find("rollback.Restore(project)")
        if min(semantic, touch, commit, restore) < 0:
            errors.append("Shape rebar atomicity ordering tokens are incomplete")
        elif not semantic < touch < commit < restore:
            errors.append("Shape rebar handles/count/mode/stale/revision state must commit while CAD is rollback-capable")
        if body.count(semantic_token) != 1 or body.count(commit_token) != 1:
            errors.append("Shape rebar requires exactly one semantic replacement phase and one CAD commit/flag boundary")
        if commit >= 0 and semantic_token in body[commit + len(commit_token):]:
            errors.append("Shape rebar still mutates generated semantic ownership after CAD commit")
        if "Editor.Regen(" in body:
            errors.append("Shape native builder must remain UI-free; viewport regen belongs to ShapeRebarGeometryCommands post-commit FinalizeUi")

    helper_start = text.find("private static void CommitSemanticUpdate")
    helper_end = text.find("private static Placement ResolvePlacement", helper_start + 1) if helper_start >= 0 else -1
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    for token in (
        "Properties[HandlesKey]",
        "GeneratedShapeRebarCount",
        "GeneratedShapeRebarMode",
        "ClearGeneratedShapeRebarStale()",
    ):
        if token not in helper:
            errors.append("Shape rebar semantic commit helper missing metadata contract: " + token)

if not command_path.is_file():
    errors.append("missing ShapeRebarGeometryCommands.cs")
else:
    command = command_path.read_text(encoding="utf-8")
    for token in ("FinalizeUi", "document.Editor.Regen()", "UI sync warning: ", "TryWriteMessage"):
        if token not in command:
            errors.append("Shape command post-commit UI isolation missing: " + token)

print("QS3D Shape Rebar cross-layer atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Shape Rebar replaces owned generated bars and advances handles/count/mode/stale/revision state before CAD commit with deep project rollback on pre-commit failure; native builder stays UI-free and command-level viewport/UI synchronization is non-fatal.")
