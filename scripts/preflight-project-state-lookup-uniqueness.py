#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateLookupSmoke.cs"
errors = []

for path in (STATE, SMOKE):
    if not path.is_file():
        errors.append("missing project lookup uniqueness contract file: " + str(path.relative_to(ROOT)))

if STATE.is_file():
    text = STATE.read_text(encoding="utf-8")
    for token in (
        "FindUnique(Elements, NormalizeLookupId(id), x => x.Id, \"element\")",
        "FindUnique(Families, NormalizeLookupId(id), x => x.Id, \"family\")",
        "FindUnique(QuantityRules, NormalizeLookupId(id), x => x.Id, \"quantity rule\")",
        "if (match != null) throw new InvalidOperationException",
        "Project contains duplicate ",
    ):
        if token not in text:
            errors.append("ProjectState.cs missing unique lookup token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicateLookupsFailClosed",
        "Throws<InvalidOperationException>(() => project.FindElement",
        "Throws<InvalidOperationException>(() => project.FindFamily",
        "Throws<InvalidOperationException>(() => project.FindQuantityRule",
    ):
        if token not in text:
            errors.append("ProjectStateLookupSmoke.cs missing duplicate lookup regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: normalized ProjectState lookups fail closed instead of selecting an arbitrary duplicate semantic ID.")
