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
    next_method = source.find("\n        private static ", start + len(signature))
    return source[start:] if next_method < 0 else source[start:next_method]


def require(errors: list[str], text: str, tokens: tuple[str, ...], label: str) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing token: {token}")


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
    direct_route_block = method_block(direct, "internal static bool CanHandleCadCommandSequence")
    direct_command_block = method_block(direct, "internal static string CallCadCommandSequence")
    direct_qsave_block = method_block(direct, "private static string SaveCadCommandSequence")
    extrude_block = method_block(direct, "private static string Extrude")
    boolean_block = method_block(direct, "private static string Boolean")
    direct_save_block = method_block(direct, "private static string Save()")
    direct_save_as_block = method_block(direct, "private static string SaveAs")
    dbmod_block = method_block(direct, "private static int WaitForSavedContentDbmod")

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
    if 'SafeInteger(SafeSystemVariable("DBMOD")) != "0"' in active_document_block:
        errors.append("cad_active_document still requires exact-zero DBMOD instead of persistent-content semantics")
    if 'string.IsNullOrWhiteSpace(filename) ? "false" : "true"' in active_document_block:
        errors.append("cad_active_document.saved still aliases filename presence instead of DBMOD dirty state")
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

    require(errors, direct, (
        '"cad_create_box"',
        '"cad_extrude"',
        '"cad_boolean_union"',
        '"cad_boolean_subtract"',
        '"cad_boolean_intersect"',
        '"cad_save"',
        '"cad_save_as"',
        'NormalizeExtrudeInputs',
        'EnsureWritableDirectory',
        'McpCadAgentRuntime.EnsureCurrentMutationRunning();',
        'string.Equals(command, "QSAVE", StringComparison.Ordinal)',
    ), "direct CAD runtime")

    # QSAVE must leave the single CAD-context callback before waiting for the host command.
    require(errors, direct_command_block, (
        'if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return SaveCadCommandSequence();',
    ), "direct QSAVE route")
    require(errors, direct_qsave_block, (
        'Save();',
        '\\"command\\":\\"QSAVE\\"',
    ), "bounded QSAVE wrapper")
    if 'McpDiagnosticHub.InvokeInCadContext' in direct_qsave_block:
        errors.append("bounded QSAVE wrapper must await native QSAVE outside a CAD-context callback")
    if 'McpCadMutationCoordinator.QueueNativeCommand' in direct_qsave_block:
        errors.append("bounded QSAVE wrapper must share McpNativeCurrentDocumentSave instead of owning a second native bridge")

    # Direct extrusion must detach the source curve before entering the BricsCAD solid kernel.
    # Region.CreateFromCurves is deliberately forbidden: the licensed-runtime regression occurs
    # on that conversion path even for a profile accepted by native EXTRUDE.
    require(errors, extrude_block, (
        'OpenEntity(transaction, document.Database, handle, OpenMode.ForRead) as Curve',
        'var sourceClone = source.Clone() as Curve;',
        'solid.CreateExtrudedSolid(sourceClone, new Vector3d(0d, 0d, height), new SweepOptions());',
        'sourceClone.Dispose();',
        'kernelSource=transient-curve-clone',
    ), "transient direct curve extrusion")
    if 'Region.CreateFromCurves' in extrude_block:
        errors.append("cad_extrude must not route the profile through Region.CreateFromCurves")
    if 'solid.CreateExtrudedSolid(source,' in extrude_block:
        errors.append("cad_extrude must not feed the database-resident Curve directly to CreateExtrudedSolid")

    # Both boolean inputs must be detached from the database while the kernel evaluates. After a
    # successful transient operation the result takes over the original target identity, preserving
    # the target ObjectId/handle; only then may the original tool entity be consumed.
    require(errors, boolean_block, (
        'var targetClone = target.Clone() as Solid3d;',
        'var operandClone = operand.Clone() as Solid3d;',
        'targetClone.BooleanOperation(operation, operandClone);',
        'target.HandOverTo(targetClone, true, true);',
        'handedOver = true;',
        'if (!operand.IsErased) operand.Erase();',
        'operandClone.Dispose();',
        'if (!handedOver) targetClone.Dispose();',
        'target=transient-clone; operand=transient-clone; result=handed-over',
    ), "detached boolean kernel inputs with target identity handover")
    if 'target.BooleanOperation(operation' in boolean_block:
        errors.append("direct boolean must not execute the kernel against the database-resident target Solid3d")
    kernel_at = boolean_block.find('targetClone.BooleanOperation(operation, operandClone);')
    handover_at = boolean_block.find('target.HandOverTo(targetClone, true, true);')
    erase_at = boolean_block.find('if (!operand.IsErased) operand.Erase();')
    if kernel_at < 0 or handover_at < 0 or erase_at < 0 or not (kernel_at < handover_at < erase_at):
        errors.append("direct boolean ordering must be transient kernel success -> target identity handover -> tool erase")

    # Current-document save is host-owned native QSAVE. The helper queues one attempt, observes
    # terminal events and waits for persistent DBMOD bits to settle before reporting success.
    require(errors, direct_save_block, (
        'McpNativeCurrentDocumentSave.SaveCurrentDocument(',
        'dbmodAfterSave',
        'route\\\":\\\"native-QSAVE-current-document',
    ), "current-document save regression guard")
    for forbidden in ('document.Database.Save();', 'document.Database.SaveAs('):
        if forbidden in direct_save_block:
            errors.append("direct cad_save must not write the already-open active drawing through " + forbidden)

    require(errors, native_save, (
        'SaveCurrentDocument',
        'McpCadMutationCoordinator.QueueNativeCommand',
        'document.SendStringToExecute(',
        '_.QSAVE',
        'ManualResetEventSlim',
        'CommandEnded',
        'CommandCancelled',
        'CommandFailed',
        'WaitForCleanDbmod',
        'DbmodPersistentContentMask = 1 | 4 | 32',
        'Application.GetSystemVariable("DBMOD")',
        '(dbmod & DbmodPersistentContentMask) == 0',
        'Do not retry automatically',
    ), "native current-document save lifecycle")
    if native_save.count('document.SendStringToExecute(') != 1:
        errors.append("native current-document save lifecycle must queue exactly one native command attempt")
    if 'Database.Save();' in native_save or 'Database.SaveAs(' in native_save:
        errors.append("native current-document save helper must never write the active path through Database.Save/SaveAs")

    require(errors, direct_save_as_block, (
        'EnsureWritableDirectory(directory);',
        'document.Database.SaveAs(fullPath, DwgVersion.Current);',
        'WaitForSavedContentDbmod();',
        'dbmodAfterSave=',
        'Path.GetFullPath(actual), fullPath',
    ), "save-as publication guard")
    if 'document.Database.Save();' in direct_save_as_block:
        errors.append("direct cad_save_as must not use Database.Save()")

    require(errors, dbmod_block, (
        'DateTime.UtcNow.AddSeconds(2)',
        'Application.GetSystemVariable("DBMOD")',
        '(dbmod & DbmodPersistentContentMask) == 0',
        'Thread.Sleep(25)',
        'window/view DBMOD bits may remain after save',
    ), "bounded SaveAs persistent-content DBMOD completion wait")
    require(errors, direct, (
        'private const int DbmodPersistentContentMask = 1 | 4 | 32;',
    ), "persistent-content DBMOD mask")
    if 'dbmod == 0' in dbmod_block:
        errors.append("save completion must not require the entire DBMOD bitmask to become zero")
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

    print("PASS: MCP direct 3D/save tools keep QSAVE owned by the bounded direct CAD runtime, extrude from a transient Curve clone without Region conversion, evaluate boolean kernels on detached target/tool clones then hand the result back to the original target identity, preserve bounded mutation routing, save the active drawing through one host-owned native QSAVE lifecycle, and confirm current-save/SaveAs completion from persistent-content DBMOD bits while allowing residual window/view state.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
