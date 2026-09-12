from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceCheckpoint.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
restore_start = source.find("public void Restore(ProjectState project)")
restore_end = source.find("private static void RequireStableKnownCount", restore_start)
if restore_start < 0 or restore_end < 0:
    fail("ProjectPersistenceCheckpoint.Restore source boundary was not found.")

restore = source[restore_start:restore_end]
version_check = "project.ChangeVersion != _projectChangeVersion"
timestamp_check = "project.UpdatedUtc != _projectUpdatedUtc"
first_restore = "foreach (var pair in _elements)"
project_restore = "project.RestorePersistenceState(_projectUpdatedUtc, _projectChangeVersion);"

version_index = restore.find(version_check)
timestamp_index = restore.find(timestamp_check)
first_restore_index = restore.find(first_restore)
project_restore_index = restore.find(project_restore)

if version_index < 0 or timestamp_index < 0:
    fail("checkpoint restore does not fail closed on project revision/timestamp drift")
if first_restore_index < 0 or project_restore_index < 0:
    fail("checkpoint restore mutation boundary was not found")
if max(version_index, timestamp_index) >= first_restore_index:
    fail("project revision/timestamp fence must run before the first element persistence restore")
if max(version_index, timestamp_index) >= project_restore_index:
    fail("project revision/timestamp fence must run before project persistence metadata restore")
if "Cannot restore a persistence checkpoint because the project revision changed" not in restore:
    fail("checkpoint restore revision-drift rejection is not explicit")

print("Project persistence checkpoint restore revision preflight passed.")
