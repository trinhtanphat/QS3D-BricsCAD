#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GUARD = ROOT / "src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (GUARD, SMOKE, REG):
    if not path.is_file():
        errors.append("missing BOM null release-block contract file: " + str(path.relative_to(ROOT)))

if GUARD.is_file():
    text = GUARD.read_text(encoding="utf-8")
    for token in (
        "if (element == null)",
        '"BOM_NULL_ELEMENT"',
        "HealthSeverity.Error",
        "continue;",
    ):
        if token not in text:
            errors.append("BomReleaseGuardService.cs missing null-state release-block token: " + token)
    null_guard = text.find("if (element == null)")
    exclusion = text.find("AutoRoomLifecycle.IsExcludedFromQuantity(project, element)")
    if null_guard < 0 or exclusion < 0 or null_guard > exclusion:
        errors.append("BOM null-element guard must run before quantity-exclusion logic dereferences the semantic element.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NullSemanticEntryBlocksReleaseWithoutCrashing",
        'Has(issues, "BOM_NULL_ELEMENT")',
        'x.Code == "BOM_NULL_ELEMENT" && x.Severity == HealthSeverity.Error',
    ):
        if token not in text:
            errors.append("BomReleaseGuardSmoke.cs missing null release-block regression token: " + token)

if REG.is_file() and "BomReleaseGuardSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("BOM release guard smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: a null semantic element becomes an Error-level BOM release blocker without crashing the guard. This source-only gate does not inspect V25 files.")
