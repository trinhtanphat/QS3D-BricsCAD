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

    add_entity = method_block(runtime, "private static string AddEntity(")
    require(bool(add_entity), "cannot inspect AddEntity", errors)
    require(
        "private static string AddEntity(Func<Entity> entityFactory, string layer, string auditTool)" in add_entity,
        "AddEntity must accept a factory so Teigha Entity construction occurs only after CAD-context dispatch",
        errors,
    )
    if add_entity:
        dispatch = add_entity.find("return InvokeCadMutation(() =>")
        lock = add_entity.find("using (document.LockDocument())")
        transaction = add_entity.find("StartTransaction()")
        factory = add_entity.find("var entity = entityFactory();")
        require(dispatch >= 0, "AddEntity must own InvokeCadMutation dispatch", errors)
        require(lock > dispatch, "AddEntity document lock must be inside InvokeCadMutation", errors)
        require(transaction > lock, "transaction must start under the document lock", errors)
        require(factory > transaction, "Entity factory must execute inside CAD context and the active document transaction", errors)

    expected_factories = {
        "CreateLine": "return AddEntity(() => new Line(",
        "CreateCircle": "return AddEntity(() => new Circle(",
        "CreateArc": "return AddEntity(() => new Arc(",
        "CreateText": "return AddEntity(() => new DBText",
    }
    for method, token in expected_factories.items():
        block = method_block(runtime, f"private static string {method}(")
        require(bool(block), f"cannot inspect {method}", errors)
        require(token in block, f"{method} must defer Teigha Entity construction through AddEntity factory", errors)

    # Entity.Layer-by-name appeared directly at the top of the recorded native AccessViolation
    # stack. Resolve/create the layer in the same transaction and assign the ObjectId instead.
    require("entity.Layer = layer;" not in runtime, "runtime still assigns Entity.Layer by name", errors)
    ensure_layer = method_block(runtime, "private static void EnsureLayer(")
    require(bool(ensure_layer), "cannot inspect EnsureLayer", errors)
    require(
        "entity.LayerId = EnsureLayerRecord(transaction, database, layer);" in ensure_layer,
        "EnsureLayer must assign the transaction-resolved LayerId",
        errors,
    )

    # The process-global writer gate remains required. This guard is additive: it protects native
    # object affinity/lifetime in the crash-proven McpCadAgentRuntime path.
    require("McpCadMutationCoordinator.EnterMutation" in runtime, "process-global mutation coordinator was removed", errors)
    require("private static string InvokeCadMutation(" in runtime, "CAD mutation dispatch helper was removed", errors)

    if errors:
        print("FAIL: MCP CAD native mutation safety guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: McpCadAgentRuntime constructs Teigha entities inside CAD-context transactions, assigns transaction-resolved LayerId values, and preserves the global writer coordinator.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
