#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateLookupSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (STATE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing project lookup contract file: " + str(path.relative_to(ROOT)))

if STATE.is_file():
    text = STATE.read_text(encoding="utf-8")
    for token in (
        'FindUnique(Elements, NormalizeLookupId(id), x => x.Id, "element")',
        'FindUnique(Families, NormalizeLookupId(id), x => x.Id, "family")',
        'FindUnique(QuantityRules, NormalizeLookupId(id), x => x.Id, "quantity rule")',
        "private static string NormalizeLookupId(string id) => (id ?? string.Empty).Trim();",
        "private static T? FindUnique<T>",
        "if (normalizedId.Length == 0) return null;",
        'throw new InvalidOperationException("Project contains duplicate " + label + " id: " + normalizedId)',
    ):
        if token not in text:
            errors.append("ProjectState.cs missing normalized lookup token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "LookupsNormalizeWhitespaceAndCase();",
        "BlankAndMissingLookupsReturnNull();",
        "DuplicateLookupsFailClosed();",
        'project.FindElement(" element-1 ")',
        'project.FindFamily(" family-1 ")',
        'project.FindQuantityRule(" rule-1 ")',
    ):
        if token not in text:
            errors.append("ProjectStateLookupSmoke.cs missing lookup regression token: " + token)

if REG.is_file() and "ProjectStateLookupSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("project-state lookup smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] project semantic element/family/rule lookups are statically guarded for trimmed case-insensitive IDs, blank/missing inputs and duplicate-ID ambiguity")
