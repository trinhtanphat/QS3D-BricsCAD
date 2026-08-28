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
required_source = (
    "RequireCanonicalQuantities(element);",
    "var canonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "var canonicalName = quantity.Key.Trim();",
    "XmlConvert.VerifyXmlChars(canonicalName);",
    "if (!canonicalNames.Add(canonicalName))",
    "if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value) || quantity.Value < 0d)",
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
    "DetachedCopyCanonicalizesQuantityNameAndNegativeZero();",
    'ExpectRejectedQuantity("negative", "AreaM2", -1d);',
    'ExpectRejectedQuantity("NaN", "AreaM2", double.NaN);',
    'ExpectRejectedQuantity("malformed UTF-16 name", "Area\\uD800M2", 1d);',
    'element.Quantities[" AreaM2 "] = 2d;',
    'detachedElement.Quantities.ContainsKey("AreaM2")',
    "BitConverter.DoubleToInt64Bits(copied) == 0L",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Project snapshot quantity regression coverage missing: " + repr(missing))
print("PASS project snapshot quantity canonicality and rollback guard")
