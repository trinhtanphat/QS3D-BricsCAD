#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawWindowCommands.cs"


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

    draw = method_slice(
        text,
        "private static void DrawWindowCore(",
        "private static void Execute(")
    require_order(
        draw,
        "Window deferred binding ownership",
        "var hasProjectBeforePrompts = projectPreview.HasProject;",
        "var project = BindProjectAfterPrompts(document, projectPreview, expectedProjectChangeVersion, operation);",
        "Execute(document, project, hasProjectBeforePrompts,")

    execute = method_slice(
        text,
        "private static void Execute(",
        "private static ProjectState BindProjectAfterPrompts(")
    require(execute, "bool projectExistedBeforeAuthoring", "Window ownership parameter")
    require(execute, "RequireExactProject(document, project, \"Direct Draw Cửa Sổ\");", "exact-project guard")
    require(execute, "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id);", "Auto Host preservation")
    require(execute, "string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase)", "stable-id re-resolution")
    require(execute, "RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })", "scoped host regeneration")
    require_order(
        execute,
        "Window failed bootstrap cleanup",
        "EraseSource(document, sourceId);",
        "rollback.Restore(project);",
        "if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);",
        "document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());")
    require(execute, "if (cleanupError != null || restoreError != null)", "rollback error aggregation")
    require(execute, "new AggregateException(errors)", "rollback aggregate preservation")

    bind = method_slice(
        text,
        "private static ProjectState BindProjectAfterPrompts(",
        "private static void RequireExactProject(")
    require(bind, "projectPreview.ResolveForMutation(document, operation);", "deferred project binding")
    require(bind, "project.ChangeVersion != expectedProjectChangeVersion.Value", "prompt freshness version guard")

    print("PASS: Window Direct Draw carries pre-prompt project ownership through deferred binding and forgets only failed projectless bootstraps.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
