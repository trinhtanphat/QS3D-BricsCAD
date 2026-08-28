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
    "index == MaxGroups",
    "index >= knownCount",
    "index != knownCount",
    "RequireStableKnownCount(groups, knownCount)",
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

order = [
    "TryGetKnownCount(groups, out var knownCount)",
    "foreach (var group in groups)",
    "if (hasKnownCount && index >= knownCount)",
    "rows.Add(new MepTbqReportRow(group));",
    "if (hasKnownCount && index != knownCount)",
    "RequireStableKnownCount(groups, knownCount)",
    "rows.Sort(CompareRows);",
]
pos = [source.index(token) for token in order]
if pos != sorted(pos):
    raise SystemExit("MEP/TBQ traversal validation order regressed")

print("PASS MEP/TBQ report traversal bound, Count stability, and lane runbook contract")
