#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawOpeningCommands.cs"


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
        "private static void Execute(",
        "private static IReadOnlyList<Point3d>? AcquireTwoPoints(")

    require_order(
        execute,
        "Opening Direct Draw bootstrap ownership",
        "EnsureActive(document, operation);",
        "var projectExistedBeforeAuthoring = projectPreview.HasProject;",
        "projectPreview.ResolveForMutation(document, operation);",
        "ProjectStateSnapshot.Capture(project);")

    require(execute, "new AutoHostLinkCommands().AutoLinkHosts();", "Auto Host preservation")
    require(execute, "string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase)", "stable-id re-resolution")
    require(execute, "RegenerateDirtySubset(project, new[] { createdElementId, hostId })", "scoped host regeneration")

    require_order(
        execute,
        "Opening Direct Draw failed bootstrap cleanup",
        "EraseSource(document, sourceId);",
        "rollback.Restore(project);",
        "if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);",
        "document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());")

    require(execute, "if (cleanupError != null || restoreError != null)", "rollback error aggregation")
    require(execute, "new AggregateException(errors)", "rollback aggregate preservation")

    print("PASS: Door/WallOpening Direct Draw forgets only failed projectless bootstraps while preserving Auto Host guards.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
