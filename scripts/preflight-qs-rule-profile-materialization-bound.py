#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/QsRuleProfile.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/qs-rule-profile-materialization-bound.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("QS rule profile materialization preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "MaximumRules = 10000",
    "MaterializeRules(rules)",
    "ReadKnownCount(rules)",
    "RequireKnownCountStable(rules, admittedCount)",
    "var moved = enumerator.MoveNext();",
    "var current = enumerator.Current;",
    "materialized.Add(current);",
    "enumerated more rules than its reported Count",
    "reported conflicting rule Count values",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("QS rule profile bounded materialization source contract missing: " + repr(missing))

if "var materialized = rules.ToList();" in source:
    raise SystemExit("QS rule profile must not return to unbounded LINQ rule materialization.")

move_pos = source.find("var moved = enumerator.MoveNext();")
move_rebound = source.find("RequireKnownCountStable(rules, admittedCount);", move_pos + 1)
current_pos = source.find("var current = enumerator.Current;", move_rebound)
current_rebound = source.find("RequireKnownCountStable(rules, admittedCount);", current_pos + 1)
retain_pos = source.find("materialized.Add(current);", current_rebound)
if min(move_pos, move_rebound, current_pos, current_rebound, retain_pos) < 0 or not (move_pos < move_rebound < current_pos < current_rebound < retain_pos):
    raise SystemExit("QS rule profile must rebound Count after MoveNext and Current before retaining a rule.")

required_smoke = (
    "RejectsKnownOverBoundBeforeEnumeration",
    "RejectsStreamingOverBoundBeforeUnexpectedCurrent",
    "RejectsTransientCurrentCountDrift",
    "MaximumRules + 1",
    "MoveNextCalls",
    "CurrentReads",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("QS rule profile materialization smoke contract missing: " + repr(missing_smoke))

print("PASS QS rule profile bounded known-Count materialization guard")
