#!/usr/bin/env python3
"""Static source contract for LOCAL-006 native documentation source completion.

This gate deliberately covers source-safe contracts only. Licensed BricsCAD runtime
qualification remains in docs/LOCAL-AGENT-INBOX.md and must not be inferred from
this preflight.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def require_file(relative):
    path = ROOT / relative
    if not path.is_file():
        raise AssertionError("missing required source file: " + relative)
    return path.read_text(encoding="utf-8")


def require_all(text, relative, needles):
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(relative + " is missing contract token(s): " + ", ".join(missing))


def check_mleader_lifecycle():
    builder_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticMLeaderBuilder.cs"
    builder = require_file(builder_rel)
    require_all(
        builder,
        builder_rel,
        (
            "new MLeader",
            "GeneratedSemanticTagHealthService.HandlesKey",
            "GeneratedSemanticTagHealthService.ArtifactKindKey",
            "GeneratedSemanticTagHealthService.LeaderTargetHandleKey",
            "GeneratedSemanticTagHealthService.LeaderTextXKey",
            "GeneratedGeometryService.MarkGenerated",
            "ProjectStateSnapshot.Capture",
            "documentation.semantic-tag.mleader",
            "BuildBatch",
        ),
    )

    removal_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs"
    removal = require_file(removal_rel)
    require_all(removal, removal_rel, ("MText", "MLeader", "RequireSupportedSemanticTag"))

    health_rel = "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
    health = require_file(health_rel)
    require_all(
        health,
        health_rel,
        (
            "MText",
            "MLeader",
            "InspectMLeader",
            "SEMANTIC_TAG_MLEADER",
            "GeneratedSemanticTagLeaderTargetHandle",
        ),
    )

    commands_rel = "src/QS3D.BricsCAD.V25/SemanticTagCommands.cs"
    commands = require_file(commands_rel)
    require_all(
        commands,
        commands_rel,
        (
            'CommandMethod("QS3DTAGLEADER",',
            'CommandMethod("QS3DTAGLEADERBATCH",',
            "SemanticMLeaderBuilder.Build",
            "SemanticMLeaderBuilder.BuildBatch",
        ),
    )


def check_sheet_lifecycle():
    ownership_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticSheetOwnershipService.cs"
    ownership = require_file(ownership_rel)
    require_all(
        ownership,
        ownership_rel,
        (
            'RegAppName = "QS3D_SHEET"',
            "ArtifactLayout",
            "ArtifactPaperSpace",
            "ArtifactViewport",
            "ArtifactTitleBlock",
            "Mark",
            "RequireMatching",
        ),
    )

    service_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticSheetArtifactService.cs"
    service = require_file(service_rel)
    require_all(
        service,
        service_rel,
        (
            "SemanticSheetPlan",
            "SemanticViewPlan",
            "LayoutManager.Current",
            "CreateLayout",
            "BlockTableRecordId",
            "new Viewport",
            "CustomScale",
            "Locked = true",
            "new BlockReference",
            "SemanticTitleBlockParameterMapBuilder.Build",
            "AttributeReference",
            "Refresh",
            "Remove",
            "ProjectStateSnapshot.Capture(project)",
            '"documentation.semantic-sheet.remove"',
            "manager.DeleteLayout(normalizedLayoutName)",
            "rollback.Restore(project)",
        ),
    )
    if ".TryAdd(" in service:
        raise AssertionError(service_rel + " must remain net48-compatible and not depend on Dictionary.TryAdd")
    remove_start = service.find("public static void Remove(")
    remove_end = service.find("public static string LayoutNameFor(", remove_start)
    remove = service[remove_start:remove_end]
    audit = remove.find('"documentation.semantic-sheet.remove"')
    delete = remove.find("manager.DeleteLayout(normalizedLayoutName)")
    rollback = remove.find("rollback.Restore(project)", delete)
    if min(audit, delete, rollback) < 0 or not audit < delete < rollback:
        raise AssertionError(service_rel + " must audit before layout deletion and retain project rollback on pre-delete failure")

    health_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticSheetRuntimeHealthService.cs"
    health = require_file(health_rel)
    require_all(
        health,
        health_rel,
        (
            "SemanticSheetPlan",
            "SemanticViewPlan",
            "ArtifactViewport",
            "ArtifactTitleBlock",
            "CustomScale",
            "Locked",
            "ModelHealthIssue",
        ),
    )

    commands_rel = "src/QS3D.BricsCAD.V25/SemanticSheetCommands.cs"
    commands = require_file(commands_rel)
    require_all(
        commands,
        commands_rel,
        (
            'CommandMethod("QS3DSHEETBUILD",',
            'CommandMethod("QS3DSHEETREFRESH",',
            'CommandMethod("QS3DSHEETREMOVE",',
            'CommandMethod("QS3DSHEETHEALTH",',
            "PromptKeywordOptions",
            "SemanticSheetArtifactService",
            "SemanticSheetRuntimeHealthService",
        ),
    )


def check_table_presentation_defaults():
    service_rel = "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
    service = require_file(service_rel)
    require_all(
        service,
        service_rel,
        (
            "ApplyPresentationDefaults",
            "table.TableStyle = database.Tablestyle",
            "CellAlignment.MiddleCenter",
            "CellAlignment.MiddleLeft",
            "TitleRowType",
            "HeaderRowType",
            "DataRowType",
            "table.SetRowHeight(0,",
            "table.SetRowHeight(1,",
            "table.SetTextHeight(titleTextHeight, TitleRowType)",
            "table.SetTextHeight(textHeight, HeaderRowType | DataRowType)",
        ),
    )


if __name__ == "__main__":
    try:
        check_mleader_lifecycle()
        check_sheet_lifecycle()
        check_table_presentation_defaults()
    except AssertionError as exc:
        print("ERROR:", exc)
        sys.exit(1)
    print("PASS: LOCAL-006 native MLeader, sheet and shared Table presentation source contracts are present.")