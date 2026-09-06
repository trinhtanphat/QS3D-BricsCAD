#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
RUNTIME = SRC / "McpCadAgentRuntime.cs"
DIRECT = SRC / "McpCadDirectModelRuntime.cs"
NATIVE_SAVE = SRC / "McpNativeCurrentDocumentSave.cs"
SERVER = SRC / "McpEmbeddedServerV2.cs"


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    candidates = [
        source.find(marker, start + len(signature))
        for marker in (
            "\n        private static ",
            "\n        internal static ",
            "\n        public static ",
        )
    ]
    candidates = [value for value in candidates if value >= 0]
    end = min(candidates) if candidates else len(source)
    return source[start:end]


def require(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


def forbid(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token in text:
            errors.append(f"{label} still contains forbidden token: {token}")


def main() -> int:
    missing = [path for path in (RUNTIME, DIRECT, NATIVE_SAVE, SERVER) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
    native_save = NATIVE_SAVE.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors: list[str] = []

    run_block = method_block(runtime, "private static string RunCadCommandSequence")
    active_document_block = method_block(runtime, "private static string BuildActiveDocumentJson")
    call_block = method_block(runtime, "public static string Call")
    direct_call_block = method_block(direct, "internal static string Call")
    direct_route_block = method_block(direct, "internal static bool CanHandleCadCommandSequence")
    direct_command_block = method_block(direct, "internal static string CallCadCommandSequence")
    direct_qsave_block = method_block(direct, "private static string SaveCadCommandSequence")
    extrude_block = method_block(direct, "private static string Extrude")
    boolean_block = method_block(direct, "private static string Boolean")
    direct_save_block = method_block(direct, "private static string Save()")
    direct_save_as_block = method_block(direct, "private static string SaveAs")

    require(errors, run_block, (
        'var command = NormalizeCadCommandToken(',
        'var inputs = NormalizeCommandInputs(',
    ), "legacy generic command fallback")
    if ('SaveActiveDocument(' in run_block
            and 'McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)' not in call_block):
        errors.append("legacy generic command fallback must not own QSAVE unless canonical direct command routing intercepts it first")

    require(errors, active_document_block, (
        'var hasLocalPath = Path.IsPathRooted(filename);',
        'var dbmod = ReadDbmod();',
        'var modified = (dbmod & DbmodPersistentContentMask) != 0;',
        'hasLocalPath && !modified',
        '\\"modified\\"',
    ), "active-document saved/dirty truth")
    require(errors, runtime, (
        'private const int DbmodPersistentContentMask = 1 | 4 | 32;',
        'private static int ReadDbmod()',
    ), "active-document DBMOD semantics")

    require(errors, call_block, (
        'McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)',
        'McpCadDirectModelRuntime.CallCadCommandSequence(args)',
        'if (McpCadDirectModelRuntime.IsTool(tool))',
        'return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));',
    ), "canonical mutation dispatch")
    if "internal static void EnsureCurrentMutationRunning()" not in runtime:
        errors.append("shared mutation epoch verifier is not exposed internally to the bounded direct runtime")

    require(errors, direct_route_block, (
        'string.Equals(command, "EXTRUDE", StringComparison.Ordinal)',
        'string.Equals(command, "QSAVE", StringComparison.Ordinal)',
    ), "direct command ownership")

    require(errors, direct_call_block, (
        'if (string.Equals(tool, "cad_save", StringComparison.Ordinal)) return Save();',
        'if (string.Equals(tool, "cad_save_as", StringComparison.Ordinal)) return SaveAs(body);',
        'catch (Exception ex)',
        'RecordDirectMutationFailure(tool, ex);',
    ), "direct mutation failure propagation")
    require(errors, direct, (
        'private static void RecordDirectMutationFailure(string tool, Exception ex)',
        '"cad-mutation-failed"',
        'reason=" + ex.Message',
    ), "unified direct mutation diagnostics")

    require(errors, direct_command_block, (
        'if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return SaveCadCommandSequence();',
    ), "direct QSAVE route")
    qsave_json = direct_qsave_block.replace("\\", "")
    require(errors, qsave_json, (
        'Save();',
        '"completed":true',
        '"saved":true',
        '"command":"QSAVE"',
    ), "bounded QSAVE wrapper")

    require(errors, extrude_block, (
        'OpenEntity(transaction, document.Database, handle, OpenMode.ForRead) as Curve',
        'var profileClone = source.Clone() as Curve;',
        'Region.CreateFromCurves(new DBObjectCollection { profileClone })',
        'solid.Extrude(region, height, 0d);',
        'region?.Dispose();',
        'profileClone.Dispose();',
        'kernelSource=transient-region',
    ), "V25 transient-region direct curve extrusion")
    forbid(errors, extrude_block, (
        'model.AppendEntity(profileClone);',
        'solid.CreateExtrudedSolid(profileClone',
        'solid.CreateExtrudedSolid(source,',
        'kernelSource=database-resident-profile-clone',
        'kernelSource=transient-curve-clone',
    ), "direct extrusion live regression")

    require(errors, boolean_block, (
        'var operandClone = operand.Clone() as Solid3d;',
        'target.BooleanOperation(operation, operandClone);',
        'if (!operand.IsErased) operand.Erase();',
        'operandClone.Dispose();',
        'kernelTarget=database-resident; kernelOperand=transient-clone',
    ), "V25 direct boolean target/transient operand")
    forbid(errors, boolean_block, (
        'model.AppendEntity(targetWorking);',
        'model.AppendEntity(operandWorking);',
        'targetWorking.BooleanOperation(operation, operandWorking);',
        'target.HandOverTo(resultClone',
        'kernelInputs=database-resident-working-clones',
    ), "direct boolean live regression")
    kernel_at = boolean_block.find('target.BooleanOperation(operation, operandClone);')
    erase_at = boolean_block.find('if (!operand.IsErased) operand.Erase();')
    if kernel_at < 0 or erase_at < 0 or kernel_at > erase_at:
        errors.append("direct boolean ordering must be target kernel success -> original tool erase")

    require(errors, direct_save_block, (
        'McpNativeCurrentDocumentSave.SaveCurrentDocument(',
        'dbmodAfterSave',
        'route\\\":\\\"native-QSAVE-current-document',
    ), "current-document save regression guard")
    forbid(errors, direct_save_block, ('document.Database.Save();', 'document.Database.SaveAs('), "current-document save")

    require(errors, native_save, (
        'SaveCurrentDocument',
        'Completion = Application.DocumentManager.ExecuteInCommandContextAsync(',
        '_ => ExecuteQsaveInCommandContext(),',
        'EnsureCommandContextAutomationNotStopped();',
        'document.Editor.Command("_.QSAVE");',
        'Task.WaitAny(',
        'WaitForCleanDbmod',
        'DbmodPersistentContentMask = 1 | 4 | 32',
        'Application.GetSystemVariable("DBMOD")',
        '(dbmod & DbmodPersistentContentMask) == 0',
        'Do not retry automatically',
    ), "native current-document single-owner command-context save lifecycle")
    forbid(errors, native_save, (
        'TaskCompletionSource',
        'TrySetResult',
        'TrySetException',
        'document.SendStringToExecute(',
        'McpCadMutationCoordinator.QueueNativeCommand(',
        'ManualResetEventSlim',
        'CommandEnded +=',
        'CommandCancelled +=',
        'CommandFailed +=',
        'Database.Save();',
        'Database.SaveAs(',
    ), "native current-document save regression")
    execute_qsave = method_block(native_save, "private Task ExecuteQsaveInCommandContext()")
    if '_ensureRunning();' in execute_qsave:
        errors.append("native QSAVE command callback must not re-enter the transport mutation execution lease")
    if native_save.count('document.Editor.Command("_.QSAVE");') != 1:
        errors.append("native current-document save lifecycle must execute exactly one synchronous QSAVE command attempt")

    require(errors, direct_save_as_block, (
        'EnsureWritableDirectory(directory);',
        'McpDiagnosticHub.InvokeInCadContext(() =>',
        'document.Database.SaveAs(fullPath, DwgVersion.Current);',
        'McpNativeCurrentDocumentSave.SaveCurrentDocument(',
        'route=Database.SaveAs+native-QSAVE',
        'dbmodAfterSave',
        'Path.GetFullPath(actual), fullPath',
    ), "save-as publication plus native DBMOD settle guard")
    if 'WaitForSavedContentDbmod();' in direct_save_as_block:
        errors.append("cad_save_as must not treat Database.SaveAs return plus a blind DBMOD poll as terminal completion")

    if "Process.Start" in direct or "cmd.exe" in direct or "powershell" in direct.lower():
        errors.append("direct CAD runtime must not introduce process/shell execution")

    require(errors, server, (
        'foreach (var descriptor in McpCadDirectModelRuntime.ToolDescriptors())',
        'tools.Add(WithToolAnnotations(descriptor));',
        'var runtimeResult = McpCadAgentRuntime.Call(tool, arguments);',
    ), "embedded server direct-tool exposure")

    if errors:
        print("ERROR: MCP CAD direct 3D/save preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP direct 3D/save tools use V25-safe transient Region/operand kernel boundaries, a single observed synchronous command-context QSAVE, SaveAs native-QSAVE settle, and unified mutation failure diagnostics.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
