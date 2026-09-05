#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "FloorGeneratedIdentityPlanner.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []

if "using System.Text;" not in text:
    errors.append("FloorGeneratedIdentityPlanner must import System.Text for Unicode normalization")

if "NormalizationForm.FormC" not in text:
    errors.append("floor generated identity text must use Unicode NFC normalization")

canonical_start = text.find("private static string CanonicalFloorId")
canonical_end = text.find("private static string NormalizeName", canonical_start)
canonical = text[canonical_start:canonical_end] if canonical_start >= 0 and canonical_end > canonical_start else ""
if ".Normalize(NormalizationForm.FormC)" not in canonical:
    errors.append("CanonicalFloorId must NFC-normalize admitted text before hashing")

name_start = text.find("private static string NormalizeName")
name_end = text.find("private static void RequireNoControlCharacters", name_start)
name_block = text[name_start:name_end] if name_start >= 0 and name_end > name_start else ""
if ".Normalize(NormalizationForm.FormC)" not in name_block:
    errors.append("NormalizeName must NFC-normalize admitted text before state-token hashing")

# Preserve fail-closed malformed-Unicode admission before normalization. Normalization itself
# can throw on malformed surrogate input and should not replace the explicit domain error.
for label, block in (("CanonicalFloorId", canonical), ("NormalizeName", name_block)):
    well_formed = block.find("RequireWellFormedUnicode")
    normalize = block.find(".Normalize(NormalizationForm.FormC)")
    if normalize >= 0 and (well_formed < 0 or well_formed > normalize):
        errors.append(f"{label} must validate well-formed Unicode before NFC normalization")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    sys.exit(1)

print("PASS: floor generated identity Unicode canonicalization contract")
