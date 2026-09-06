#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DETACHER = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotDetacher.cs"
STORE = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RevisionSnapshotCardinalitySmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


detacher = read(DETACHER)
store = read(STORE)
smoke = read(SMOKE)

for needle in [
    "internal const int MaxElements = 100000;",
    "internal const int MaxEntriesPerCollection = 100000;",
    "internal static void ValidatePersistenceCardinality(RevisionSnapshot snapshot, string label)",
    "ValidatePersistenceCount(elements.Count, MaxElements",
    "ValidatePersistenceCount(element.Properties?.Count ?? -1, MaxEntriesPerCollection",
    "ValidatePersistenceCount(element.Quantities?.Count ?? -1, MaxEntriesPerCollection",
    "ValidatePersistenceCount(element.SourceHandles?.Count ?? -1, MaxEntriesPerCollection",
    "ValidatePersistenceCount(element.Dependencies?.Count ?? -1, MaxEntriesPerCollection",
    "throw new InvalidDataException(\"Revision \" + label + \" exceeds the supported bound of \" + maximum + \" entries.\");",
]:
    if needle not in detacher:
        errors.append("RevisionSnapshotDetacher missing persistence cardinality token: " + needle)

save_start = store.find("private void Save(RevisionSnapshot snapshot, string path, long maximumBytes)")
save_end = store.find("public RevisionSnapshot LoadWithBackupFallback", save_start)
save_body = store[save_start:save_end] if save_start >= 0 and save_end >= 0 else ""
if not save_body:
    errors.append("could not isolate RevisionSnapshotStore.Save body")
else:
    admission = save_body.find('RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "persistence");')
    path_resolution = save_body.find("var full = Path.GetFullPath(path);")
    serialization = save_body.find("Serialize(snapshot, bounded);")
    if admission < 0:
        errors.append("Save must apply shared cardinality admission")
    if admission >= 0 and path_resolution >= 0 and admission > path_resolution:
        errors.append("Save cardinality admission must run before path/temp publication side effects")
    if admission >= 0 and serialization >= 0 and admission > serialization:
        errors.append("Save cardinality admission must run before serialization")

load_start = store.find("public RevisionSnapshot Load(string path)")
load_end = store.find("private static int ReadSchemaVersion", load_start)
load_body = store[load_start:load_end] if load_start >= 0 and load_end >= 0 else ""
if not load_body:
    errors.append("could not isolate RevisionSnapshotStore.Load body")
else:
    admission = load_body.find('RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "loaded persistence");')
    returned = load_body.rfind("return snapshot;")
    duplicate_check = load_body.find("Revision contains duplicate element ids.")
    if admission < 0:
        errors.append("Load must apply shared cardinality admission")
    if admission >= 0 and returned >= 0 and admission > returned:
        errors.append("Load cardinality admission must run before returning parsed state")
    if admission >= 0 and duplicate_check >= 0 and admission < duplicate_check:
        errors.append("Load must preserve canonical/duplicate validation before final cardinality publication admission")

for needle in [
    "SaveAcceptsExactNestedBoundary();",
    "SaveRejectsOversizedNestedCollectionBeforePublication();",
    "SaveRejectsOversizedElementCollectionBeforePublication();",
    "LoadRejectsOversizedNestedCollection();",
    "private const int MaximumEntries = 100000;",
    "Capture<InvalidDataException>",
    "False(File.Exists(path)",
    "[ModuleInitializer]",
]:
    if needle not in smoke:
        errors.append("cardinality smoke missing public-behavior contract: " + needle)

# Mutation controls: the guard must reject the two simplest regressions that reopened
# the original defect — deleting Save admission or deleting Load admission.
def contract_holds(detacher_text, store_text):
    return (
        "internal const int MaxElements = 100000;" in detacher_text
        and "internal const int MaxEntriesPerCollection = 100000;" in detacher_text
        and 'RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "persistence");' in store_text
        and 'RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "loaded persistence");' in store_text
    )

if detacher and store:
    if not contract_holds(detacher, store):
        errors.append("baseline cardinality contract is incomplete")
    save_mutant = store.replace(
        'RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "persistence");',
        "",
        1,
    )
    if contract_holds(detacher, save_mutant):
        errors.append("mutation control failed: removed Save admission remained acceptable")
    load_mutant = store.replace(
        'RevisionSnapshotDetacher.ValidatePersistenceCardinality(snapshot, "loaded persistence");',
        "",
        1,
    )
    if contract_holds(detacher, load_mutant):
        errors.append("mutation control failed: removed Load admission remained acceptable")

print("QS3D revision snapshot persistence cardinality preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: revision capture and persistence share the 100,000-entry cardinality invariant; Save rejects hostile oversized state before filesystem publication, Load fails closed before returning oversized parsed state, and deterministic public-behavior smoke coverage locks exact/over-limit boundaries.")
