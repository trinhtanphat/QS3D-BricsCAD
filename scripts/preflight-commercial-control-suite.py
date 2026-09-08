#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TENDER = ROOT / "src/QS3D.Core/Commercial/TenderProcurementWorkflow.cs"
CONTROL = ROOT / "src/QS3D.Core/Commercial/CommercialCostControl.cs"
TENDER_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TenderProcurementWorkflowSmoke.cs"
CONTROL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialCostControlSmoke.cs"
ENTRY = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestEntryPoint.cs"

errors = []

def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing commercial control suite file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(source: str, token: str, label: str):
    if token not in source:
        errors.append(f"{label}: missing {token!r}")

tender = read(TENDER)
control = read(CONTROL)
tender_smoke = read(TENDER_SMOKE)
control_smoke = read(CONTROL_SMOKE)
entry = read(ENTRY)

for token in (
    "public sealed class TenderProcurementPackage",
    "public sealed class TenderProcurementService",
    "new TenderEvaluationService().Evaluate(package.Requirements, package.Bids)",
    "PassesMandatoryCompliance",
    "RecommendedBidId",
    "public TenderAwardDecision Award(",
    "CommercialRevisionRef PackageRevision",
    "CommercialRevisionRef AwardRevision",
):
    require(tender, token, "tender")

for token in (
    "public sealed class CommercialControlPeriod",
    "public sealed class CommercialCostControlService",
    "CommercialGuard.Add(",
    "CommercialGuard.Subtract(",
    "ForecastFinalCost",
    "ForecastVariance",
    "CvrMargin",
    "public CommercialControlPeriod Freeze(",
    "public CommercialControlPeriod Reopen(",
    "public CommercialControlPeriod ReviseForecast(",
):
    require(control, token, "cost-control")

# Ranking arithmetic must stay in the existing Cost tender authority.
for forbidden in (
    "requirement.Quantity *",
    "quote.UnitRate *",
    "EvaluatedTotal = checked",
):
    if forbidden in tender:
        errors.append(f"tender: duplicate tender arithmetic is forbidden: {forbidden!r}")

for token in (
    "ReusesTenderRankingAndComplianceForRecommendation",
    "AwardFailsClosedForIncompleteOrNonCompliantBid",
):
    require(tender_smoke, token, "tender smoke")
for token in (
    "EvaluatesBudgetCvrAndForecastExactly",
    "FrozenPeriodRejectsMutationUntilReopened",
):
    require(control_smoke, token, "cost-control smoke")
require(entry, "TenderProcurementWorkflowSmoke.Run();", "smoke entry")
require(entry, "CommercialCostControlSmoke.Run();", "smoke entry")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS commercial control suite source guard")
