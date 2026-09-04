#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotElementIdentitySmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Project snapshot quantity preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

helper_start = source.find("private static void RequireCanonicalQuantities(ProjectElement element)")
helper_end = source.find("private static bool HasControlCharacter", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("Project snapshot quantity validation helper is missing.")
helper = source[helper_start:helper_end]

canonical = "var canonicalName = quantity.Key.Trim();"
canonical_pos = helper.find(canonical)
reject_tokens = (
    "!string.Equals(canonicalName, quantity.Key, StringComparison.Ordinal)",
    "!string.Equals(quantity.Key, canonicalName, StringComparison.Ordinal)",
)
reject_positions = [helper.find(token, canonical_pos) for token in reject_tokens]
reject_positions = [position for position in reject_positions if position >= 0]
reject_pos = min(reject_positions) if reject_positions else -1
xml_pos = helper.find("XmlConvert.VerifyXmlChars(canonicalName)", canonical_pos)
duplicate_pos = helper.find("if (!canonicalNames.Add(canonicalName))", canonical_pos)
value_pos = helper.find("if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value) || quantity.Value < 0d)", canonical_pos)

if canonical_pos < 0:
    raise SystemExit("Project snapshot quantity validation must compute the trimmed canonical name.")
if reject_pos < 0:
    raise SystemExit("Project snapshot quantity validation must reject stored keys that differ ordinally from their canonical name.")
if xml_pos < 0 or duplicate_pos < 0 or value_pos < 0:
    raise SystemExit("Project snapshot quantity validation lost XML, duplicate-identity, or finite/non-negative value validation.")
if not (canonical_pos < reject_pos < xml_pos < duplicate_pos < value_pos):
    raise SystemExit("Project snapshot quantity validation ordering regressed; non-canonical identity must fail before later validation.")

required_source = (
    "RequireCanonicalQuantities(element);",
    "var canonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "foreach (var quantity in source.Quantities) target.SetQuantity(quantity.Key, quantity.Value);",
    "quantity names that collapse to the same canonical identity",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Project snapshot quantity source contract missing: " + repr(missing))
if "foreach (var quantity in source.Quantities) target.Quantities[quantity.Key] = quantity.Value;" in source:
    raise SystemExit("Project snapshot quantity clone regressed to raw dictionary assignment.")

required_smoke = (
    "RejectsInvalidMutableQuantityState();",
    "RejectsCanonicalQuantityNameCollision();",
    "DetachedCopyCanonicalizesNegativeZero();",
    'ExpectRejectedQuantity("padded name", " AreaM2 ", 1d);',
    'ExpectRejectedQuantity("negative", "AreaM2", -1d);',
    'ExpectRejectedQuantity("NaN", "AreaM2", double.NaN);',
    'ExpectRejectedQuantity("malformed UTF-16 name", "Area\\uD800M2", 1d);',
    'element.Quantities["AreaM2"] = BitConverter.Int64BitsToDouble',
    'detachedElement.Quantities.ContainsKey("AreaM2")',
    "BitConverter.DoubleToInt64Bits(copied) == 0L",
    "Rejected snapshot quantity validation mutated the source quantity dictionary.",
    "Rejected snapshot quantity validation changed project ChangeVersion.",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Project snapshot quantity regression coverage missing: " + repr(missing))
if "DetachedCopyCanonicalizesQuantityNameAndNegativeZero" in smoke:
    raise SystemExit("Legacy padded-key canonicalization smoke returned; padded mutable quantity identity must fail closed.")

print("PASS project snapshot quantity fail-closed canonicality, rollback, and negative-zero guard")
