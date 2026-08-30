#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Audit/AuditTrail.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AuditTrailTransientCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/audit-trail-transient-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Audit transient Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for forbidden in ("while (enumerator.MoveNext())", "foreach (var item in _events)", "foreach (var existing in _events)"):
    if forbidden in source:
        raise SystemExit("Audit traversal regressed to a Count-unsafe shape: " + forbidden)

for token in (
    "while (true)",
    "RequireStableHistoryCount(storedCount);",
    "if (!enumerator.MoveNext())",
    "RequireCanReadCurrent(storedCount, observed);",
    "RequireObservedHistoryCount(storedCount, observed);",
):
    if token not in source:
        raise SystemExit("Audit transient Count source contract missing: " + token)

for token in (
    "EventsRejectsTransientGrowthBeforeCurrent",
    "RecordRejectsTransientShrinkBeforeCurrentOrMutation",
    "ClearRejectsTransientNegativeCountBeforeCurrentOrMutation",
    "StableHistoryRemainsReadableAndMutable",
    "Equal(0, history.CurrentReads",
    "Equal(0, history.AddCalls",
    "Equal(0, history.ClearCalls",
    "private sealed class TransientCountHistory : IList<AuditEvent>",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("Audit transient Count smoke matrix missing: " + token)

print("PASS audit trail transient known-Count stability")
