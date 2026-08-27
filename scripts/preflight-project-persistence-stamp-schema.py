#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectPersistenceStampSchemaVersionSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing persistence-stamp schema-version integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private int _savedSchemaVersion;",
        "_savedSchemaVersion = project.SchemaVersion;",
        "var savedSchemaVersion = project.SchemaVersion;",
        "_savedSchemaVersion = savedSchemaVersion;",
        "project.SchemaVersion == _savedSchemaVersion",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectPersistenceStamp.cs missing schema-version dirty-state token: " + token)

    mark_saved = text.find("public void MarkSaved(ProjectState project)")
    capture = text.find("var savedSchemaVersion = project.SchemaVersion;", mark_saved)
    nested = text.find("var savedNestedPersistedContent = SnapshotNestedPersistedContent(project);", mark_saved)
    publish = text.find("_savedSchemaVersion = savedSchemaVersion;", mark_saved)
    if min(mark_saved, capture, nested, publish) < 0 or not (mark_saved < capture < nested < publish):
        errors.append("MarkSaved must capture schema version before snapshot materialization and publish it only after the snapshot succeeds.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SchemaVersionOnlyChangeIsDirty();",
        "MarkSavedRefreshesSchemaVersion();",
        "OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact();",
        "project.SchemaVersion = ProjectState.CurrentSchemaVersion - 1;",
        "project.ChangeVersion == originalChangeVersion",
        "stamp.RequiresSave(project)",
        "stamp.MarkSaved(project);",
    ):
        if token not in text:
            errors.append("ProjectPersistenceStampSchemaVersionSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectPersistenceStamp treats SchemaVersion as persisted dirty state and refreshes it atomically in MarkSaved.")
