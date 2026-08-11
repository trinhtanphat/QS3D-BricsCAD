#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"


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

    batch = method_slice(
        text,
        "public static int Capture(Document document, ElementCategory category)",
        "public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)")
    require_order(
        batch,
        "batch capture bootstrap rollback",
        "EnsureCapturePreflight(document, snapshots, category);",
        "ProjectContextCoordinator.TryGetReadOnly(document, out _);",
        "ProjectContextCoordinator.GetOrCreate(document);",
        "ProjectStateSnapshot.Capture(project);")
    require(batch, "RestoreCaptureOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError, \"Semantic capture batch\");", "batch cleanup")

    single = method_slice(
        text,
        "public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)",
        "private static void EnsureCapturePreflight(")
    require_order(
        single,
        "single capture bootstrap rollback",
        "EnsureCapturePreflight(document, new[] { snapshot }, category);",
        "ProjectContextCoordinator.TryGetReadOnly(document, out _);",
        "ProjectContextCoordinator.GetOrCreate(document);",
        "ProjectStateSnapshot.Capture(project);")
    require(single, "RestoreCaptureOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError, \"Semantic capture\");", "single cleanup")

    helper = method_slice(
        text,
        "private static void RestoreCaptureOrThrow(",
        "private static void RestoreOrThrow(")
    require_order(
        helper,
        "capture rollback helper",
        "rollback.Restore(project);",
        "if (!projectExistedBeforeCapture) ProjectContextCoordinator.Forget(document);")
    require(helper, "new AggregateException(operationError, restoreError)", "rollback aggregate preservation")

    legacy = method_slice(
        text,
        "private static void RestoreOrThrow(",
        "private static void ReplaceSourceMetric(")
    if "ProjectContextCoordinator.Forget" in legacy:
        raise AssertionError("Existing-project rollback helper must not forget project context.")

    print("PASS: failed semantic-capture bootstrap is forgotten while existing-project rollback remains intact.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
