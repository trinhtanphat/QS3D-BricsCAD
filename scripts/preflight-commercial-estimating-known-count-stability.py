#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialEstimatingKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print("FAIL commercial estimating known-count stability: " + message)
    sys.exit(1)


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
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    "if (snapshot.Count >= MaximumLines)",
    "var line = enumerator.Current;",
    "var postTraversalKnownCount = SnapshotKnownCount(lines);",
    "known line count changed during enumeration",
]
for token in required_portfolio:
    if token not in portfolio:
        fail("portfolio invariant is missing: " + token)

for guard in [
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    "if (snapshot.Count >= MaximumLines)",
]:
    if portfolio.index(guard) > portfolio.index("var line = enumerator.Current;"):
        fail("portfolio bounds must execute before Current")

required_request = [
    "using (var enumerator = lineIds.GetEnumerator())",
    "if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)",
    "if (ids.Count >= MaximumSelectedLines)",
    "var raw = enumerator.Current;",
    "var postTraversalLineIdCount = SnapshotKnownCount(lineIds, MaximumSelectedLines, \"selected-line\");",
    "selected-line known count changed during enumeration",
    "using (var enumerator = unitRates.GetEnumerator())",
    "if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)",
    "if (rates.Count >= MaximumUnitRates)",
    "var assignment = enumerator.Current;",
    "var postTraversalUnitRateCount = SnapshotKnownCount(unitRates, MaximumUnitRates, \"unit-rate\");",
    "unit-rate known count changed during enumeration",
]
for token in required_request:
    if token not in request:
        fail("bulk assignment invariant is missing: " + token)

if request.index("if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)") > request.index("var raw = enumerator.Current;"):
    fail("selected-line known-count guard must execute before Current")
if request.index("if (ids.Count >= MaximumSelectedLines)") > request.index("var raw = enumerator.Current;"):
    fail("selected-line streaming ceiling must execute before Current")
unit_section = request[request.find("if (unitRates == null)"):]
if unit_section.index("if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)") > unit_section.index("var assignment = enumerator.Current;"):
    fail("unit-rate known-count guard must execute before Current")
if unit_section.index("if (rates.Count >= MaximumUnitRates)") > unit_section.index("var assignment = enumerator.Current;"):
    fail("unit-rate streaming ceiling must execute before Current")

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
