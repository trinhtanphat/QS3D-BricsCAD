#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
RUNTIME = SRC / "McpCadAgentRuntime.cs"
DIRECT = SRC / "McpCadDirectModelRuntime.cs"
SERVER = SRC / "McpEmbeddedServerV2.cs"


def between(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        return ""
    end_index = text.find(end, start_index + len(start))
    if end_index < 0:
        return text[start_index:]
    return text[start_index:end_index]


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

    run_block = between(runtime, "private static string RunCadCommandSequence", "private static string RunQs3dCommand")
    save_block = between(runtime, "private static string SaveActiveDocument", "private static string RunQs3dCommand")
    active_document_block = between(runtime, "private static string BuildActiveDocumentJson", "private static string BuildSelectionJson")
    call_block = between(runtime, "public static string Call", "private static string Mutation")

    require(errors, run_block, (
        'if (command == "QSAVE") return SaveActiveDocument(document);',
        'var inputs = NormalizeCommandInputs(',
    ), "QSAVE command route")

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
    ), "synchronous QSAVE completion guard")
    if "SendStringToExecute" in save_block:
        errors.append("QSAVE completion path must not queue a native command with SendStringToExecute")

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
    ), "direct CAD runtime")
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

    print("PASS: MCP direct 3D/save tools are published through the embedded server, routed through the canonical confirmation/mutation epoch, preserve bounded EXTRUDE grammar, and keep QSAVE/SaveAs completion guards.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
