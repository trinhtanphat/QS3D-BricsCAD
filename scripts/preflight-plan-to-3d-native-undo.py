#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
UNDO = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileUndoCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR: Plan-to-3D native Undo semantic preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str, start: int = 0) -> int:
    pos = text.find(token, start)
    if pos < 0:
        fail(label + " missing contract token: " + token)
    return pos


def main() -> None:
    for path, label in ((SOURCE, "PlanTo3DCommands.cs"), (UNDO, "SourceReconcileUndoCoordinator.cs")):
        if not path.is_file():
            fail("missing " + label)

    text = SOURCE.read_text(encoding="utf-8")
    undo = UNDO.read_text(encoding="utf-8")

    method_marker = "private static void ConvertPlanWalls(string operation, bool promptStyle)"
    next_marker = "private static IReadOnlyList<ObjectId>? AcquireSelection"
    start = text.find(method_marker)
    if start < 0:
        fail("ConvertPlanWalls implementation not found")
    end = text.find(next_marker, start)
    if end < 0:
        fail("ConvertPlanWalls boundary not found")
    body = text[start:end]

    if "document.Database.StartUndoRecord();" in body:
        fail("Database.StartUndoRecord is not the semantic Undo authority; reuse SourceReconcileUndoCoordinator")

    prompt_revalidation = require(body, "RequireSameSources(sources, refreshedSources);", "Plan-to-3D")
    bind_project = require(body, "projectPreview.ResolveForMutation(document, operation)", "Plan-to-3D", prompt_revalidation)
    commit_revalidation = require(body, "RequireSameSources(sources, commitSources);", "Plan-to-3D", bind_project)
    fresh = require(body, "RequireFreshSources(project, sources);", "Plan-to-3D", commit_revalidation)
    before = require(body, "var rollback = ProjectStateSnapshot.Capture(project);", "Plan-to-3D", fresh)
    stamp = require(
        body,
        "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
        "Plan-to-3D",
        before,
    )
    scope = require(
        body,
        "using (SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document))",
        "Plan-to-3D",
        stamp,
    )
    capture = require(body, "SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall)", "Plan-to-3D", scope)
    properties = require(body, 'element.Properties["ThicknessM"] =', "Plan-to-3D", capture)
    regenerate = require(body, "regenerator.RegenerateDirtySubset", "Plan-to-3D", properties)
    line_builder = require(body, "WallSolidBuilder.BuildSelectedLineWalls", "Plan-to-3D", regenerate)
    polyline_builder = require(body, "PolylineWallSolidBuilder.BuildSelected", "Plan-to-3D", line_builder)
    touch = require(body, "project.Touch();", "Plan-to-3D", polyline_builder)
    changed = require(body, "if (!rollbackStamp.Matches(project))", "Plan-to-3D", touch)
    publish = require(
        body,
        "SourceReconcileUndoCoordinator.CommitExternalTransition(",
        "Plan-to-3D",
        changed,
    )
    rollback = require(
        body,
        "RollbackBatch(document, project, rollback, createdElements, operationError);",
        "Plan-to-3D",
        publish,
    )
    finalize = require(body, "FinalizeUi(document, createdElements, sources.Count, solids, regenerated);", "Plan-to-3D", rollback)

    if not prompt_revalidation < bind_project < commit_revalidation < fresh < before < stamp < scope:
        fail("prompt/final-source validation and semantic before-state ordering changed")
    if not scope < capture < properties < regenerate < line_builder < polyline_builder < touch < changed < publish < rollback < finalize:
        fail("semantic/native mutation, history publication, rollback, or UI completion ordering changed")

    for forbidden in (
        "document.CommandEnded +=",
        "document.CommandWillStart +=",
        "new Dictionary<string, ProjectStateSnapshot>",
    ):
        if forbidden in body:
            fail("Plan-to-3D introduced competing Undo observation/history: " + forbidden)

    for token in (
        "if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);",
        "if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;",
        'if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";',
        'if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";',
        "targetEntry.Snapshot.Restore(project);",
        "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    ):
        require(undo, token, "production native semantic Undo coordinator")

    print(
        "PASS: Plan-to-3D captures one command-level semantic before-state, suppresses nested builder history, "
        "publishes the post-batch semantic revision through SourceReconcileUndoCoordinator, and preserves fail-closed rollback."
    )


if __name__ == "__main__":
    main()
