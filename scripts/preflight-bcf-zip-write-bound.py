from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfZipPackage.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfZipWriteBoundSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

for token in [
    "public const int MaxArchiveBytes = 16 * 1024 * 1024;",
    "BoundedArchiveWriteStream",
    "new BoundedArchiveWriteStream(stream, MaxArchiveBytes)",
    "new ZipArchive(boundedStream, ZipArchiveMode.Create, true)",
    'throw new InvalidDataException("BCF package exceeds the bounded archive size.")',
    "Math.Max(_inner.Length, checked(_inner.Position + count))",
]:
    if token not in source:
        raise SystemExit("FAIL: missing bounded BCF ZIP write contract: " + token)

bounded_pos = source.index("new BoundedArchiveWriteStream(stream, MaxArchiveBytes)")
archive_pos = source.index("new ZipArchive(boundedStream, ZipArchiveMode.Create, true)")
array_pos = source.index("stream.ToArray()")
if not (bounded_pos < archive_pos < array_pos):
    raise SystemExit("FAIL: bounded archive stream must wrap ZIP output before final byte materialization")

for token in [
    "AggregatePackageCrossingArchiveCeilingFailsClosed();",
    "OrdinaryPackageStillRoundTrips();",
    "BcfIssueExchangeContract.MaxTopics",
    "BcfZipPackage.Write(BcfIssueExchange.Create(topics))",
    '"BCF package exceeds the bounded archive size."',
    "BcfZipPackage.Read(package)",
]:
    if token not in smoke:
        raise SystemExit("FAIL: missing deterministic BCF ZIP write-bound smoke contract: " + token)

if "BcfZipWriteBoundSmoke.Run();" not in registration:
    raise SystemExit("FAIL: BCF ZIP write-bound smoke is not registered")

print("PASS: BCF ZIP write is bounded during archive emission")