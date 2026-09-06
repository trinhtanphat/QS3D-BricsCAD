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
    "if (additionalCharacters > MaximumSnapshotCharacters - (long)snapshot.Length)",
]:
    if anchor not in stamp:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: missing production anchor {anchor!r}")

if stamp.count("RequireSnapshotCapacity(snapshot, encoded.Length);") < 3:
    raise SystemExit("persistence stamp bounded snapshot preflight failed: scalar framing appenders are not all budget-admitted")

for anchor in [
    'const string encodedNull = "S-1:";',
    "RequireSnapshotCapacity(snapshot, encodedNull.Length);",
    'var prefix = "S" + value.Length.ToString(CultureInfo.InvariantCulture) + ":";',
    "RequireSnapshotCapacity(snapshot, (long)prefix.Length + value.Length);",
    "snapshot.Append(prefix).Append(value);",
]:
    if anchor not in stamp:
        raise SystemExit(f"persistence stamp bounded snapshot preflight failed: missing string-framing budget anchor {anchor!r}")

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
