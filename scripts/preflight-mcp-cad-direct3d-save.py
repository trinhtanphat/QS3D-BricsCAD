#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"


def between(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        return ""
    end_index = text.find(end, start_index + len(start))
    if end_index < 0:
        return text[start_index:]
    return text[start_index:end_index]


def main() -> int:
    if not RUNTIME.is_file():
        print("ERROR: missing", RUNTIME.relative_to(ROOT))
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    errors: list[str] = []

    run_block = between(runtime, "private static string RunCadCommandSequence", "private static string RunQs3dCommand")
    save_block = between(runtime, "private static string SaveActiveDocument", "private static string RunQs3dCommand")
    active_document_block = between(runtime, "private static string BuildActiveDocumentJson", "private static string BuildSelectionJson")

    required_run_tokens = (
        'if (command == "QSAVE") return SaveActiveDocument(document);',
        'var inputs = NormalizeCommandInputs(',
    )
    for token in required_run_tokens:
        if token not in run_block:
            errors.append(f"QSAVE command route missing token: {token}")

    required_save_tokens = (
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
    )
    for token in required_save_tokens:
        if token not in save_block:
            errors.append(f"synchronous QSAVE completion guard missing token: {token}")

    if "SendStringToExecute" in save_block:
        errors.append("QSAVE completion path must not queue a native command with SendStringToExecute")

    required_status_tokens = (
        'var hasLocalPath = Path.IsPathRooted(filename);',
        'var modified = SafeInteger(SafeSystemVariable("DBMOD")) != "0";',
        'hasLocalPath && !modified',
        '\\"modified\\"',
    )
    for token in required_status_tokens:
        if token not in active_document_block:
            errors.append(f"active-document saved/dirty truth missing token: {token}")

    stale_saved_heuristic = 'string.IsNullOrWhiteSpace(filename) ? "false" : "true"'
    if stale_saved_heuristic in active_document_block:
        errors.append("cad_active_document.saved still aliases filename presence instead of DBMOD dirty state")

    if errors:
        print("ERROR: MCP CAD save preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP QSAVE uses synchronous Database.Save with idle/path/DBMOD completion checks, and active-document saved state is DBMOD-aware.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
