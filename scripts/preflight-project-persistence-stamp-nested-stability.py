from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAMP = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceStamp.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectPersistenceStampNestedStabilitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"

stamp = STAMP.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

for anchor in [
    "var firstMetadata = SnapshotMetadata(project.Metadata);",
    "var firstNestedPersistedContent = SnapshotNestedPersistedContent(project, boundary);",
    "var secondMetadata = SnapshotMetadata(project.Metadata);",
    "var secondNestedPersistedContent = SnapshotNestedPersistedContent(project, boundary);",
    "!MetadataMatches(secondMetadata, firstMetadata)",
    "!string.Equals(secondNestedPersistedContent, firstNestedPersistedContent, StringComparison.Ordinal)",
    "Nested persisted project state changed while the persistence stamp was materializing content.",
]:
    if anchor not in stamp:
        raise SystemExit(f"persistence stamp nested stability preflight failed: missing production anchor {anchor!r}")

if stamp.count("SnapshotNestedPersistedContent(project, boundary)") < 2:
    raise SystemExit("persistence stamp nested stability preflight failed: nested content is not materialized twice")
if stamp.count("SnapshotMetadata(project.Metadata)") < 2:
    raise SystemExit("persistence stamp nested stability preflight failed: metadata is not materialized twice")

for anchor in [
    "RejectsFamilyMutationDuringMaterialization();",
    "new MutatingDictionary(",
    "() => family.Name = \"After\"",
    "new ProjectPersistenceStamp(project)",
    "beforeProjectRevision",
    "stamp.RequiresSave(project)",
]:
    if anchor not in smoke:
        raise SystemExit(f"persistence stamp nested stability preflight failed: missing smoke anchor {anchor!r}")

if "ProjectPersistenceStampNestedStabilitySmoke.Run();" not in registration:
    raise SystemExit("persistence stamp nested stability preflight failed: smoke is not registered")

print("PASS project persistence stamp nested stability source guard")
