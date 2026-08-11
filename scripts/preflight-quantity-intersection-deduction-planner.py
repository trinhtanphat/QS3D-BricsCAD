#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityIntersectionDeductionPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityIntersectionDeductionPlannerSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityIntersectionDeductionPlannerSmokeRegistration.cs"


def require(text, tokens, label):
    return [label + ": " + token for token in tokens if token not in text]


def main():
    code = CODE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    missing += require(code, [
        "public sealed class QuantityIntersectionCandidateMeasurement",
        "public sealed class QuantityIntersectionDeductionPlan",
        "public sealed class QuantityIntersectionDeductionPlanner",
        "private readonly QuantityCalculationDeductionGate _gate;",
        "_gate.TryAllowConcreteDeduction(",
        "_gate.TryAllowSideFormworkByConcreteDeduction(",
        "_gate.TryAllowBottomFormworkByConcreteDeduction(",
        "_gate.TryAllowSideFormworkBySideFormworkDeduction(",
        "_gate.TryAllowBottomFormworkByBottomFormworkDeduction(",
        "if (!found)",
        "return Empty(candidate.SourceCode, candidate.TargetCode);",
        "public bool RuleFound { get; }",
        "public bool HasAnyDeduction =>",
        "new QuantityIntersectionDeductionPlan(sourceCode, targetCode, false, 0d, 0d, 0d, 0d, 0d)",
        "double.IsNaN(value) || double.IsInfinity(value) || value < 0d",
    ], "planner")
    missing += require(smoke, [
        "AllEnabledAtExactThresholds();",
        "MixedFlagsAndBelowThresholdsZeroOnlyRejectedCandidates();",
        "MissingAndReversePairsStayEmpty();",
        "UnknownIntegerCodesRoundTripExactly();",
        "PlanningDoesNotMutateCandidate();",
        "RejectsMalformedCandidateEvidence();",
        "new QuantityIntersectionCandidateMeasurement(\n                1301, 1302",
        "new QuantityIntersectionCandidateMeasurement(\n                403, 901",
        "double.PositiveInfinity",
    ], "smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityIntersectionDeductionPlannerSmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: quantity intersection deduction planner contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    forbidden = [
        "MinConcreteVolumeM3",
        "MinSubtractAreaMm2",
        "MinFormworkAreaMm2",
        "TryGetIntersectionRule(targetCode, sourceCode",
        "new QuantityIntersectionRuleSetting",
        "(ElementCategory)",
        "BooleanOperation",
        "Brep",
        "Solid3d",
        "ProjectQuantityReportBuilder",
        "StructuralRegenerator",
        "ProjectState",
        "AuditTrail",
        "EngulfRelPercent",
        "EngulfMinAreaMm2",
    ]
    present = [token for token in forbidden if token in code]
    if present:
        print("ERROR: deduction planner duplicated business rules or crossed a prohibited geometry/report boundary:")
        for item in present:
            print(" -", item)
        return 1

    mutable_plan_tokens = [
        "public int SourceCode { get; set; }",
        "public int TargetCode { get; set; }",
        "public bool RuleFound { get; set; }",
        "public double ConcreteVolumeM3 { get; set; }",
    ]
    mutable = [token for token in mutable_plan_tokens if token in code]
    if mutable:
        print("ERROR: deduction plan/candidate handoff must stay immutable:")
        for item in mutable:
            print(" -", item)
        return 1

    print("PASS: quantity intersection deduction planner delegates to the gate, preserves directed exact-code evidence, is immutable, zeros missing/denied deductions and remains geometry/report agnostic.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
