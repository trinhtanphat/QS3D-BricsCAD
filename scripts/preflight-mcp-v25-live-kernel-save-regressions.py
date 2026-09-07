#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DIRECT = ROOT / "src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs"
SAVE = ROOT / "src/QS3D.BricsCAD.V25/McpNativeCurrentDocumentSave.cs"


def block(text, start, end):
    a = text.find(start)
    if a < 0: return ""
    b = text.find(end, a + len(start))
    return text[a:] if b < 0 else text[a:b]


def require(errors, text, tokens, label):
    for token in tokens:
        if token not in text: errors.append(f"{label} missing token: {token}")


def forbid(errors, text, tokens, label):
    for token in tokens:
        if token in text: errors.append(f"{label} still contains live-regression token: {token}")


def main():
    direct = DIRECT.read_text(encoding="utf-8")
    save = SAVE.read_text(encoding="utf-8")
    errors = []
    extrude = block(direct, "private static string Extrude(", "private static string Boolean(")
    boolean = block(direct, "private static string Boolean(", "private static string Save()")
    queue = block(save, "internal void QueueInCadContext()", "internal bool DetachBestEffort()")

    require(errors, extrude, (
        "Region.CreateFromCurves(new DBObjectCollection { source })",
        "model.AppendEntity(region);",
        "transaction.AddNewlyCreatedDBObject(region, true);",
        "solid.Extrude(region, height, 0d);",
        "if (!region.IsErased) region.Erase();",
        "kernelSource=database-resident-region",
    ), "V25 closed-polyline extrusion fallback")
    forbid(errors, extrude, (
        "var profileClone = source.Clone() as Curve;",
        "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "solid.CreateExtrudedSolid(profileClone", "kernelSource=transient-region",
        "kernelSource=database-resident-profile-clone",
    ), "V25 closed-polyline extrusion fallback")

    require(errors, boolean, (
        "target.BooleanOperation(operation, operand);",
        "if (!operand.IsErased) operand.Erase();",
        "kernelTarget=database-resident; kernelOperand=database-resident",
    ), "V25 boolean fallback")
    forbid(errors, boolean, (
        "var operandClone = operand.Clone() as Solid3d;", "target.BooleanOperation(operation, operandClone);",
        "model.AppendEntity(targetWorking)", "model.AppendEntity(operandWorking)",
        "target.HandOverTo(resultClone", "kernelOperand=transient-clone",
        "kernelInputs=database-resident-working-clones",
    ), "V25 boolean fallback")

    require(errors, queue, (
        "McpCadMutationCoordinator.QueueNativeCommand(",
        "document.SendStringToExecute(\"_.QSAVE\\n\", true, false, true)",
        "AttachHandlers(document);",
    ), "V25 event-owned QSAVE queue")
    require(errors, save, (
        "ManualResetEventSlim", "CommandEnded += OnCommandEnded", "CommandCancelled += OnCommandCancelled",
        "CommandFailed += OnCommandFailed", "Done.Wait(", "TerminalError", "EnsureCommandContextAutomationNotStopped();",
    ), "QSAVE event terminal ownership")
    forbid(errors, save, (
        "Application.DocumentManager.ExecuteInCommandContextAsync(", "TaskCompletionSource",
        "document.Editor.Command(\"_.QSAVE\")",
    ), "QSAVE command-context regression")

    if errors:
        print("ERROR: live V25 MCP kernel/save regression preflight failed closed:")
        for error in errors: print(" -", error)
        return 1
    print("PASS: live V25 closed-polyline extrusion and booleans use database-resident inputs, and QSAVE owns terminal completion through native command events.")
    return 0


if __name__ == "__main__": sys.exit(main())
