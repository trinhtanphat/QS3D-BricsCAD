from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AuditTrailCurrentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/audit-trail-current-count-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("audit Current-count integrity file missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

read_current = "var item = enumerator.Current;"
modify_current = "var existing = enumerator.Current;"
if source.count(read_current) != 1 or source.count(modify_current) != 1:
    raise SystemExit("audit traversal Current shape changed")

read_pos = source.index(read_current)
read_rebound = source.index("RequireStableHistoryCount(storedCount);", read_pos)
read_observed = source.index("observed++;", read_pos)
read_null = source.index("if (item == null)", read_pos)
read_budget = source.index("AccumulateTextCharacters(item, ref textCharacters);", read_pos)
read_validate = source.index("GetStoredEventValidationError(item)", read_pos)
read_clone = source.index("snapshot.Add(Clone(item));", read_pos)
if not (read_pos < read_rebound < read_observed < read_null < read_budget < read_validate < read_clone):
    raise SystemExit("AuditTrail.Events post-Current Count rebound ordering changed")

modify_pos = source.index(modify_current)
modify_rebound = source.index("RequireStableHistoryCount(storedCount);", modify_pos)
modify_observed = source.index("observed++;", modify_pos)
modify_null = source.index("if (existing == null)", modify_pos)
modify_budget = source.index("AccumulateTextCharacters(existing, ref textCharacters);", modify_pos)
modify_validate = source.index("GetStoredEventValidationError(existing)", modify_pos)
if not (modify_pos < modify_rebound < modify_observed < modify_null < modify_budget < modify_validate):
    raise SystemExit("AuditTrail mutation-validation post-Current Count rebound ordering changed")

required_smoke = (
    "EventsCurrentCountDriftPreemptsMalformedEventValidation",
    "ClearCurrentCountDriftPreemptsMalformedEventValidation",
    '"event count does not match stored history traversal"',
    "Equal(1, source.MoveNextCalls",
    "Equal(1, source.CurrentReads",
    "Equal(0, source.ClearCalls",
    "[ModuleInitializer]",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("audit Current-count smoke token(s) missing: " + repr(missing))

for token in (
    "post-`Current`",
    "Count drift",
    "before malformed-event validation",
    "read path",
    "mutation-validation path",
    "NOT_APPLICABLE",
):
    if token not in runbook:
        raise SystemExit("audit Current-count runbook token missing: " + token)

print("PASS AuditTrail post-Current Count stability before event acceptance")
