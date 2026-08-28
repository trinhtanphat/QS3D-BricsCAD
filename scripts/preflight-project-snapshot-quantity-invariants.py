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
    "if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value) || quantity.Value < 0d)",
    "foreach (var quantity in source.Quantities) target.SetQuantity(quantity.Key, quantity.Value);",
    "Cannot snapshot element ",
    "non-canonical padded quantity name",
    "quantity name containing control characters",
)
missing_source = [token for token in required_source if token not in source]
if missing_source:
    raise SystemExit("Project snapshot quantity source contract missing: " + repr(missing_source))

raw_assignment = "foreach (var quantity in source.Quantities) target.Quantities[quantity.Key] = quantity.Value;"
if raw_assignment in source:
    raise SystemExit("Project snapshot quantity clone regressed to raw dictionary assignment.")

validate_call = source.index("RequireCanonicalQuantities(element);")
copy_method = source.index("private static void CopyElementInto")
if validate_call > copy_method:
    raise SystemExit("Project snapshot quantity validation must run before element-copy mutation paths.")

required_smoke = (
    "RejectsInvalidMutableQuantityState();",
    "DetachedCopyCanonicalizesNegativeZero();",
    'ExpectRejectedQuantity("negative", "AreaM2", -1d);',
    'ExpectRejectedQuantity("NaN", "AreaM2", double.NaN);',
    'ExpectRejectedQuantity("positive infinity", "AreaM2", double.PositiveInfinity);',
    'ExpectRejectedQuantity("padded name", " AreaM2", 1d);',
    'ExpectRejectedQuantity("control-character name", "Area\\tM2", 1d);',
    "BitConverter.DoubleToInt64Bits(copied) == 0L",
    "project.ChangeVersion == originalChangeVersion",
    "project.UpdatedUtc == originalProjectUpdatedUtc",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Project snapshot quantity regression coverage missing: " + repr(missing_smoke))

print("PASS project snapshot quantity canonicality and rollback guard")
