#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
RUNTIME = SRC / "McpCadAgentRuntime.cs"
DIRECT = SRC / "McpCadDirectModelRuntime.cs"
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
    missing = [path for path in (RUNTIME, DIRECT, SERVER) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors: list[str] = []

    run_block = method_block(runtime, "private static string RunCadCommandSequence")
    save_block = method_block(runtime, "private static string SaveActiveDocument")
    active_document_block = method_block(runtime, "private static string BuildActiveDocumentJson")
    call_block = method_block(runtime, "public static string Call")
    direct_command_block = method_block(direct, "internal static string CallCadCommandSequence")
    extrude_block = method_block(direct, "private static string Extrude")
    boolean_block = method_block(direct, "private static string Boolean")
    direct_save_block = method_block(direct, "private static string Save()")
    dbmod_block = method_block(direct, "private static void WaitForCleanDbmod")

    require(errors, run_block, (
        'if (command == "QSAVE") return SaveActiveDocument(document);',
        'var inputs = NormalizeCommandInputs(',
    ), "legacy QSAVE fallback")

    require(errors, save_block, (
        'Path.IsPathRooted(filename)',
        'SafeSystemVariable("CMDACTIVE")',
        'EnsureCurrentMutationRunning();',
        'using (document.LockDocument())',
        'document.Database.Save();',
        'SafeSystemVariable("DBMOD")',
        'dbmod != "0"',
        '\\"completed\\":true',
        '\\"saved\\":true',
        'completed=true',
    ), "legacy synchronous QSAVE fallback guard")
    if "SendStringToExecute" in save_block:
        errors.append("legacy QSAVE fallback must not queue a native command with SendStringToExecute")

    require(errors, active_document_block, (
        'var hasLocalPath = Path.IsPathRooted(filename);',
        'var modified = SafeInteger(SafeSystemVariable("DBMOD")) != "0";',
        'hasLocalPath && !modified',
        '\\"modified\\"',
    ), "active-document saved/dirty truth")
    if 'string.IsNullOrWhiteSpace(filename) ? "false" : "true"' in active_document_block:
        errors.append("cad_active_document.saved still aliases filename presence instead of DBMOD dirty state")

    require(errors, call_block, (
        'McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)',
        'McpCadDirectModelRuntime.CallCadCommandSequence(args)',
        'if (McpCadDirectModelRuntime.IsTool(tool))',
        'return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));',
    ), "canonical mutation dispatch")
    if "internal static void EnsureCurrentMutationRunning()" not in runtime:
        errors.append("shared mutation epoch verifier is not exposed internally to the bounded direct runtime")

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
    require(errors, direct_command_block, (
        'McpDiagnosticHub.InvokeInCadContext(() =>',
        'Save();',
        '\\"command\\":\\"QSAVE\\"',
    ), "direct QSAVE route")

    require(errors, extrude_block, (
        'OpenEntity(transaction, document.Database, handle, OpenMode.ForRead) as Curve',
        'Region.CreateFromCurves(new DBObjectCollection { source })',
        'regionSource=database-resident',
    ), "database-resident closed-curve extrusion")
    if 'source.Clone()' in extrude_block or 'new DBObjectCollection { clone }' in extrude_block:
        errors.append("cad_extrude must not feed a transient Curve clone to Region.CreateFromCurves")

    require(errors, boolean_block, (
        'var operandClone = operand.Clone() as Solid3d;',
        'target.BooleanOperation(operation, operandClone);',
        'if (!operand.IsErased) operand.Erase();',
        'operandClone.Dispose();',
        'operand=transient-clone',
    ), "transient boolean kernel operand")
    if 'target.BooleanOperation(operation, operand);' in boolean_block:
        errors.append("direct boolean must not pass the database-resident tool Solid3d directly to BooleanOperation")

    require(errors, direct_save_block, (
        'document.Database.SaveAs(filename, DwgVersion.Current);',
        'WaitForCleanDbmod();',
        'document.Database.SaveAs(fullPath, DwgVersion.Current);',
        'route=SaveAs-current-path',
    ), "save/reopen regression guard")
    if 'document.Database.Save();' in direct_save_block:
        errors.append("direct cad_save must not use Database.Save(), which regressed after close/reopen with eCantOpenFile")
    require(errors, dbmod_block, (
        'DateTime.UtcNow.AddSeconds(2)',
        'Application.GetSystemVariable("DBMOD")',
        'Thread.Sleep(25)',
        'dbmod == 0',
    ), "bounded DBMOD completion wait")
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

    print("PASS: MCP direct 3D/save tools use database-resident curve inputs for Region creation, transient clones for boolean kernel operands, preserve bounded mutation routing, intercept QSAVE in CAD context, avoid Database.Save after reopen, and confirm save completion with a bounded DBMOD settle wait.")
    return 0


if __name__ == "__main__":
    sys.exit(main())