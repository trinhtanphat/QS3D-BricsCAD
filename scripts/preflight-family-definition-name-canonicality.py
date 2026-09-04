#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "FamilyDefinition.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.find("private static string RequireName(string value)")
if start < 0:
    fail("FamilyDefinition.RequireName is missing")
end = text.find("private static string NormalizeMaterial", start)
if end < 0:
    fail("FamilyDefinition.NormalizeMaterial boundary is missing")
block = text[start:end]

required = (
    "string.IsNullOrWhiteSpace(value)",
    "ValidatePersistedText(value, nameof(value), \"Family name\")",
    "var canonical = value.Trim();",
    "string.Equals(value, canonical, StringComparison.Ordinal)",
    "leading or trailing whitespace",
    "return value;",
)
for marker in required:
    if marker not in block:
        fail(f"FamilyDefinition name boundary is missing canonicality marker: {marker}")

trim_pos = block.index("var canonical = value.Trim();")
equality_pos = block.index("string.Equals(value, canonical, StringComparison.Ordinal)")
return_pos = block.index("return value;")
if not (trim_pos < equality_pos < return_pos):
    fail("FamilyDefinition name canonicality must be checked before identity publication")

if "return value.Trim();" in block or "return canonical;" in block:
    fail("FamilyDefinition name identity must not silently normalize malformed surrounding whitespace")

material_start = text.find("private static string NormalizeMaterial", end)
material_end = text.find("private static string ValidatePersistedText", material_start)
material = text[material_start:material_end]
if "return value.Trim();" not in material:
    fail("FamilyDefinition material normalization contract changed unexpectedly")

print("PASS: FamilyDefinition rejects non-canonical name identity without widening material semantics")
