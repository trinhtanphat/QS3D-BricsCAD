#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingKnownCountStabilitySmoke.cs"
CURRENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingCurrentCountAcceptanceSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/estimating-known-count-stability.md"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        fail(f"missing {label}: {marker}")
    return index


def require_order(segment: str, markers: tuple[str, ...], label: str) -> None:
    cursor = -1
    for marker in markers:
        position = segment.find(marker, cursor + 1)
        if position < 0:
            fail(f"{label} missing ordered marker: {marker}")
        if position <= cursor:
            fail(f"{label} marker is out of order: {marker}")
        cursor = position


for path, label in (
    (SOURCE, "production source"),
    (SMOKE, "historical smoke"),
    (CURRENT_SMOKE, "Current-boundary smoke"),
    (RUNBOOK, "runbook"),
):
    if not path.is_file():
        fail(f"missing {label}: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
current_smoke = CURRENT_SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "private static void RequireKnownCountStable(IEnumerable<EstimatingLine> lines, int? expectedKnownCount)",
    "private static void RequireKnownCountStable<T>(IEnumerable<T> values, int? expectedKnownCount, int maximum, string subject)",
):
    require(source, token, "Count stability helper")

portfolio_start = require(source, "public EstimatingPortfolio(IEnumerable<EstimatingLine> lines)", "portfolio materializer")
portfolio_end = require(source[portfolio_start:], "public IReadOnlyList<EstimatingLine> Lines", "portfolio materializer end") + portfolio_start
portfolio = source[portfolio_start:portfolio_end]
require_order(portfolio, (
    "using (var enumerator = lines.GetEnumerator())",
    "RequireKnownCountStable(lines, knownCount);",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(lines, knownCount);",
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    "var line = enumerator.Current;",
    "RequireKnownCountStable(lines, knownCount);",
    "if (line == null)",
), "portfolio traversal")

request_start = require(source, "public BulkRateAssignmentRequest(", "bulk request")
request_end = require(source[request_start:], "public IReadOnlyList<string> LineIds", "bulk request end") + request_start
request = source[request_start:request_end]
require_order(request, (
    "using (var enumerator = lineIds.GetEnumerator())",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "var raw = enumerator.Current;",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "var id = CommercialGuard.RequireToken(raw, nameof(lineIds));",
), "selected-line traversal")
require_order(request, (
    "using (var enumerator = unitRates.GetEnumerator())",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "var assignment = enumerator.Current;",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "if (assignment == null)",
), "unit-rate traversal")

for method in (
    "RejectPortfolioTransientGrowthBeforeSecondMoveNext",
    "RejectSelectedLineTransientShrinkBeforeSecondMoveNext",
    "RejectUnitRateTransientNegativeBeforeSecondMoveNext",
    "RejectUnitRateTransientConflictBeforeSecondMoveNext",
    "PreserveStableCountedInputs",
    "PreserveStreamingInputs",
):
    require(smoke, method, f"historical smoke regression {method}")

for method in (
    "PortfolioRejectsCurrentInducedCountDriftBeforeNullAcceptance",
    "SelectedLineRejectsCurrentInducedCountDriftBeforeTokenAcceptance",
    "UnitRateRejectsCurrentInducedCountDriftBeforeNullAcceptance",
    "StableCountedControlsRemainAccepted",
):
    require(current_smoke, method, f"Current-boundary regression {method}")
for token in (
    "reached ordinary item acceptance before Count stability was rebound",
    "CurrentReads",
    "Current-induced Count drift",
):
    require(current_smoke, token, f"Current-boundary smoke token {token}")

for phrase in (
    "before every MoveNext",
    "after every successful MoveNext",
    "before IEnumerator.Current",
    "immediately after every successful Current",
    "before semantic acceptance",
    "transient Count drift",
    "streaming",
    "NOT_APPLICABLE",
):
    require(runbook, phrase, f"runbook contract {phrase}")

print("PASS: estimating known-Count stability includes post-Current acceptance boundary")
