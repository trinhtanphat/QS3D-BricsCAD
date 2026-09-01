#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PolygonRegionSetTopologySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/polygon-region-id-unicode-integrity.md"

errors = []


def read(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


source = read(SOURCE, "polygon region topology source")
smoke = read(SMOKE, "polygon region topology smoke")
read(RUNBOOK, "polygon region Unicode runbook")

for token, label in (
    ("new UTF8Encoding(false, true)", "strict UTF-8 encoder"),
    ("RequirePortableRegionId(id);", "portable RegionId admission"),
    ("StrictUtf8.GetByteCount(id);", "malformed UTF-16 rejection"),
    ("XmlConvert.VerifyXmlChars(id);", "XML text validation"),
    ("catch (EncoderFallbackException ex)", "Unicode failure contract"),
    ("catch (XmlException ex)", "XML failure contract"),
):
    require(source, token, label)

for token, label in (
    ("MalformedUnicodeIdsFailClosed();", "malformed Unicode smoke registration"),
    ('"region-\\uD800"', "lone high-surrogate regression"),
    ('"region-\\uDC00"', "lone low-surrogate regression"),
    ('"region-\\uFFFE"', "XML-invalid noncharacter regression"),
    ('"region-\\U0001F6E0"', "valid supplementary-plane control"),
    ("Equal(expected, topology.Islands.Single().RegionId);", "canonical RegionId retention control"),
    ("Equal(expected, segment.RegionId);", "tagged segment RegionId retention control"),
):
    require(smoke, token, label)

if errors:
    print("FAIL polygon region id Unicode integrity")
    for error in errors:
        print(" - " + error)
    raise SystemExit(1)

print("PASS polygon region id Unicode integrity")
