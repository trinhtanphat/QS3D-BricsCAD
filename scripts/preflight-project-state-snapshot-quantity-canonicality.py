#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectStateSnapshot.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.find("private static void RequireCanonicalQuantities(ProjectElement element)")
end = text.find("private static bool HasControlCharacter", start)
if start < 0 or end < 0:
    fail("ProjectStateSnapshot must retain the canonical quantity validation helper")

block = text[start:end]
canonical = "var canonicalName = quantity.Key.Trim();"
canonical_pos = block.find(canonical)
if canonical_pos < 0:
    fail("quantity validation must compute the canonical trimmed name")

reject_tokens = (
    "!string.Equals(canonicalName, quantity.Key, StringComparison.Ordinal)",
    "!string.Equals(quantity.Key, canonicalName, StringComparison.Ordinal)",
)
reject_pos = min((block.find(token, canonical_pos) for token in reject_tokens if block.find(token, canonical_pos) >= 0), default=-1)
if reject_pos < 0:
    fail("snapshot quantity validation must reject a key whose ordinal text differs from its trimmed canonical name")

xml_pos = block.find("XmlConvert.VerifyXmlChars(canonicalName)", canonical_pos)
copy_pos = text.find("target.SetQuantity", start)
if xml_pos < 0:
    fail("quantity validation must retain XML character validation")
if not (canonical_pos < reject_pos < xml_pos):
    fail("non-canonical quantity identity must be rejected immediately after canonicalization and before later validation")
if copy_pos < 0:
    fail("snapshot materialization must retain SetQuantity so this guard protects against silent key normalization")

print("PASS: project snapshot rejects non-canonical quantity keys before detached materialization")
