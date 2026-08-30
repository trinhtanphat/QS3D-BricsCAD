#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqWorkspaceKnownCountSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/tbq-workspace-known-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("TBQ workspace Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    'RequireKnownCountStable(items, MaxBillItems, "bill items", knownCount);',
    'RequireKnownCountStable(rates, MaxBuildUpRates, "build-up rates", knownCount);',
    'RequireKnownCountStable(source, maximum, label, knownCount);',
    'private static void RequireKnownCountStable<T>(IEnumerable<T> source, int maximum, string label, int? knownCount)',
    'var finalKnownCount = ValidateKnownCount(source, maximum, label);',
    'known count changed during traversal.',
    'if (knownCount.HasValue && index == knownCount.Value)',
    'RequireKnownCountMatchesTraversal(label, knownCount, count);',
    'reports an invalid negative known count',
    'reports conflicting known counts',
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("TBQ workspace Count-stability preflight missing source contract: " + ", ".join(missing))

bill_mismatch = source.index('RequireKnownCountMatchesTraversal("bill items", knownCount, index);')
bill_rebind = source.index('RequireKnownCountStable(items, MaxBillItems, "bill items", knownCount);', bill_mismatch)
bill_sort = source.index('snapshot.Sort(CompareBillItems);', bill_rebind)
if not bill_mismatch < bill_rebind < bill_sort:
    raise SystemExit("TBQ bill-item Count must be rebound after traversal equality and before sorting/publication.")

build_mismatch = source.index('RequireKnownCountMatchesTraversal("build-up rates", knownCount, index);')
build_rebind = source.index('RequireKnownCountStable(rates, MaxBuildUpRates, "build-up rates", knownCount);', build_mismatch)
build_sort = source.index('snapshot.Sort(CompareBuildUps);', build_rebind)
if not build_mismatch < build_rebind < build_sort:
    raise SystemExit("TBQ build-up Count must be rebound after traversal equality and before sorting/publication.")

bounded_mismatch = source.index('RequireKnownCountMatchesTraversal(label, knownCount, count);')
bounded_rebind = source.index('RequireKnownCountStable(source, maximum, label, knownCount);', bounded_mismatch)
if not bounded_mismatch < bounded_rebind:
    raise SystemExit("TBQ bounded collection Count must be rebound after exact traversal equality.")

required_smoke = (
    'BillItemCountDriftFailsAfterTraversal();',
    'BuildUpCountDriftFailsAfterTraversal();',
    'RateReferenceCountDriftFailsAfterTraversal();',
    'LibraryCountDriftFailsAfterTraversal();',
    'NegativeCountAfterTraversalFailsClosed();',
    'ConflictingCountsAfterTraversalFailClosed();',
    'StableMultiInterfaceCountsRemainAccepted();',
    'DriftingReadOnlyCollection<T>',
    'MultiCountSequence<T>',
    'Exact bill-item Count must be rebound throughout traversal.',
    'Exact rate-reference Count must be rebound throughout traversal.',
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("TBQ workspace Count-stability smoke is incomplete: " + ", ".join(missing_smoke))

for phrase in (
    "post-traversal",
    "bill items",
    "build-up rates",
    "rate references",
    "BQ library entries",
    "pure streaming",
    "10,000",
    "50,000",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("TBQ workspace Count-stability runbook missing boundary: " + phrase)

print("PASS TBQ workspace known-Count stability")