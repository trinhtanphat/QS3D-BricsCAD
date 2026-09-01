#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfZipPackage.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfXmlEntryWriteBoundSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

for token in [
    "public const int MaxEntryBytes = 2 * 1024 * 1024;",
    "BoundedEntryWriteStream",
    "new BoundedEntryWriteStream(stream, MaxEntryBytes)",
    "XmlWriter.Create",
    "root.WriteTo(writer)",
    "StrictUtf8.GetString(stream.ToArray())",
]:
    if token not in source:
        raise SystemExit("FAIL: missing bounded BCF XML entry serialization token: " + token)

xml_pos = source.index("private static string Xml(XElement root)")
bounded_pos = source.index("new BoundedEntryWriteStream(stream, MaxEntryBytes)", xml_pos)
write_pos = source.index("root.WriteTo(writer)", xml_pos)
array_pos = source.index("StrictUtf8.GetString(stream.ToArray())", xml_pos)
if not (xml_pos < bounded_pos < write_pos < array_pos):
    raise SystemExit("FAIL: BCF XML must be bounded before root serialization and byte materialization")

if "new XDocument(new XDeclaration(\"1.0\", \"UTF-8\", null), root).ToString" in source:
    raise SystemExit("FAIL: BCF XML still materializes an unbounded document string before entry admission")

for token in [
    "OversizedMarkupFailsClosedAtEntryCeiling();",
    "OrdinaryMarkupStillRoundTrips();",
    "index <= 600",
    "BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }))",
    "BCF package entry exceeds the bounded size:",
    "BcfZipPackage.Read(package)",
]:
    if token not in smoke:
        raise SystemExit("FAIL: missing deterministic BCF XML entry-bound smoke token: " + token)

if "BcfXmlEntryWriteBoundSmoke.Run();" not in registration:
    raise SystemExit("FAIL: BCF XML entry-bound smoke is not registered")

print("PASS: BCF XML entry serialization is bounded before materialization")
