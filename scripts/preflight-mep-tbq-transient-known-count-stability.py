#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqTransientKnownCountSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/mep-tbq-transient-known-count-stability.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

start = source.find("public IReadOnlyList<MepTbqReportRow> BuildReport(")
end = source.find("public string SerializeCsv(", start)
region = source[start:end] if start >= 0 and end > start else ""

required = (
    "var hasKnownCount = TryGetKnownCount(groups, out var knownCount);",
    "using (var enumerator = groups.GetEnumerator())",
    "RequireStableKnownCount(groups, knownCount);",
    "var moved = enumerator.MoveNext();",
    "if (!moved)",
    "if (index == MaxGroups)",
    "if (hasKnownCount && index >= knownCount)",
    "var group = enumerator.Current;",
    "if (hasKnownCount && index != knownCount)",
    "rows.Sort(CompareRows);",
)
if not region or any(token not in region for token in required):
    raise SystemExit("MEP/TBQ transient Count guard missing BuildReport contract token")

first_rebound = region.find("RequireStableKnownCount(groups, knownCount);")
move = region.find("var moved = enumerator.MoveNext();", first_rebound)
move_result_gate = region.find("if (!moved)", move)
second_rebound = region.find("RequireStableKnownCount(groups, knownCount);", move_result_gate)
cap = region.find("if (index == MaxGroups)", second_rebound)
overrun = region.find("if (hasKnownCount && index >= knownCount)", cap)
current = region.find("var group = enumerator.Current;", overrun)
if min(first_rebound, move, move_result_gate, second_rebound, cap, overrun, current) < 0 or not (
    first_rebound < move < move_result_gate < second_rebound < cap < overrun < current
):
    raise SystemExit("MEP/TBQ BuildReport must rebind admitted Count before MoveNext and again after successful MoveNext before capacity/Current")

smoke_tokens = (
    "TransientGrowthFailsBeforeCurrent();",
    "TransientShrinkFailsBeforeCurrent();",
    "TransientNegativeFailsBeforeCurrent();",
    "TransientConflictFailsBeforeCurrent();",
    "Equal(0, source.CurrentReads);",
    "StableCountedAndStreamingRemainAccepted();",
    "[ModuleInitializer]",
)
if any(token not in smoke for token in smoke_tokens):
    raise SystemExit("MEP/TBQ transient Count smoke contract is incomplete")

runbook_tokens = (
    "Lane-Key: `issue-4721`",
    "Transient Count",
    "before semantic `Current`",
    "Runtime: `NOT_APPLICABLE`",
)
if any(token not in runbook for token in runbook_tokens):
    raise SystemExit("MEP/TBQ transient Count runbook contract is incomplete")

print("PASS: MEP/TBQ BuildReport rebinds known Count around MoveNext before semantic Current")
