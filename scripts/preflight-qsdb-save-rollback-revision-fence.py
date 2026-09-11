from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"

source = STORE.read_text(encoding="utf-8")

required = [
    "var previousSchemaVersion = project.SchemaVersion;",
    "var previousUpdatedUtc = project.UpdatedUtc;",
    "var previousChangeVersion = project.ChangeVersion;",
    "var saveOwnedSchemaVersion =",
    "var saveOwnedUpdatedUtc =",
    "var saveOwnedChangeVersion =",
    "saveOwnedSchemaVersion = project.SchemaVersion;",
    "saveOwnedUpdatedUtc = project.UpdatedUtc;",
    "RestoreFailedSavePersistenceState(",
    "if (project.SchemaVersion != saveOwnedSchemaVersion ||",
    "project.UpdatedUtc != saveOwnedUpdatedUtc ||",
    "project.ChangeVersion != saveOwnedChangeVersion)",
    "QSDB save rollback refused to overwrite a newer project persistence revision.",
    "project.SchemaVersion = previousSchemaVersion;",
    "project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);",
]
for anchor in required:
    if anchor not in source:
        raise SystemExit(f"qsdb save rollback revision fence preflight failed: missing production anchor {anchor!r}")

legacy_block = """if (!committed)
                {
                    project.SchemaVersion = previousSchemaVersion;
                    project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);
                }"""
if legacy_block in source:
    raise SystemExit("qsdb save rollback revision fence preflight failed: failed-save cleanup still restores stale persistence state unconditionally")

helper_index = source.find("private static bool RestoreFailedSavePersistenceState(")
restore_index = source.find("project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);")
if helper_index < 0 or restore_index < helper_index:
    raise SystemExit("qsdb save rollback revision fence preflight failed: persistence restore is not contained by the fenced rollback helper")

print("PASS qsdb failed-save rollback revision ownership fence source guard")
