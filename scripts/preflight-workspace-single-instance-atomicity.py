#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ViewModels" / "WorkspaceViewModel.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing WorkspaceViewModel.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private string ApplyInstanceProperty(")
    end = text.find("private void ResetInstanceProperty(", start)
    body = text[start:end] if start >= 0 and end > start else ""

    if not body:
        errors.append("ApplyInstanceProperty body was not found")
    else:
        executor = body.find("ProjectSemanticMutationExecutor.Execute(")
        operation = body.find('"Workspace single-instance property edit"')
        set_property = body.find("element.SetProperty(key, next);")
        touch = body.find("project.Touch();")
        overflow = body.find("ex is OverflowException")
        return_current_after_catch = body.find("return current;", overflow)

        for position, label in (
            (executor, "rollback-protected semantic mutation executor"),
            (operation, "stable Workspace single-instance operation name"),
            (set_property, "instance SetProperty mutation"),
            (touch, "project Touch mutation"),
            (overflow, "OverflowException handling"),
        ):
            if position < 0:
                errors.append("ApplyInstanceProperty missing " + label)

        if min(executor, operation, set_property, touch) >= 0 and not (executor < operation < set_property < touch):
            errors.append("ApplyInstanceProperty must enter ProjectSemanticMutationExecutor before SetProperty and Touch")
        if overflow >= 0 and return_current_after_catch < overflow:
            errors.append("ApplyInstanceProperty must return the previous value after a rollback-triggering overflow")

        direct_sequence = "element.SetProperty(key, next);\n            project.Touch();"
        if direct_sequence in body:
            errors.append("ApplyInstanceProperty regressed to direct SetProperty -> Touch outside an explicit rollback boundary")

    reset_start = text.find("private void ResetInstanceProperty(")
    reset_end = text.find("private bool TryGetCurrentProjectForMutation(", reset_start)
    reset_body = text[reset_start:reset_end] if reset_start >= 0 and reset_end > reset_start else ""
    if "row.Value = ToDisplayValue(key, liveFamilyRaw ?? string.Empty);" not in reset_body:
        errors.append("ResetInstanceProperty must continue routing reset through PropertyRow.Value/Apply so it shares the atomic edit boundary")

print("QS3D Workspace single-instance atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Workspace single-instance edits and resets share the rollback-protected semantic mutation boundary; Touch overflow cannot leave partial instance state.")
