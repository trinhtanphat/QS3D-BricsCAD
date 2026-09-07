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
    candidates = [source.find(marker, start + len(signature)) for marker in (
        "\n        private static ", "\n        internal static ", "\n        public static ")]
    candidates = [value for value in candidates if value >= 0]
    end = min(candidates) if candidates else len(source)
    return source[start:end]


def require(errors, text, tokens, label):
    for token in tokens:
        if token not in text: errors.append(f"{label} missing token: {token}")


def forbid(errors, text, tokens, label):
    for token in tokens:
        if token in text: errors.append(f"{label} still contains forbidden token: {token}")


def main():
    missing = [path for path in (RUNTIME, DIRECT, NATIVE_SAVE, SERVER) if not path.is_file()]
    if missing:
        for path in missing: print("ERROR: missing", path.relative_to(ROOT))
        return 1
    runtime = RUNTIME.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
    native_save = NATIVE_SAVE.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors = []

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
    queue_start = native_save.find("internal void QueueInCadContext()")
    queue_end = native_save.find("internal bool DetachBestEffort()", queue_start)
    queue = native_save[queue_start:queue_end] if queue_start >= 0 and queue_end > queue_start else ""

    require(errors, run_block, ('var command = NormalizeCadCommandToken(', 'var inputs = NormalizeCommandInputs('), "legacy generic command fallback")
    if 'SaveActiveDocument(' in run_block and 'McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)' not in call_block:
        errors.append("legacy generic command fallback must not own QSAVE unless canonical direct command routing intercepts it first")
    require(errors, active_document_block, ('var hasLocalPath = Path.IsPathRooted(filename);', 'var dbmod = ReadDbmod();', 'var modified = (dbmod & DbmodPersistentContentMask) != 0;', 'hasLocalPath && !modified', '\\"modified\\"'), "active-document saved/dirty truth")
    require(errors, runtime, ('private const int DbmodPersistentContentMask = 1 | 4 | 32;', 'private static int ReadDbmod()'), "active-document DBMOD semantics")
    require(errors, call_block, ('McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)', 'McpCadDirectModelRuntime.CallCadCommandSequence(args)', 'if (McpCadDirectModelRuntime.IsTool(tool))', 'return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));'), "canonical mutation dispatch")
    if "internal static void EnsureCurrentMutationRunning()" not in runtime:
        errors.append("shared mutation epoch verifier is not exposed internally to the bounded direct runtime")
    require(errors, direct_route_block, ('string.Equals(command, "EXTRUDE", StringComparison.Ordinal)', 'string.Equals(command, "QSAVE", StringComparison.Ordinal)'), "direct command ownership")
    require(errors, direct_call_block, ('if (string.Equals(tool, "cad_save", StringComparison.Ordinal)) return Save();', 'if (string.Equals(tool, "cad_save_as", StringComparison.Ordinal)) return SaveAs(body);', 'catch (Exception ex)', 'RecordDirectMutationFailure(tool, ex);'), "direct mutation failure propagation")
    require(errors, direct, ('private static void RecordDirectMutationFailure(string tool, Exception ex)', '"cad-mutation-failed"', 'reason=" + ex.Message'), "unified direct mutation diagnostics")
    require(errors, direct_command_block, ('if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return SaveCadCommandSequence();',), "direct QSAVE route")
    qsave_json = direct_qsave_block.replace("\\", "")
    require(errors, qsave_json, ('Save();', '"completed":true', '"saved":true', '"command":"QSAVE"'), "bounded QSAVE wrapper")

    require(errors, extrude_block, (
        'OpenEntity(transaction, document.Database, handle, OpenMode.ForRead) as Curve',
        'Region.CreateFromCurves(new DBObjectCollection { source })',
        'model.AppendEntity(region);',
        'transaction.AddNewlyCreatedDBObject(region, true);',
        'solid.Extrude(region, height, 0d);',
        'if (!region.IsErased) region.Erase();',
        'kernelSource=database-resident-region',
    ), "V25 database-resident Region direct curve extrusion")
    forbid(errors, extrude_block, (
        'var profileClone = source.Clone() as Curve;',
        'Region.CreateFromCurves(new DBObjectCollection { profileClone })',
        'solid.CreateExtrudedSolid(profileClone',
        'kernelSource=transient-region',
        'kernelSource=database-resident-profile-clone',
    ), "direct extrusion live regression")

    require(errors, boolean_block, (
        'target.BooleanOperation(operation, operand);',
        'if (!operand.IsErased) operand.Erase();',
        'kernelTarget=database-resident; kernelOperand=database-resident',
    ), "V25 direct boolean resident target/resident operand")
    forbid(errors, boolean_block, (
        'var operandClone = operand.Clone() as Solid3d;',
        'target.BooleanOperation(operation, operandClone);',
        'model.AppendEntity(targetWorking);',
        'model.AppendEntity(operandWorking);',
        'target.HandOverTo(resultClone',
        'kernelOperand=transient-clone',
        'kernelInputs=database-resident-working-clones',
    ), "direct boolean live regression")
    kernel_at = boolean_block.find('target.BooleanOperation(operation, operand);')
    erase_at = boolean_block.find('if (!operand.IsErased) operand.Erase();')
    if kernel_at < 0 or erase_at < 0 or kernel_at > erase_at:
        errors.append("direct boolean ordering must be resident target/tool kernel success -> tool erase")

    require(errors, direct_save_block, ('McpNativeCurrentDocumentSave.SaveCurrentDocument(', 'dbmodAfterSave', 'route\\\":\\\"native-QSAVE-current-document'), "current-document save regression guard")
    forbid(errors, direct_save_block, ('document.Database.Save();', 'document.Database.SaveAs('), "current-document save")
    require(errors, native_save, (
        'SaveCurrentDocument', 'ManualResetEventSlim', 'QueueNativeCommand(',
        'document.SendStringToExecute("_.QSAVE\\n", true, false, true)',
        'CommandEnded += OnCommandEnded', 'CommandCancelled += OnCommandCancelled', 'CommandFailed += OnCommandFailed',
        'EnsureCommandContextAutomationNotStopped();', 'Done.Wait(', 'TerminalError', 'WaitForCleanDbmod',
        'DbmodPersistentContentMask = 1 | 4 | 32', 'Application.GetSystemVariable("DBMOD")',
        '(dbmod & DbmodPersistentContentMask) == 0', 'Do not retry automatically'),
        "native current-document V25 event-owned save lifecycle")
    forbid(errors, native_save, (
        'Application.DocumentManager.ExecuteInCommandContextAsync(', 'TaskCompletionSource',
        'document.Editor.Command("_.QSAVE")', 'Database.Save();', 'Database.SaveAs('),
    ), "native current-document save regression")
    if not queue:
        errors.append("native QSAVE QueueInCadContext block is missing")
    if native_save.count('document.SendStringToExecute("_.QSAVE\\n", true, false, true)') != 1:
        errors.append("native current-document save lifecycle must queue exactly one QSAVE command attempt")

    require(errors, direct_save_as_block, ('EnsureWritableDirectory(directory);', 'McpDiagnosticHub.InvokeInCadContext(() =>', 'document.Database.SaveAs(fullPath, DwgVersion.Current);', 'McpNativeCurrentDocumentSave.SaveCurrentDocument(', 'route=Database.SaveAs+native-QSAVE', 'dbmodAfterSave', 'Path.GetFullPath(actual), fullPath'), "save-as publication plus native DBMOD settle guard")
    if 'WaitForSavedContentDbmod();' in direct_save_as_block:
        errors.append("cad_save_as must not treat Database.SaveAs return plus a blind DBMOD poll as terminal completion")
    if "Process.Start" in direct or "cmd.exe" in direct or "powershell" in direct.lower():
        errors.append("direct CAD runtime must not introduce process/shell execution")
    require(errors, server, ('foreach (var descriptor in McpCadDirectModelRuntime.ToolDescriptors())', 'tools.Add(WithToolAnnotations(descriptor));', 'var runtimeResult = McpCadAgentRuntime.Call(tool, arguments);'), "embedded server direct-tool exposure")

    if errors:
        print("ERROR: MCP CAD direct 3D/save preflight failed:")
        for error in errors: print(" -", error)
        return 1
    print("PASS: MCP direct 3D/save uses database-resident V25 kernel inputs and event-owned native QSAVE terminal completion.")
    return 0


if __name__ == "__main__": sys.exit(main())
