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
    queue_start = native_save.find("internal void QueueInCadContext()")
    queue_end = native_save.find("internal bool DetachBestEffort()", queue_start)
    queue = native_save[queue_start:queue_end] if queue_start >= 0 and queue_end > queue_start else ""

    require(errors, extrude, (
        "Region.CreateFromCurves(new DBObjectCollection { source })",
        "model.AppendEntity(region);", "transaction.AddNewlyCreatedDBObject(region, true);",
        "solid.Extrude(region, height, 0d);", "if (!region.IsErased) region.Erase();",
        "kernelSource=database-resident-region",
    ), "V25 database-resident-region extrusion profile")
    forbid(errors, extrude, (
        "var profileClone = source.Clone() as Curve;",
        "Region.CreateFromCurves(new DBObjectCollection { profileClone })",
        "solid.CreateExtrudedSolid(profileClone", "kernelSource=transient-region",
        "kernelSource=database-resident-profile-clone", "kernelSource=transient-curve-clone",
    ), "licensed closed-polyline extrusion regression")

    require(errors, boolean, (
        "target.BooleanOperation(operation, operand);",
        "if (!operand.IsErased) operand.Erase();",
        "kernelTarget=database-resident; kernelOperand=database-resident",
    ), "V25 boolean resident target/resident operand topology")
    forbid(errors, boolean, (
        "var operandClone = operand.Clone() as Solid3d;", "target.BooleanOperation(operation, operandClone);",
        "model.AppendEntity(targetWorking);", "model.AppendEntity(operandWorking);",
        "targetWorking.BooleanOperation(operation, operandWorking);", "target.HandOverTo(resultClone",
        "kernelOperand=transient-clone", "kernelInputs=database-resident-working-clones",
    ), "licensed boolean eInvalidInput regression")

    require(errors, native_save, (
        "ManualResetEventSlim", "McpCadMutationCoordinator.QueueNativeCommand(",
        "document.SendStringToExecute(\"_.QSAVE\\n\", true, false, true)",
        "CommandEnded += OnCommandEnded", "CommandCancelled += OnCommandCancelled", "CommandFailed += OnCommandFailed",
        "EnsureCommandContextAutomationNotStopped();", "Done.Wait(", "TerminalError",
        "WaitForCleanDbmod", "Do not retry automatically", "DbmodPersistentContentMask = 1 | 4 | 32",
    ), "V25 event-owned native QSAVE")
    forbid(errors, native_save, (
        "Application.DocumentManager.ExecuteInCommandContextAsync(", "TaskCompletionSource",
        "document.Editor.Command(\"_.QSAVE\")", "Database.Save();", "Database.SaveAs(",
    ), "command-context QSAVE regression")
    if not queue:
        errors.append("QSAVE queue block is missing")
    if native_save.count('document.SendStringToExecute("_.QSAVE\\n", true, false, true)') != 1:
        errors.append("current-document QSAVE must have exactly one queued command attempt")

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
    print("PASS: V25 kernels use database-resident Region/operand topology and QSAVE uses event-owned terminal completion without mutation-lease escape.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
