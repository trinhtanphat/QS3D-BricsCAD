#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    brace = source.find("{", start)
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(source)):
        ch = source[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[start:index + 1]
    return ""


def require(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def main() -> int:
    if not RUNTIME.is_file():
        print("FAIL: missing src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs")
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    errors: list[str] = []
    active_doc = method_block(runtime, "private static string BuildActiveDocumentJson(")
    fallback_save = method_block(runtime, "private static string SaveActiveDocument(")

    require("private const int DbmodPersistentContentMask = 1 | 4 | 32;" in runtime,
            "agent runtime must define the same persistent DBMOD content mask as direct save", errors)
    require("private static int ReadDbmod(" in runtime,
            "agent runtime must centralize fail-closed integer DBMOD parsing", errors)
    require(bool(active_doc), "cannot inspect BuildActiveDocumentJson", errors)
    require(bool(fallback_save), "cannot inspect SaveActiveDocument", errors)

    if active_doc:
        require("var dbmod = ReadDbmod();" in active_doc,
                "cad_active_document must read DBMOD through the shared content-aware parser", errors)
        require("var modified = (dbmod & DbmodPersistentContentMask) != 0;" in active_doc,
                "cad_active_document modified state must ignore window/view-only DBMOD bits", errors)
        require('SafeInteger(SafeSystemVariable("DBMOD")) != "0"' not in active_doc,
                "cad_active_document still uses exact-zero DBMOD semantics", errors)

    if fallback_save:
        require(fallback_save.count("document.Database.Save();") == 1,
                "fallback QSAVE must keep exactly one native Database.Save attempt", errors)
        require('SafeInteger(SafeSystemVariable("CMDACTIVE")) != "0"' in fallback_save,
                "fallback QSAVE command-idle gate was removed", errors)
        require("var dbmodAfterSave = ReadDbmod();" in fallback_save,
                "fallback QSAVE must inspect DBMOD after the native save", errors)
        require("(dbmodAfterSave & DbmodPersistentContentMask) != 0" in fallback_save,
                "fallback QSAVE must fail closed only when persistent content bits remain", errors)
        require('if (dbmod != "0")' not in fallback_save,
                "fallback QSAVE still requires the whole DBMOD bitmask to be zero", errors)
        require("dbmodAfterSave" in fallback_save,
                "fallback QSAVE must retain bounded post-save DBMOD diagnostics", errors)

    if errors:
        print("FAIL: MCP CAD agent DBMOD semantics guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: cad_active_document and fallback QSAVE use persistent DBMOD content bits while preserving the idle gate and one native save attempt.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
