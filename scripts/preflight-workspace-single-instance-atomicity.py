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
        operation = body.find('"Workspace single-instance property edit"', executor)
        set_token = "element.SetProperty(key, next);"
        touch_token = "project.Touch();"
        set_property = body.find(set_token, executor)
        touch = body.find(touch_token, set_property)
        executor_close = body.find("});", touch)
        overflow = body.find("ex is OverflowException", executor_close)
        return_current_after_catch = body.find("return current;", overflow)

        for position, label in (
            (executor, "rollback-protected semantic mutation executor"),
            (operation, "stable Workspace single-instance operation name"),
            (set_property, "instance SetProperty mutation"),
            (touch, "project Touch mutation"),
            (executor_close, "executor closure after Touch"),
            (overflow, "OverflowException handling"),
        ):
            if position < 0:
                errors.append("ApplyInstanceProperty missing " + label)

        if body.count(set_token) != 1:
            errors.append("ApplyInstanceProperty must contain exactly one instance SetProperty mutation")
        if body.count(touch_token) != 1:
            errors.append("ApplyInstanceProperty must contain exactly one project Touch mutation")
        if min(executor, operation, set_property, touch, executor_close) >= 0 and not (
            executor < operation < set_property < touch < executor_close
        ):
            errors.append("ApplyInstanceProperty must keep SetProperty and Touch inside the ProjectSemanticMutationExecutor closure")
        if overflow >= 0 and not (executor_close < overflow < return_current_after_catch):
            errors.append("ApplyInstanceProperty must catch Touch overflow after the executor and return the previous value")

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

print("PASS: Workspace single-instance edits and resets keep SetProperty + Touch inside the rollback-protected semantic mutation boundary; Touch overflow cannot leave partial instance state.")
