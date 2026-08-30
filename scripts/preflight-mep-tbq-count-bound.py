#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqProjectionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/mep-tbq-count-bound.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = [
    "internal const int MaxGroups = 10000;",
    "TryGetKnownCount(groups, out var knownCount)",
    "knownCount > MaxGroups",
    "using (var enumerator = groups.GetEnumerator())",
    "RequireStableKnownCount(groups, knownCount);",
    "var moved = enumerator.MoveNext();",
    "if (!moved)",
    "index == MaxGroups",
    "index >= knownCount",
    "var group = enumerator.Current;",
    "index != knownCount",
    "ICollection<MepQuantityGroup>",
    "IReadOnlyCollection<MepQuantityGroup>",
    "ICollection nonGenericCollection",
    "source reports conflicting known counts",
    "source reports an invalid negative known count",
]
required_smoke = [
    "KnownCountOverrunWinsBeforeExtraRowValidation();",
    "KnownCountUnderYieldFailsClosed();",
    "KnownCountDriftFailsClosedAfterTraversal();",
    "OversizedKnownCountFailsBeforeEnumeration();",
    "StreamingTraversalIsBounded();",
    "new CountedGroups(10001",
    "Stream(group, 10001)",
]
required_runbook = [
    "Lane-Key: `issue-4383`",
    "Reservation-Protocol: `v2`",
    "agent/longnguyentuan2107-maker-c01-20260828t1900z-mep-tbq/issue-4383-mep-tbq-count-bound",
    "MaxGroups = 10000",
    "known Count overrun",
    "known Count under-yield",
    "Count drift after traversal",
    "Runtime: `NOT_APPLICABLE`",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
missing += [token for token in required_runbook if token not in runbook]
if missing:
    raise SystemExit("MEP/TBQ traversal guard missing contract token(s): " + ", ".join(missing))

start = source.index("public IReadOnlyList<MepTbqReportRow> BuildReport(")
end = source.index("public string SerializeCsv(", start)
region = source[start:end]
first_rebound = region.index("RequireStableKnownCount(groups, knownCount);")
move = region.index("var moved = enumerator.MoveNext();", first_rebound)
second_rebound = region.index("RequireStableKnownCount(groups, knownCount);", move)
cap = region.index("if (index == MaxGroups)", second_rebound)
overrun = region.index("if (hasKnownCount && index >= knownCount)", cap)
current = region.index("var group = enumerator.Current;", overrun)
add = region.index("rows.Add(new MepTbqReportRow(group));", current)
under_yield = region.index("if (hasKnownCount && index != knownCount)", add)
final_rebound = region.index("RequireStableKnownCount(groups, knownCount);", under_yield)
sort = region.index("rows.Sort(CompareRows);", final_rebound)
if not (first_rebound < move < second_rebound < cap < overrun < current < add < under_yield < final_rebound < sort):
    raise SystemExit("MEP/TBQ traversal validation order regressed")

print("PASS MEP/TBQ report traversal bound, Count stability, and lane runbook contract")
