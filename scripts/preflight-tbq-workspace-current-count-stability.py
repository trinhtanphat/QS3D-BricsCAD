from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqProjectWorkspaceSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/tbq-workspace-current-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("TBQ Current-count stability file missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

bill_current = "var item = enumerator.Current;"
if source.count(bill_current) != 3:
    raise SystemExit("TBQ traversal Current shape changed")

bill_start = source.index("private static IReadOnlyList<TbqBillItem> SnapshotBillItems")
build_start = source.index("private static IReadOnlyList<BuildUpRateSnapshot> SnapshotBuildUpRates")
bounded_start = source.index("private static IEnumerable<T> Bounded<T>")

bill_current_pos = source.index(bill_current, bill_start)
bill_rebound = source.index('RequireKnownCountStable(items, MaxBillItems, "bill items", knownCount);', bill_current_pos)
bill_null = source.index("if (item == null)", bill_current_pos)
bill_duplicate = source.index("if (!ids.Add(item.ItemCode))", bill_current_pos)
bill_snapshot = source.index("snapshot.Add(item);", bill_current_pos)
if not (bill_current_pos < bill_rebound < bill_null < bill_duplicate < bill_snapshot):
    raise SystemExit("TBQ bill-item post-Current Count rebound ordering changed")

build_current_pos = source.index(bill_current, build_start)
build_rebound = source.index('RequireKnownCountStable(rates, MaxBuildUpRates, "build-up rates", knownCount);', build_current_pos)
build_null = source.index("if (item == null)", build_current_pos)
build_duplicate = source.index("if (!ids.Add(item.RateCode))", build_current_pos)
build_snapshot = source.index("snapshot.Add(item);", build_current_pos)
if not (build_current_pos < build_rebound < build_null < build_duplicate < build_snapshot):
    raise SystemExit("TBQ build-up post-Current Count rebound ordering changed")

bounded_current_pos = source.index(bill_current, bounded_start)
bounded_rebound = source.index("RequireKnownCountStable(source, maximum, label, knownCount);", bounded_current_pos)
bounded_count = source.index("count++;", bounded_current_pos)
bounded_yield = source.index("yield return item;", bounded_current_pos)
if not (bounded_current_pos < bounded_rebound < bounded_count < bounded_yield):
    raise SystemExit("TBQ bounded-input post-Current Count rebound ordering changed")

required_smoke = (
    "CurrentCountDriftFailsBeforeItemAcceptance",
    "CurrentCountDriftCollection<TbqBillItem>",
    "CurrentCountDriftCollection<BuildUpRateSnapshot>",
    "CurrentCountDriftCollection<RateReferenceEdge>",
    "CurrentCountDriftCollection<BqLibraryEntry>",
    '"bill items known count changed during traversal"',
    '"build-up rates known count changed during traversal"',
    '"rate references known count changed during traversal"',
    '"BQ library entries known count changed during traversal"',
    "driftOnCurrent: false",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("TBQ Current-count smoke token(s) missing: " + repr(missing))

for token in (
    "post-`Current`",
    "Count drift",
    "bill items",
    "build-up rates",
    "rate references",
    "BQ library entries",
    "before semantic acceptance",
    "NOT_APPLICABLE",
):
    if token not in runbook:
        raise SystemExit("TBQ Current-count runbook token missing: " + token)

print("PASS TBQ workspace post-Current Count stability before semantic acceptance")
