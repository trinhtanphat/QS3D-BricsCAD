#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "MultiRegionRebarCommands.cs"


def fail(message: str) -> int:
    print("ERROR:", message)
    return 1


def method_body(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        raise ValueError("missing method signature: " + signature)
    brace = source.find("{", start)
    if brace < 0:
        raise ValueError("missing method body: " + signature)
    depth = 0
    for index in range(brace, len(source)):
        ch = source[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[brace : index + 1]
    raise ValueError("unterminated method body: " + signature)


def require_order(body: str, tokens: list[str]) -> None:
    position = -1
    for token in tokens:
        found = body.find(token, position + 1)
        if found < 0:
            raise ValueError("missing ordered native Undo token: " + token)
        position = found


def main() -> int:
    if not COMMANDS.is_file():
        return fail("multi-region rebar command source is missing: " + str(COMMANDS.relative_to(ROOT)))

    source = COMMANDS.read_text(encoding="utf-8")
    if "QS3DUNDOSHIM" in source:
        return fail("multi-region production command must not rely on QS3DUNDOSHIM")

    try:
        slab = method_body(source, "public void BuildSlabMultiRegionRebar3D()")
        foundation = method_body(source, "public void BuildFoundationMultiRegionRebar3D()")
        bridge = method_body(source, "private static MultiRegionMeshBuildResult BuildWithNativeUndoBridge(")

        for name, body, builder in (
            ("Slab", slab, "SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab(document, project)"),
            ("Foundation", foundation, "SlabFoundationMultiRegionMeshSolidBuilder.BuildFoundation(document, project)"),
        ):
            if "BuildWithNativeUndoBridge(" not in body or builder not in body:
                raise ValueError(name + " multi-region command bypasses the native Undo bridge")

        require_order(
            bridge,
            [
                "var beforeSnapshot = ProjectStateSnapshot.Capture(project);",
                "var beforeStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
                "SourceReconcileUndoCoordinator.BeginTransition(",
                "undoTransition.StageNativeMarker();",
                "var result = build();",
                "undoTransition.StageAfter(project, ProjectStateSnapshot.Capture(project));",
                "commandTransaction.Commit();",
                "nativeCommitted = true;",
                "undoTransition.ConfirmCommitted();",
                "return result;",
            ],
        )

        if "if (!nativeCommitted)" not in bridge or "beforeSnapshot.Restore(project);" not in bridge:
            raise ValueError("pre-commit semantic rollback is not fail-closed")
        if "undoTransition?.Dispose();" not in bridge:
            raise ValueError("native Undo transition is not disposed")

        # The command-level transaction must enclose registration + the existing nested builder.
        if not re.search(
            r"using\s*\(var\s+commandTransaction\s*=\s*document\.Database\.TransactionManager\.StartTransaction\(\)\)",
            bridge,
        ):
            raise ValueError("command-level native transaction boundary is missing")
    except ValueError as exc:
        return fail(str(exc))

    print("PASS: multi-region rebar commands bind CAD + semantic state to the production native Undo/Redo bridge.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
