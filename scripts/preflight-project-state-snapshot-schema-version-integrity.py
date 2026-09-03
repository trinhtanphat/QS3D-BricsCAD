from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectStateSnapshotSchemaVersionIntegritySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"

snapshot = SNAPSHOT.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_snapshot_anchors = [
    "RequireSupportedSchemaVersion(source);",
    "source.SchemaVersion <= 0",
    "source.SchemaVersion > ProjectState.CurrentSchemaVersion",
]
for anchor in required_snapshot_anchors:
    if anchor not in snapshot:
        raise SystemExit(f"snapshot schema-version integrity preflight failed: missing production anchor {anchor!r}")

required_smoke_anchors = [
    "RejectsUnsupportedSchemaVersionWithoutMutation(0);",
    "RejectsUnsupportedSchemaVersionWithoutMutation(-1);",
    "ProjectState.CurrentSchemaVersion + 1",
    "ProjectStateSnapshot.Capture(project)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "beforeChangeVersion",
    "beforeUpdatedUtc",
    "AcceptsCurrentSchemaVersion();",
]
for anchor in required_smoke_anchors:
    if anchor not in smoke:
        raise SystemExit(f"snapshot schema-version integrity preflight failed: missing smoke anchor {anchor!r}")

if "ProjectStateSnapshotSchemaVersionIntegritySmoke.Run();" not in registration:
    raise SystemExit("snapshot schema-version integrity preflight failed: smoke is not registered")

print("PASS project state snapshot schema-version integrity source guard")
