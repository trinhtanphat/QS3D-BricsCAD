#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationDeductionGate.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationDeductionGateSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationDeductionGateSmokeRegistration.cs"


def require(text, tokens, label):
    missing = [token for token in tokens if token not in text]
    return [label + ": " + token for token in missing]


def main():
    code = CODE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    missing += require(code, [
        "public sealed class QuantityCalculationDeductionGate",
        "QuantityCalculationRuleSet _rules",
        "_settings = rules.Snapshot;",
        "public bool AllowsFormworkArea(double candidateAreaMm2)",
        "candidateAreaMm2 >= _settings.MinFormworkAreaMm2",
        "public bool TryAllowConcreteDeduction(int sourceCode, int targetCode",
        "rule.SubtractConcrete && candidateVolumeM3 >= _settings.MinConcreteVolumeM3",
        "x => x.SubtractSideFormworkByConcrete",
        "x => x.SubtractBottomFormworkByConcrete",
        "x => x.SubtractSideFormworkBySideFormwork",
        "x => x.SubtractBottomFormworkByBottomFormwork",
        "candidateAreaMm2 >= _settings.MinSubtractAreaMm2",
        "_rules.TryGetIntersectionRule(sourceCode, targetCode, out var rule)",
        "_rules.TryGetIntersectionRule(source, target, out var rule)",
        "double.IsNaN(value) || double.IsInfinity(value) || value < 0d",
        "throw new ArgumentOutOfRangeException",
    ], "gate")
    missing += require(smoke, [
        "ExactThresholdsAndAllFlags();",
        "BelowThresholdAndDisabledFlags();",
        "DirectedAndMissingPairsStayDistinct();",
        "NativeCompatibilityUsesEstablishedRuleSetMapping();",
        "SnapshotIsDefensive();",
        "RejectsMalformedCandidates();",
        "TryAllowConcreteDeduction(901, 403, 0.0001d",
        "TryAllowSideFormworkByConcreteDeduction(901, 403, 10d",
        "TryAllowBottomFormworkByConcreteDeduction(901, 403, 10d",
        "TryAllowSideFormworkBySideFormworkDeduction(901, 403, 10d",
        "TryAllowBottomFormworkByBottomFormworkDeduction(901, 403, 10d",
        "TryAllowConcreteDeduction(403, 901, 1d",
        "TryAllowConcreteDeduction(ElementCategory.Room, ElementCategory.Column",
        "double.PositiveInfinity",
    ], "smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityCalculationDeductionGateSmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: quantity deduction gate contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    forbidden = [
        "(ElementCategory)sourceCode",
        "(ElementCategory)targetCode",
        "TryGetIntersectionRule(targetCode, sourceCode",
        "TryGetIntersectionRule(target, source",
        "new QuantityIntersectionRuleSetting",
        "BooleanOperation",
        "Brep",
        "EngulfRelPercent",
        "EngulfMinAreaMm2",
        "ProjectQuantityReportBuilder",
        "StructuralRegenerator",
    ]
    present = [token for token in forbidden if token in code]
    if present:
        print("ERROR: deduction gate crossed a prohibited geometry/mapping/report boundary:")
        for item in present:
            print(" -", item)
        return 1

    print("PASS: quantity deduction gate is directed, thresholded, defensive, exact-code-safe and geometry-agnostic.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
