#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawReferenceWallCommands.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        pos = text.find(token, cursor + 1)
        if pos < 0:
            raise AssertionError(label + " missing ordered token: " + token)
        cursor = pos


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def main():
    text = SOURCE.read_text(encoding="utf-8")
    draw = method_slice(text, "private static void DrawWallFromReferenceCore(", "private static ReferenceLinePlan? AcquireReferenceLine(")
    acquire = method_slice(text, "private static ReferenceLinePlan? AcquireReferenceLine(", "private static ReferenceLinePlan? ReadReferenceLine(")
    execute = method_slice(text, "private static void Execute(", "private static ObjectId CreateWcsLine(")

    require_order(
        text,
        "Reference acquisition before mutation",
        "var reference = AcquireReferenceLine(document);",
        "var projectPreview = DirectDrawProjectPreviewContext.Capture(document);")
    require_order(
        acquire,
        "PICKFIRST fallback",
        "document.Editor.SelectImplied()",
        "objectIds.Length == 1",
        "ReadReferenceLine(document, objectIds[0], failIfNotLine: false)",
        "document.Editor.GetEntity(options)")
    require_order(
        draw,
        "Bootstrap ownership handoff",
        "var hasDefaultsProject = projectPreview.HasProject;",
        "projectPreview.ResolveForMutation(document, operation)",
        "Execute(",
        "project,",
        "hasDefaultsProject,")

    require(execute, "bool projectExistedBeforeAuthoring,", "Execute ownership parameter")
    require_order(
        execute,
        "Failed bootstrap cleanup",
        "EraseCreatedCad(document, project, createdElement, sourceId, generatedHandles);",
        "rollback.Restore(project);",
        "if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);",
        "document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());",
        "if (ownershipDiscoveryError != null || cleanupError != null || restoreError != null)")

    require(execute, "RegenerateDirtySubset(project, new[] { createdElementId })", "scoped regeneration")
    require(execute, "WallSolidBuilder.BuildSelectedLineWalls", "native wall builder")
    require(text, "GeneratedGeometryService.RequireMatchingOwnership", "generated ownership rollback")
    require(execute, "FinalizeUi(document, createdElement!, sourceId, solids, regenerated);", "success UI finalization")

    print("PASS: Reference Wall preserves PICKFIRST/scoped ownership and releases only failed projectless bootstraps.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
