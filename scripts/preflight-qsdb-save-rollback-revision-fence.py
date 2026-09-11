from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"

source = STORE.read_text(encoding="utf-8")

required = [
    "var saveOwnedSchemaVersion = project.SchemaVersion;",
    "var saveOwnedUpdatedUtc = project.UpdatedUtc;",
    "var saveOwnedChangeVersion = project.ChangeVersion;",
    "RestoreFailedSavePersistenceState(",
    "QSDB save rollback refused to overwrite a newer project persistence revision.",
]
for anchor in required:
    if anchor not in source:
        raise SystemExit(f"qsdb save rollback revision fence preflight failed: missing production anchor {anchor!r}")

legacy = "project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);"
if legacy in source:
    raise SystemExit("qsdb save rollback revision fence preflight failed: failed-save cleanup still restores stale persistence state unconditionally")

print("PASS qsdb failed-save rollback revision ownership fence source guard")
