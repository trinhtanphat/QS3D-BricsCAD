#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomBoundaryCommands.cs"
DOC = ROOT / "docs" / "ROOM-AUTO-PREVIEW-COMMIT-FRESHNESS.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


text = read(SOURCE)
doc = read(DOC)
inbox = read(INBOX)

start = text.find("public void DiscoverRooms()")
end = text.find("private static void EnsureBoundaryCommitFreshness", start + 1) if start >= 0 else -1
body = text[start:end] if start >= 0 and end > start else ""
if not body:
    errors.append("cannot isolate QS3DROOMAUTO command body")
else:
    tokens = (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var existingPreview)",
        "RoomBoundarySegmentReader.ReadCurrentSelection(document, arcSagitta, tolerance, splineChord)",
        "LengthUnit? selectionUnit = segments.Count == 0 ? (LengthUnit?)null : CadUnitService.GetLengthUnit(document)",
        "new RoomBoundaryDiagnosticService().Analyze(segments, tolerance, minimumArea)",
        "var boundaries = diagnostic.AcceptedBoundaries",
        "if (boundaries.Count == 0)",
        "ExistingProjectMutationContext.Require(document, \"Room Auto\")",
        "ProjectContextCoordinator.TryGetReadOnly(document, out _)",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "EnsureBoundaryCommitFreshness(document, project, selectionUnit.Value, tolerance, arcSagitta, splineChord, minimumArea)",
        "ProjectStateSnapshot.Capture(project)",
        "ResolveRoomFamily(project)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("QS3DROOMAUTO missing diagnostic/freshness lifecycle token")
    else:
        preview, read_selection, selection_unit, diagnose, accepted, empty_boundaries, require_existing, appeared_project, create_new, freshness, snapshot, mutate = positions
        if not (preview < read_selection < selection_unit < diagnose < accepted < empty_boundaries):
            errors.append("QS3DROOMAUTO must preview metadata, acquire/metricize selection, diagnose topology, and exit no-face before any project commit path")
        if not (empty_boundaries < require_existing < appeared_project < create_new < freshness < snapshot < mutate):
            errors.append("QS3DROOMAUTO must bind/create only after accepted topology, reject an appeared project on the no-project preview path, then revalidate context before snapshot/mutation")

    sync_pos = body.find("SemanticCaptureService.SyncExistingRoomFinishes(project, element)")
    mark_pos = body.find("var staleRooms = AutoRoomLifecycle.MarkStaleForSelection(")
    target_pos = body.find("var regenerationTargets = new HashSet<string>(activeRoomIds, StringComparer.OrdinalIgnoreCase);")
    stale_loop_pos = body.find("foreach (var stale in staleRooms)", target_pos)
    add_pos = body.find("regenerationTargets.Add(stale.Id);", stale_loop_pos)
    subset_pos = body.find(".RegenerateDirtySubset(project, regenerationTargets);", add_pos)
    regeneration_positions = (sync_pos, mark_pos, target_pos, stale_loop_pos, add_pos, subset_pos)
    if any(position < 0 for position in regeneration_positions):
        errors.append("QS3DROOMAUTO missing scoped active/stale Room regeneration contract")
    elif not all(left < right for left, right in zip(regeneration_positions, regeneration_positions[1:])):
        errors.append("QS3DROOMAUTO must sync active Room finishes, mark selected stale Rooms, build active+stale targets, then regenerate only that subset")

    if ".RegenerateDirty(project)" in body or ".RegenerateProject(project)" in body:
        errors.append("QS3DROOMAUTO must not consume unrelated project dirty state via full-project regeneration")

for token in (
    "expectedProjectId = existingPreview.ProjectId",
    "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
    "QS3D project đã xuất hiện trong lúc đọc Room boundary",
    "Drawing unit policy đã thay đổi trong lúc đọc Room boundary",
    "Room boundary settings đã thay đổi trong lúc đọc selection",
    "Creation-capable only after usable CAD input produced at least one closed face.",
    "Cancel/empty/no-face paths above must never bootstrap a blank project.",
):
    if token not in text:
        errors.append("Room Auto preview/commit freshness guard missing: " + token)

helper_start = text.find("private static void EnsureBoundaryCommitFreshness")
helper_end = text.find("private static string FormatRoomBoundaryDiagnostic", helper_start + 1) if helper_start >= 0 else -1
helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
if not helper:
    errors.append("cannot isolate Room Auto commit freshness helper")
else:
    for token in (
        "CadUnitService.GetLengthUnit(document) != selectionUnit",
        'MetadataNumber(project, "RoomBoundaryToleranceM", 0.005d, minimumExclusive: 0d) != tolerance',
        'MetadataNumber(project, "RoomBoundaryArcSagittaM", 0.002d, minimumExclusive: 0d) != arcSagitta',
        'MetadataNumber(project, "RoomBoundarySplineChordM", 0.02d, minimumExclusive: 0d) != splineChord',
        'MetadataNonNegative(project, "RoomBoundaryMinimumAreaM2", 0.5d) != minimumArea',
    ):
        if token not in helper:
            errors.append("Room Auto commit freshness helper missing: " + token)

if "new RoomBoundaryEngine().Discover(segments, tolerance, minimumArea)" in body:
    errors.append("Room Auto lifecycle gate regressed to the pre-diagnostics direct topology path")

for token in (
    "no-project preview",
    "drawing-unit policy",
    "before `ProjectStateSnapshot`",
    "LOCAL-001",
):
    if token not in doc:
        errors.append("Room Auto freshness documentation missing: " + token)

for token in (
    "Also qualify `QS3DROOMAUTO` preview-to-commit freshness.",
    "project-appears refusal",
    "effective-unit freshness rejection",
    "docs/ROOM-AUTO-PREVIEW-COMMIT-FRESHNESS.md",
):
    if token not in inbox:
        errors.append("LOCAL-001 Room Auto freshness handoff missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DROOMAUTO exits diagnostics before project creation, rejects project appearance after a no-project preview, preserves same-ProjectId existing mutation, revalidates drawing-unit plus Room settings before snapshot/mutation, scopes final regeneration to active plus selected-stale Rooms without consuming unrelated dirty project state, and keeps the matching V25 scenario in LOCAL-001.")
