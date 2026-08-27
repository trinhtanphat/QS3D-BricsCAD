#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectPersistenceStampSchemaVersionSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing persistence-stamp schema/revision integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private int _savedSchemaVersion;",
        "var snapshot = CaptureStableSnapshot(project);",
        "_savedSchemaVersion = snapshot.Boundary.SchemaVersion;",
        "boundary.SchemaVersion == _savedSchemaVersion",
        "private static StableSnapshot CaptureStableSnapshot(ProjectState project)",
        "var boundary = new PersistenceBoundary(project);",
        "SnapshotNestedPersistedContent(project, boundary)",
        "if (!boundary.Matches(project))",
        "Project state changed while the persistence stamp was materializing persisted content.",
        "AppendString(snapshot, boundary.Name);",
        "AppendDateTime(snapshot, boundary.UpdatedUtc);",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectPersistenceStamp.cs missing schema/revision integrity token: " + token)

    if text.count("var snapshot = CaptureStableSnapshot(project);") < 3:
        errors.append("constructor, RequiresSave, and MarkSaved must all use stable persistence capture.")

    mark_saved = text.find("public void MarkSaved(ProjectState project)")
    capture = text.find("var snapshot = CaptureStableSnapshot(project);", mark_saved)
    publish_version = text.find("_savedChangeVersion = snapshot.Boundary.ChangeVersion;", mark_saved)
    publish_schema = text.find("_savedSchemaVersion = snapshot.Boundary.SchemaVersion;", mark_saved)
    if min(mark_saved, capture, publish_version, publish_schema) < 0 or not (
        mark_saved < capture < publish_version <= publish_schema
    ):
        errors.append(
            "MarkSaved must complete stable capture before publishing saved revision/schema state."
        )

    stable_capture = text.find("private static StableSnapshot CaptureStableSnapshot(ProjectState project)")
    boundary_capture = text.find("var boundary = new PersistenceBoundary(project);", stable_capture)
    metadata_capture = text.find("var metadata = SnapshotMetadata(project.Metadata);", stable_capture)
    nested_capture = text.find("var nestedPersistedContent = SnapshotNestedPersistedContent(project, boundary);", stable_capture)
    revalidate = text.find("if (!boundary.Matches(project))", stable_capture)
    publish_capture = text.find("return new StableSnapshot(boundary, metadata, nestedPersistedContent);", stable_capture)
    if min(stable_capture, boundary_capture, metadata_capture, nested_capture, revalidate, publish_capture) < 0 or not (
        stable_capture < boundary_capture < metadata_capture < nested_capture < revalidate < publish_capture
    ):
        errors.append(
            "stable persistence capture must bind the boundary before collection traversal and revalidate it before returning."
        )

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SchemaVersionOnlyChangeIsDirty();",
        "MarkSavedRefreshesSchemaVersion();",
        "OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact();",
        "ConstructorRejectsRevisionDriftDuringTraversal();",
        "RequiresSaveRejectsRevisionDriftDuringTraversal();",
        "FailedMarkSavedDoesNotPublishMixedRevisionState();",
        "new MutatingList<ZoneDefinition>",
        "RequireThrows<InvalidOperationException>",
        "project.DrawingFingerprint = \"constructor-drift\";",
        "project.DrawingFingerprint = \"requires-save-drift\";",
        "project.ActiveZoneId = \"zone-a\";",
        "stamp.RequiresSave(project)",
        "stamp.MarkSaved(project);",
    ):
        if token not in text:
            errors.append("ProjectPersistenceStampSchemaVersionSmoke.cs missing schema/revision regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectPersistenceStamp binds schema/revision dirty state to one stable persisted snapshot and publishes MarkSaved atomically.")
