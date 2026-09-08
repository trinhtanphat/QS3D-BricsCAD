from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfIssueExchangeSerializer.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var settings = new XmlReaderSettings",
    "DtdProcessing = DtdProcessing.Prohibit",
    "XmlResolver = null",
    "MaxCharactersInDocument = MaxSemanticXmlCharacters",
    "new StringReader(payload)",
    "XmlReader.Create(textReader, settings)",
    "XDocument.Load(reader, LoadOptions.None)",
]
for token in required_source:
    if token not in source:
        raise SystemExit("FAIL: missing fail-closed BCF issue-exchange XML parser contract: " + token)

parse_start = source.index("public static BcfIssueExchange Deserialize")
parse_end = source.index("private static IReadOnlyList<BcfViewpoint> ReadViewpoints", parse_start)
deserialize = source[parse_start:parse_end]
if "XDocument.Parse(" in deserialize:
    raise SystemExit("FAIL: BCF issue-exchange Deserialize must not use implicit XDocument.Parse on untrusted XML")

settings_pos = deserialize.index("new XmlReaderSettings")
dtd_pos = deserialize.index("DtdProcessing = DtdProcessing.Prohibit")
resolver_pos = deserialize.index("XmlResolver = null")
reader_pos = deserialize.index("XmlReader.Create(textReader, settings)")
load_pos = deserialize.index("XDocument.Load(reader, LoadOptions.None)")
if not (settings_pos < dtd_pos < reader_pos < load_pos and settings_pos < resolver_pos < reader_pos):
    raise SystemExit("FAIL: BCF issue-exchange DTD/resolver policy must be configured before reader creation and XML materialization")

required_smoke = [
    "DtdBearingPayloadsFailClosedBeforeSemanticUse();",
    "<!DOCTYPE BcfIssueExchange [<!ENTITY version '3.0'>]>",
    "<!DOCTYPE BcfIssueExchange SYSTEM",
    "BCF issue exchange XML with an inline DTD/entity must fail closed before entity expansion.",
    "BCF issue exchange XML with an external DTD must fail closed before resolver use.",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("FAIL: missing deterministic hostile BCF issue-exchange DTD regression: " + token)

print("PASS: BCF issue-exchange XML parsing prohibits DTD/entity processing before document materialization")
