from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfZipPackage.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfZipPackageSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var settings = new XmlReaderSettings",
    "DtdProcessing = DtdProcessing.Prohibit",
    "XmlResolver = null",
    "MaxCharactersInDocument = MaxEntryBytes",
    "new StringReader(text)",
    "XmlReader.Create(textReader, settings)",
    "XDocument.Load(reader, LoadOptions.PreserveWhitespace)",
]
for token in required_source:
    if token not in source:
        raise SystemExit("FAIL: missing fail-closed BCF ZIP XML parser contract: " + token)

parse_start = source.index("private static XElement ParseRoot")
parse_end = source.index("private static void EnsureDocumentContent", parse_start)
parse_root = source[parse_start:parse_end]
if "XDocument.Parse(" in parse_root:
    raise SystemExit("FAIL: BCF ZIP ParseRoot must not use implicit XDocument.Parse on untrusted package XML")

settings_pos = parse_root.index("new XmlReaderSettings")
dtd_pos = parse_root.index("DtdProcessing = DtdProcessing.Prohibit")
reader_pos = parse_root.index("XmlReader.Create(textReader, settings)")
load_pos = parse_root.index("XDocument.Load(reader, LoadOptions.PreserveWhitespace)")
if not (settings_pos < dtd_pos < reader_pos < load_pos):
    raise SystemExit("FAIL: BCF ZIP DTD prohibition must be configured before XML reader creation and document materialization")

required_smoke = [
    "DtdBearingEntriesFailClosedBeforeSemanticUse();",
    "<!DOCTYPE Version [<!ENTITY v '3.0'>]>",
    "<!DOCTYPE Markup [<!ENTITY title 'Expanded title'>]>",
    "<!DOCTYPE VisualizationInfo [<!ENTITY one '1'>]>",
    "BCF version XML with an inline DTD/entity must fail closed.",
    "BCF markup XML with an inline DTD/entity must fail closed.",
    "BCF viewpoint XML with an inline DTD/entity must fail closed.",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("FAIL: missing deterministic hostile BCF ZIP DTD regression: " + token)

print("PASS: BCF ZIP XML parsing prohibits DTD/entity processing before document materialization")
