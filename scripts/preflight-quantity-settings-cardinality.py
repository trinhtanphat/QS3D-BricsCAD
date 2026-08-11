#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationSettings.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCardinalitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCardinalitySmokeRegistration.cs"


def require(text, tokens, label):
    return [label + ": " + token for token in tokens if token not in text]


def main():
    code = CODE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    normalize_start = code.find("public void NormalizeAndValidate()")
    normalize_end = code.find("public QuantityCategoryRuleSetting? FindCategoryRule", normalize_start)
    if normalize_start < 0 or normalize_end <= normalize_start:
        print("ERROR: cannot isolate QuantityCalculationSettings.NormalizeAndValidate().")
        return 1
    normalize = code[normalize_start:normalize_end]

    missing = []
    missing += require(code, [
        "public const int MaxObservedCategoryCodeCount = 256;",
        "public const int MaxDirectedIntersectionRuleCount = MaxObservedCategoryCodeCount * MaxObservedCategoryCodeCount;",
        "private static void AddObservedCategoryCode(HashSet<int> observedCategoryCodes, int categoryCode)",
        "if (observedCategoryCodes.Count > MaxObservedCategoryCodeCount)",
        "private static long PairKey(int sourceCode, int targetCode)",
        "return ((long)(uint)sourceCode << 32) | (uint)targetCode;",
    ], "settings")
    missing += require(normalize, [
        "if (CategoryRules.Count > MaxObservedCategoryCodeCount)",
        "if (IntersectionRules.Count > MaxDirectedIntersectionRuleCount)",
        "var observedCategoryCodes = new HashSet<int>();",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Category);",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Source);",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Target);",
        "var pairs = new HashSet<long>();",
        "var key = PairKey(rule.Source, rule.Target);",
    ], "normalize")
    missing += require(smoke, [
        "DefaultSettingsRemainValid();",
        "ImportedTwentyEightCodeMatrixRemainsValid();",
        "ExactUnknownIntegerCodesRemainValid();",
        "ExactCategoryUniverseBoundaryRemainsValid();",
        "CategoryRuleOverflowFailsClosed();",
        "DirectedRuleCountOverflowFailsClosed();",
        "SparseDistinctObservedCodeOverflowFailsClosed();",
        "settings.CategoryRules.Add(CategoryRule(int.MaxValue));",
        "QuantityCalculationSettings.MaxDirectedIntersectionRuleCount + 1",
        "settings.IntersectionRules.Count < QuantityCalculationSettings.MaxDirectedIntersectionRuleCount",
    ], "smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityCalculationSettingsCardinalitySmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: Quantity Settings cardinality boundary is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    category_guard = normalize.find("if (CategoryRules.Count > MaxObservedCategoryCodeCount)")
    category_loop = normalize.find("foreach (var rule in CategoryRules)")
    intersection_guard = normalize.find("if (IntersectionRules.Count > MaxDirectedIntersectionRuleCount)")
    intersection_loop = normalize.find("foreach (var rule in IntersectionRules)")
    if not (0 <= category_guard < category_loop):
        print("ERROR: CategoryRules cardinality must be rejected before category-rule traversal.")
        return 1
    if not (0 <= intersection_guard < intersection_loop):
        print("ERROR: IntersectionRules cardinality must be rejected before directed-rule traversal.")
        return 1

    forbidden = [
        "(ElementCategory)",
        "Enum.IsDefined",
        "CategoryRules.Add(",
        "IntersectionRules.Add(",
        "CategoryRules.Remove",
        "IntersectionRules.Remove",
        "CategoryRules.Clear",
        "IntersectionRules.Clear",
    ]
    present = [token for token in forbidden if token in normalize]
    if present:
        print("ERROR: cardinality validation inferred category semantics or mutated the rule payload:")
        for item in present:
            print(" -", item)
        return 1

    print("PASS: Quantity Settings cardinality is bounded before matrix amplification while exact unknown integer codes and directed rule semantics remain unchanged.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
