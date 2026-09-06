#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DIRECT = ROOT / "src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs"
SAVE = ROOT / "src/QS3D.BricsCAD.V25/McpNativeCurrentDocumentSave.cs"


def block(text: str, start: str, end: str) -> str:
    a = text.find(start)
    if a < 0:
        return ""
    b = text.find(end, a + len(start))
    return text[a:] if b < 0 else text[a:b]


def require(errors, text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def forbid(errors, text, tokens, label):
    for token in tokens:
        if token in text:
            errors.append(f"{label} still contains live-regression token: {token}")


def main() -> int:
    direct = DIRECT.read_text(encoding="utf-8")
    save = SAVE.read_text(encoding="utf-8")
    errors = []

    extrude = block(direct, "private static string Extrude(", "private static string Boolean(")
    boolean = block(direct, "private static string Boolean(", "private static string Save()")
    execute_qsave = block(save, "private Task ExecuteQsaveInCommandContext()", "internal void EnsureSameActiveDocumentAndPath()")
    schedule = block(save, "internal void ScheduleInCadContext()", "private Task ExecuteQsaveInCommandContext()")

    require(errors, extrude, (
        "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "solid.Extrude(region, height, 0d);",
        "kernelSource=transient-region",
    ), "V25 Circle extrusion fallback")
    forbid(errors, extrude, (
        "solid.CreateExtrudedSolid(profileClone",
        "model.AppendEntity(profileClone)",
        "kernelSource=database-resident-profile-clone",
    ), "V25 Circle extrusion fallback")

    require(errors, boolean, (
        "var operandClone = operand.Clone() as Solid3d;",
        "target.BooleanOperation(operation, operandClone);",
        "if (!operand.IsErased) operand.Erase();",
        "kernelTarget=database-resident; kernelOperand=transient-clone",
    ), "V25 boolean fallback")
    forbid(errors, boolean, (
        "model.AppendEntity(targetWorking)",
        "model.AppendEntity(operandWorking)",
        "target.HandOverTo(resultClone",
        "kernelInputs=database-resident-working-clones",
    ), "V25 boolean fallback")

    require(errors, schedule, (
        "Completion = Application.DocumentManager.ExecuteInCommandContextAsync(",
        "_ => ExecuteQsaveInCommandContext(),",
    ), "single-owner QSAVE task")
    forbid(errors, schedule, (
        "TaskCompletionSource",
        "TrySetException",
        "TrySetResult",
    ), "single-owner QSAVE task")
    require(errors, execute_qsave, (
        "EnsureCommandContextAutomationNotStopped();",
        "document.Editor.Command(\"_.QSAVE\");",
    ), "QSAVE command-context stop boundary")
    forbid(errors, execute_qsave, ("_ensureRunning();",), "QSAVE command-context mutation lease")

    if errors:
        print("ERROR: live V25 MCP kernel/save regression preflight failed closed:")
        for error in errors:
            print(" -", error)
        return 1
    print("PASS: live V25 MCP Circle extrusion avoids failing CreateExtrudedSolid path, booleans avoid DB-resident working-clone handover, and QSAVE owns one observed host task without re-entering the mutation lease from command context.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
