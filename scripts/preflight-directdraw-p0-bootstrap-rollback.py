#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawCommands.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        position = text.find(token, cursor + 1)
        if position < 0:
            raise AssertionError(label + " missing ordered token: " + token)
        if position <= cursor:
            raise AssertionError(label + " has invalid token ordering: " + token)
        cursor = position


def main():
    text = SOURCE.read_text(encoding="utf-8")
    execute = method_slice(
        text,
        "private static void ExecuteDirect(",
        "private static int BuildSelected(")

    require_order(
        execute,
        "P0 Direct Draw bootstrap ownership",
        "EnsureActive(document, operation);",
        "var projectExistedBeforeAuthoring = projectPreview != null",
        "? projectPreview.HasProject",
        ": ProjectContextCoordinator.TryGetReadOnly(document, out _);",
        "projectPreview.ResolveForMutation(document, operation)",
        "ProjectStateSnapshot.Capture(project);")

    require_order(
        execute,
        "P0 Direct Draw failed bootstrap cleanup",
        "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles);",
        "rollback.Restore(project);",
        "if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);",
        "document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());")

    require(execute, "if (ownershipDiscoveryError != null || cadCleanupError != null || restoreError != null)", "rollback error aggregation")
    require(execute, "new AggregateException(errors)", "rollback aggregate preservation")

    cleanup_index = execute.find("if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);")
    success_index = execute.find("FinalizeUi(document, createdElement!, sourceId, solids, regenerated);")
    if cleanup_index < 0 or success_index < 0 or cleanup_index > success_index:
        raise AssertionError("Project cleanup must remain failure-path behavior before successful UI finalization.")

    print("PASS: Direct Draw P0 forgets only failed projectless bootstraps after CAD/semantic rollback.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
