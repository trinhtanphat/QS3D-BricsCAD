#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationMatrixDiagnostics.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationMatrixDiagnosticsSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationMatrixDiagnosticsSmokeRegistration.cs"


def require(text, tokens, label):
    return [label + ": " + token for token in tokens if token not in text]


def main():
    code = CODE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    missing += require(code, [
        "public static class QuantityCalculationMatrixDiagnostics",
        "var snapshot = settings.Clone();",
        "snapshot.NormalizeAndValidate();",
        ".OrderBy(x => x)",
        "IntersectionOnlyCategoryCodes",
        "UnreferencedCategoryRuleCodes",
        "MissingDirectedPairs",
        "foreach (var sourceCode in observedCodes)",
        "foreach (var targetCode in observedCodes)",
        "if (!existingPairs.Contains(PairKey(sourceCode, targetCode)))",
        "new QuantityCalculationMatrixPair(sourceCode, targetCode)",
        "public bool IsCompleteDirectedMatrix => MissingDirectedPairs.Count == 0;",
        "public long ExpectedDirectedRuleCount =>",
    ], "diagnostics")
    missing += require(smoke, [
        "CompleteMatrixReportsNoGaps();",
        "MissingReversePairRemainsDirected();",
        "ReportsIntersectionOnlyAndUnreferencedCodes();",
        "UnknownImportedCodesStayExactAndSorted();",
        "AnalysisDoesNotMutateCallerOrdering();",
        "Pair(result.MissingDirectedPairs[0], 20, 10);",
        "Sequence(result.IntersectionOnlyCategoryCodes, 30);",
        "Sequence(result.UnreferencedCategoryRuleCodes, 40);",
        "Sequence(result.ObservedCategoryCodes, 1301, 1302);",
    ], "smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityCalculationMatrixDiagnosticsSmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: quantity matrix diagnostics contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    forbidden = [
        "settings.CategoryRules =",
        "settings.IntersectionRules =",
        "settings.NormalizeAndValidate();",
        "snapshot.IntersectionRules.Add",
        "snapshot.CategoryRules.Add",
        "new QuantityIntersectionRuleSetting",
        "(ElementCategory)",
        "Enum.IsDefined",
        "TryGetIntersectionRule(targetCode, sourceCode",
        "ProjectQuantityReportBuilder",
        "StructuralRegenerator",
        "ProjectState",
        "AuditTrail",
        "Solid3d",
        "Brep",
    ]
    present = [token for token in forbidden if token in code]
    if present:
        print("ERROR: matrix diagnostics mutates settings, infers mappings, repairs rules or crosses a prohibited boundary:")
        for item in present:
            print(" -", item)
        return 1

    if "Distinct()\n                .OrderBy(x => x)" not in code:
        print("ERROR: observed category codes must be deduplicated and deterministically sorted.")
        return 1

    print("PASS: quantity matrix diagnostics is defensive, deterministic, directed, exact-code-preserving and read-only without rule synthesis.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
