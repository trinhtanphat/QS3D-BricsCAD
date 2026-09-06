from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAMP = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceStamp.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectPersistenceStampBoundedSnapshotSmoke.cs"

stamp = STAMP.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for anchor in [
    "private const int MaximumSnapshotCharacters = 64 * 1024 * 1024;",
    "private static void RequireSnapshotCapacity(StringBuilder snapshot, long additionalCharacters)",
    "Persistence stamp semantic snapshot exceeds the supported 64 Mi-character materialization budget.",
]:
    if anchor not in stamp:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: missing production anchor {anchor!r}")

for method in ["AppendSequenceCount", "AppendInt32", "AppendDouble", "AppendString"]:
    start = stamp.find(f"private static void {method}")
    if start < 0:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: missing {method}")
    end = stamp.find("\n        }", start)
    if end < 0 or "RequireSnapshotCapacity(snapshot," not in stamp[start:end]:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: {method} is not budget-admitted before append")

if "new StringBuilder()" not in stamp:
    raise SystemExit("persistence stamp bounded snapshot preflight failed: semantic snapshot materialization shape changed unexpectedly")

for anchor in [
    "[ModuleInitializer]",
    "MaximumSnapshotCharacters",
    "RequireSnapshotCapacity",
    "(long)budget + 1L",
    "InvalidOperationException",
    "64 Mi-character",
]:
    if anchor not in smoke:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: missing smoke anchor {anchor!r}")

print("PASS project persistence stamp bounded semantic snapshot source guard")
