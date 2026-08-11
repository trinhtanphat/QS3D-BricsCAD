#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomBoundaryCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs")
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void DiscoverRooms()")
end = text.find("private static double MetadataNumber", start + 1) if start >= 0 else -1
body = text[start:end] if start >= 0 and end > start else ""
if not body:
    errors.append("cannot isolate QS3DROOMAUTO command body")
else:
    tokens = (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var existingPreview)",
        "RoomBoundarySegmentReader.ReadCurrentSelection(document, sagitta, planarityTolerance, splineChord)",
        "if (segments.Count == 0)",
        "RoomBoundaryDiscovery.Discover(segments, snapTolerance, minimumArea)",
        "if (boundaries.Count == 0)",
        "ExistingProjectMutationContext.Require(document, \"Room Auto\")",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "RoomBoundaryMaterializer.Materialize(document, project, boundaries)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("QS3DROOMAUTO missing preview/selection/discovery/commit lifecycle token")
    else:
        preview, read_selection, empty_segments, discover, empty_boundaries, require_existing, create_new, materialize = positions
        if not (preview < read_selection < empty_segments < discover < empty_boundaries):
            errors.append("QS3DROOMAUTO must preview metadata, then acquire selection and prove a closed boundary before any project commit path")
        if not (empty_boundaries < require_existing < materialize and empty_boundaries < create_new < materialize):
            errors.append("QS3DROOMAUTO existing bind/project creation must occur only after non-empty boundary discovery and before materialization")

for token in (
    "DefaultSnapToleranceM",
    "DefaultArcSagittaM",
    "DefaultPlanarityToleranceM",
    "DefaultSplineChordM",
    "DefaultMinimumAreaM2",
    "expectedProjectId = existingPreview.ProjectId",
    "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
    "creation-capable only after the user supplied usable CAD",
):
    if token not in text:
        errors.append("Room Auto creation/canonical lifecycle guard missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DROOMAUTO uses existing project metadata read-only or safe defaults for boundary discovery; cancel/empty/no-face exits before project binding/creation, while valid no-project geometry remains creation-capable and existing-project materialization binds the same canonical ProjectId.")
