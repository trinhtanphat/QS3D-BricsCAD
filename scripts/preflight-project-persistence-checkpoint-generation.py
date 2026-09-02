#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectPersistenceCheckpoint.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ProjectPersistenceCheckpoint.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "public ElementPersistenceState(ProjectElement owner, ElementDirtyFlags dirty, DateTime updatedUtc)",
        "Owner = owner ?? throw new ArgumentNullException(nameof(owner));",
        "public ProjectElement Owner { get; }",
        "new ElementPersistenceState(element, element.Dirty, element.UpdatedUtc)",
        "!ReferenceEquals(element, pair.Value.Owner)",
        "!ReferenceEquals(element, captured.Owner)",
        "Cannot restore persistence checkpoint because captured element generation changed:",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectPersistenceCheckpoint.cs missing generation-safety token: " + token)

    restore = text.find("public void Restore(ProjectState project)")
    resolve = text.find("var targets = new Dictionary<string, ProjectElement>", restore)
    affinity = text.find("!ReferenceEquals(element, captured.Owner)", restore)
    first_restore = text.find("pair.Value.Restore(targets[pair.Key]);", restore)
    project_restore = text.find("project.RestorePersistenceState", restore)
    if min(restore, resolve, affinity, first_restore, project_restore) < 0 or not (
        restore < resolve < affinity < first_restore < project_restore
    ):
        errors.append("Restore must validate every captured element generation before the first element/project persistence mutation.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectPersistenceCheckpoint restore is fenced to the exact captured element generations before mutation.")
