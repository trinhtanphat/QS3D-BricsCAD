#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"


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
    if not RUNTIME.is_file() or not DIRECT.is_file():
        print("FAIL: missing MCP CAD runtime source")
        return 1

    runtime = RUNTIME.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
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

    # Layer-by-name assignment has repeatedly appeared at the top of native AccessViolation
    # stacks. Resolve/create the layer in the current transaction and assign the ObjectId.
    require("entity.Layer = layer;" not in runtime, "runtime still assigns Entity.Layer by name", errors)
    ensure_layer = method_block(runtime, "private static void EnsureLayer(")
    require(bool(ensure_layer), "cannot inspect EnsureLayer", errors)
    require(
        "entity.LayerId = EnsureLayerRecord(transaction, database, layer);" in ensure_layer,
        "EnsureLayer must assign the transaction-resolved LayerId",
        errors,
    )

    apply_layer = method_block(direct, "private static void ApplyLayer(")
    require(bool(apply_layer), "cannot inspect direct ApplyLayer", errors)
    require("entity.Layer = layer;" not in apply_layer, "direct runtime still assigns Entity.Layer by name", errors)
    require("entity.LayerId = layerId;" in apply_layer, "direct ApplyLayer must assign a transaction-resolved LayerId", errors)

    # The existing process-global writer gate remains required; this guard is additive and
    # protects native object affinity rather than replacing multi-session serialization.
    require("McpCadMutationCoordinator.EnterMutation" in runtime, "process-global mutation coordinator was removed", errors)
    require("private static string InvokeCadMutation(" in runtime, "CAD mutation dispatch helper was removed", errors)
    require("McpDiagnosticHub.InvokeInCadContext" in direct, "direct model runtime lost CAD-context dispatch", errors)

    if errors:
        print("FAIL: MCP CAD native mutation safety guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: Teigha entities are constructed only inside CAD-context mutation dispatch, layer changes use transaction-resolved LayerId, and the global writer coordinator remains active.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
