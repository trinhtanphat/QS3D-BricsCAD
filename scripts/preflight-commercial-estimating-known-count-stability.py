#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialEstimatingKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print("FAIL commercial estimating known-count stability: " + message)
    sys.exit(1)


def require_order(segment: str, markers: tuple[str, ...], label: str) -> None:
    cursor = -1
    for marker in markers:
        position = segment.find(marker, cursor + 1)
        if position < 0:
            fail(label + " missing ordered invariant: " + marker)
        cursor = position


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

portfolio_start = source.find("public EstimatingPortfolio(IEnumerable<EstimatingLine> lines)")
portfolio_end = source.find("public IReadOnlyList<EstimatingLine> Lines", portfolio_start)
request_start = source.find("public BulkRateAssignmentRequest(")
request_end = source.find("public IReadOnlyList<string> LineIds", request_start)
if portfolio_start < 0 or portfolio_end < 0 or request_start < 0 or request_end < 0:
    fail("commercial materializer source boundaries are missing")
portfolio = source[portfolio_start:portfolio_end]
request = source[request_start:request_end]

required_portfolio = [
    "using (var enumerator = lines.GetEnumerator())",
    "while (true)",
    "RequireKnownCountStable(lines, knownCount);",
    "if (!enumerator.MoveNext())",
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    "if (snapshot.Count >= MaximumLines)",
    "var line = enumerator.Current;",
    "var postTraversalKnownCount = SnapshotKnownCount(lines);",
    "known line count changed during enumeration",
]
for token in required_portfolio:
    if token not in portfolio:
        fail("portfolio invariant is missing: " + token)

require_order(portfolio, (
    "using (var enumerator = lines.GetEnumerator())",
    "while (true)",
    "RequireKnownCountStable(lines, knownCount);",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(lines, knownCount);",
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    "if (snapshot.Count >= MaximumLines)",
    "var line = enumerator.Current;",
), "portfolio traversal")

required_request = [
    "using (var enumerator = lineIds.GetEnumerator())",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)",
    "if (ids.Count >= MaximumSelectedLines)",
    "var raw = enumerator.Current;",
    "var postTraversalLineIdCount = SnapshotKnownCount(lineIds, MaximumSelectedLines, \"selected-line\");",
    "selected-line known count changed during enumeration",
    "using (var enumerator = unitRates.GetEnumerator())",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)",
    "if (rates.Count >= MaximumUnitRates)",
    "var assignment = enumerator.Current;",
    "var postTraversalUnitRateCount = SnapshotKnownCount(unitRates, MaximumUnitRates, \"unit-rate\");",
    "unit-rate known count changed during enumeration",
]
for token in required_request:
    if token not in request:
        fail("bulk assignment invariant is missing: " + token)

line_section_end = request.find("if (unitRates == null)")
if line_section_end < 0:
    fail("selected-line traversal boundary is missing")
line_section = request[:line_section_end]
require_order(line_section, (
    "using (var enumerator = lineIds.GetEnumerator())",
    "while (true)",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, \"selected-line\");",
    "if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)",
    "if (ids.Count >= MaximumSelectedLines)",
    "var raw = enumerator.Current;",
), "selected-line traversal")

unit_section = request[line_section_end:]
require_order(unit_section, (
    "using (var enumerator = unitRates.GetEnumerator())",
    "while (true)",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "if (!enumerator.MoveNext())",
    "RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, \"unit-rate\");",
    "if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)",
    "if (rates.Count >= MaximumUnitRates)",
    "var assignment = enumerator.Current;",
), "unit-rate traversal")

required_smoke = [
    "PortfolioOverrunRejectsBeforeSecondCurrent",
    "PortfolioUnderYieldFailsClosed",
    "PortfolioPostTraversalCountDriftFailsClosed",
    "PortfolioConflictingCountsFailBeforeTraversal",
    "SelectedLineOverrunRejectsBeforeSecondCurrent",
    "UnitRateOverrunRejectsBeforeSecondCurrent",
    "SelectedLineStreamingCeilingRejectsBeforeOverflowCurrent",
    "UnitRateStreamingCeilingRejectsBeforeOverflowCurrent",
    "HonestCommercialCollectionsRemainAccepted",
    "Equal(1, source.CurrentReads",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        fail("required deterministic smoke evidence is missing: " + token)

print("PASS commercial estimating known-count stability source guard")
