from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "BcfZipPackage.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BcfZipPackageSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);",
    "new StreamReader(stream, StrictUtf8, false)",
    "var bytes = StrictUtf8.GetBytes(text);",
)
for token in required_source:
    if token not in source:
        raise SystemExit("BCF ZIP UTF-8 source contract missing: " + token)

for forbidden in (
    "new StreamReader(stream, new UTF8Encoding(false, true), true)",
    "new StreamReader(stream, StrictUtf8, true)",
):
    if forbidden in source:
        raise SystemExit("BCF ZIP reader must not enable BOM-based alternate encoding detection: " + forbidden)

required_smoke = (
    "AlternateBomEncodingsFailClosed();",
    'RewriteEntryEncoding(canonical, "bcf.version", Encoding.Unicode)',
    'RewriteEntryEncoding(canonical, TopicB + "/markup.bcf", Encoding.BigEndianUnicode)',
    'RewriteEntryEncoding(canonical, TopicB + "/" + Viewpoint + ".bcfv", new UTF32Encoding(false, true, true))',
    'RewriteEntryEncoding(legacy, "extensions.xml", new UTF32Encoding(true, true, true))',
    "encoding.GetPreamble()",
)
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("BCF ZIP alternate-encoding smoke contract missing: " + token)

print("PASS BCF ZIP strict UTF-8 text-entry encoding guard")
