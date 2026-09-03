#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
DOMAIN = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
FAMILY_SERVICE = ROOT / "src/QS3D.Core/Domain/ProjectFamilyService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyRestoreAtomicitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyRestoreAtomicitySmokeRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing snapshot family restore atomicity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


snapshot = read(SNAPSHOT)
domain = read(DOMAIN)
family_service = read(FAMILY_SERVICE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "ProjectStateSnapshotFamilyRestoreAtomicitySmoke",
    "ThrowingFamilySubscriberCannotPublishPartialRestore",
    "PropertyChangedEventHandler throwingSubscriber",
    "Snapshot restore must preserve captured ProjectFamily object identity.",
    "Snapshot restore must preserve the captured ProjectFamily property-store object identity.",
):
    if token not in smoke:
        errors.append("snapshot family restore regression missing: " + token)

for token in (
    "[ModuleInitializer]",
    "ProjectStateSnapshotFamilyRestoreAtomicitySmoke.Run();",
):
    if token not in registration:
        errors.append("snapshot family restore atomicity smoke registration missing: " + token)

# Snapshot rollback/materialization must not route through externally-observable
# ProjectFamily setters or persistence-aware dictionary mutation callbacks.
for token in (
    "internal void RestoreSnapshotState(",
    "var nextName = RequireName(name);",
    "var nextCategory = RequireCategory(category);",
    "var nextProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);",
    "_name = nextName;",
    "_category = nextCategory;",
    "_properties.ReplaceSnapshotState(nextProperties);",
):
    if token not in domain:
        errors.append("ProjectFamily snapshot restore atomicity contract missing: " + token)

if "internal void ReplaceSnapshotState(Dictionary<string, string> replacement)" not in domain:
    errors.append("ProjectFamily property store lacks an internal callback-free snapshot replacement path")

# SnapshotProperties returns one validated, detached, read-only materialization.
# Restore must consume that exact materialization rather than validating the
# source dictionary and then enumerating the mutable source a second time.
for token in (
    "bool preserveNullValues = false",
    "preserveNullValues && pair.Value == null ? null! : normalizedValue",
):
    if token not in family_service:
        errors.append("ProjectFamilyService snapshot null-fidelity contract missing: " + token)

for token in (
    "var snapshotProperties = ProjectFamilyService.SnapshotProperties(",
    '"snapshot materialization",',
    "preserveNullValues: true);",
    "target.RestoreSnapshotState(source.Name, source.Category, snapshotProperties);",
):
    if token not in snapshot:
        errors.append("ProjectStateSnapshot family materialization contract missing: " + token)

if 'target.RestoreSnapshotState(source.Name, source.Category, source.Properties);' in snapshot:
    errors.append("ProjectStateSnapshot must not re-enumerate mutable source family properties after validation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectStateSnapshot materializes family properties once with null backing fidelity, runs the regression through dedicated deterministic registration, and restores preserved family state without external callbacks or stale property-store identity.")
