#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
DOMAIN = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyRestoreAtomicitySmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing snapshot family restore atomicity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


snapshot = read(SNAPSHOT)
domain = read(DOMAIN)
smoke = read(SMOKE)

for token in (
    "ProjectStateSnapshotFamilyRestoreAtomicitySmoke",
    "ThrowingFamilySubscriberCannotPublishPartialRestore",
    "PropertyChangedEventHandler throwingSubscriber",
    "Snapshot restore must preserve captured ProjectFamily object identity.",
):
    if token not in smoke:
        errors.append("snapshot family restore regression missing: " + token)

# Snapshot rollback/materialization must not route through externally-observable
# ProjectFamily setters. A hostile PropertyChanged subscriber can throw after a
# setter has already mutated the family, leaving CopyInto half-applied.
for token in (
    "internal void RestoreSnapshotState(",
    "_name = nextName;",
    "_category = nextCategory;",
):
    if token not in domain:
        errors.append("ProjectFamily snapshot restore atomicity contract missing: " + token)

if "target.RestoreSnapshotState(source.Name, source.Category, source.Properties);" not in snapshot:
    errors.append("ProjectStateSnapshot must restore preserved family state through the non-notifying atomic snapshot path")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectStateSnapshot restores preserved family state without exposing a partial rollback to throwing PropertyChanged subscribers.")
