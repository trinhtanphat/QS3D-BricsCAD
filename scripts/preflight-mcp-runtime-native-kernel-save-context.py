#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
DIRECT = SRC / "McpCadDirectModelRuntime.cs"
NATIVE_SAVE = SRC / "McpNativeCurrentDocumentSave.cs"
DOMAIN = SRC / "McpQs3dDomainRuntime.cs"


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    candidates = [source.find(marker, start + len(signature)) for marker in (
        "\n        private static ", "\n        internal static ", "\n        public static ", "\n            private ", "\n            internal ")]
    candidates = [value for value in candidates if value >= 0]
    end = min(candidates) if candidates else len(source)
    return source[start:end]


def require(errors, text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def forbid(errors, text, tokens, label):
    for token in tokens:
        if token in text:
            errors.append(f"{label} still contains forbidden token: {token}")


def main() -> int:
    missing = [path for path in (DIRECT, NATIVE_SAVE, DOMAIN) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1
    direct = DIRECT.read_text(encoding="utf-8")
    native_save = NATIVE_SAVE.read_text(encoding="utf-8")
    domain = DOMAIN.read_text(encoding="utf-8")
    errors = []
    call = method_block(direct, "internal static string Call")
    extrude = method_block(direct, "private static string Extrude")
    boolean = method_block(direct, "private static string Boolean")
    save_as = method_block(direct, "private static string SaveAs")
    status = method_block(domain, "internal static string BuildStatusJson")
    schedule_start = native_save.find("internal void ScheduleInCadContext()")
    schedule_end = native_save.find("private Task ExecuteQsaveInCommandContext()", schedule_start)
    schedule = native_save[schedule_start:schedule_end] if schedule_start >= 0 and schedule_end > schedule_start else ""
    execute_qsave = method_block(native_save, "private Task ExecuteQsaveInCommandContext()")

    require(errors, extrude, (
        "var profileClone = source.Clone() as Curve;",
        "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "solid.Extrude(region, height, 0d);",
        "region?.Dispose();", "profileClone.Dispose();", "kernelSource=transient-region",
    ), "V25 transient-region extrusion profile")
    forbid(errors, extrude, (
        "model.AppendEntity(profileClone);", "solid.CreateExtrudedSolid(profileClone",
        "kernelSource=database-resident-profile-clone", "kernelSource=transient-curve-clone",
    ), "licensed Circle extrusion regression")

    require(errors, boolean, (
        "var operandClone = operand.Clone() as Solid3d;", "target.BooleanOperation(operation, operandClone);",
        "if (!operand.IsErased) operand.Erase();", "operandClone.Dispose();",
        "kernelTarget=database-resident; kernelOperand=transient-clone",
    ), "V25 boolean target/transient-operand topology")
    forbid(errors, boolean, (
        "model.AppendEntity(targetWorking);", "model.AppendEntity(operandWorking);",
        "targetWorking.BooleanOperation(operation, operandWorking);", "target.HandOverTo(resultClone",
        "kernelInputs=database-resident-working-clones",
    ), "licensed boolean eInvalidInput regression")

    require(errors, native_save, (
        "Application.DocumentManager.ExecuteInCommandContextAsync(",
        "var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);",
        "Completion = completionSource.Task;", "completionSource.TrySetResult(null);",
        "completionSource.TrySetException(ex);", "EnsureCommandContextAutomationNotStopped();",
        "document.Editor.Command(\"_.QSAVE\");", "Task.WaitAny(", "completion.GetAwaiter().GetResult();",
        "WaitForCleanDbmod", "Do not retry automatically", "DbmodPersistentContentMask = 1 | 4 | 32",
    ), "V25 ExecutionResult-compatible synchronous native QSAVE")
    forbid(errors, native_save, (
        "Completion = Application.DocumentManager.ExecuteInCommandContextAsync(",
        "document.SendStringToExecute(", "McpCadMutationCoordinator.QueueNativeCommand(",
        "ManualResetEventSlim", "CommandEnded +=", "CommandCancelled +=", "CommandFailed +=",
        "Database.Save();", "Database.SaveAs(",
    ), "queued/host-result QSAVE regression")
    if "throw;" in schedule:
        errors.append("QSAVE scheduling callback must capture callback failures into Completion without rethrowing an unobserved host fault")
    if "_ensureRunning();" in execute_qsave:
        errors.append("QSAVE command-context callback must not re-enter the transport mutation execution lease")
    if native_save.count('document.Editor.Command("_.QSAVE");') != 1:
        errors.append("current-document QSAVE must have exactly one synchronous command attempt")

    require(errors, save_as, (
        "McpDiagnosticHub.InvokeInCadContext(() =>", "document.Database.SaveAs(fullPath, DwgVersion.Current);",
        "McpNativeCurrentDocumentSave.SaveCurrentDocument(", "route=Database.SaveAs+native-QSAVE", "dbmodAfterSave",
    ), "SaveAs native completion settle")
    forbid(errors, save_as, ("WaitForSavedContentDbmod();",), "SaveAs blind DBMOD polling regression")
    require(errors, call, ("catch (Exception ex)", "RecordDirectMutationFailure(tool, ex);"), "direct mutation failure routing")
    require(errors, direct, ("private static void RecordDirectMutationFailure(string tool, Exception ex)", '"cad-mutation-failed"', 'reason=" + ex.Message'), "unified direct failure diagnostics")
    require(errors, status, ("ExistingProjectMutationContext.TryGet(document, out project)", "No persisted QS3D project context"), "persisted project-context hydration")
    forbid(errors, status, ("ProjectContextCoordinator.GetOrCreate(document)", "No cached QS3D project context"), "project-context fabrication/cold-cache regression")

    if errors:
        print("ERROR: licensed MCP runtime regression guard failed:")
        for error in errors: print(" -", error)
        return 1
    print("PASS: V25 kernels use transient-region/operand topology and QSAVE uses an observed TCS completion signal compatible with BricsCAD ExecutionResult, without callback rethrow or mutation-lease escape.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
