#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationSettings.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCardinalitySmoke.cs"
CLONE_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCloneValidationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCardinalitySmokeRegistration.cs"


def require(text, tokens, label):
    return [label + ": " + token for token in tokens if token not in text]


def main():
    code = CODE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    clone_smoke = CLONE_SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    clone_start = code.find("public QuantityCalculationSettings Clone()")
    clone_end = code.find("public void NormalizeAndValidate()", clone_start)
    normalize_start = clone_end
    normalize_end = code.find("public QuantityCategoryRuleSetting? FindCategoryRule", normalize_start)
    helper_start = code.find("private static void RequireCollectionCardinality(", normalize_end)
    helper_end = code.find("private static void AddObservedCategoryCode", helper_start)
    if clone_start < 0 or clone_end <= clone_start:
        print("ERROR: cannot isolate QuantityCalculationSettings.Clone().")
        return 1
    if normalize_start < 0 or normalize_end <= normalize_start:
        print("ERROR: cannot isolate QuantityCalculationSettings.NormalizeAndValidate().")
        return 1
    if helper_start < 0 or helper_end <= helper_start:
        print("ERROR: cannot isolate shared collection-cardinality helper.")
        return 1

    clone = code[clone_start:clone_end]
    normalize = code[normalize_start:normalize_end]
    helper = code[helper_start:helper_end]

    missing = []
    missing += require(code, [
        "public const int MaxObservedCategoryCodeCount = 256;",
        "public const int MaxDirectedIntersectionRuleCount = MaxObservedCategoryCodeCount * MaxObservedCategoryCodeCount;",
        "private static void AddObservedCategoryCode(HashSet<int> observedCategoryCodes, int categoryCode)",
        "if (observedCategoryCodes.Count > MaxObservedCategoryCodeCount)",
        "private static long PairKey(int sourceCode, int targetCode)",
        "return ((long)(uint)sourceCode << 32) | (uint)targetCode;",
    ], "settings")
    missing += require(clone, [
        "var categoryRules = CategoryRules ?? new List<QuantityCategoryRuleSetting>();",
        "var intersectionRules = IntersectionRules ?? new List<QuantityIntersectionRuleSetting>();",
        "RequireCollectionCardinality(categoryRules, intersectionRules);",
        "CategoryRules = categoryRules.Select(CloneCategoryRule).ToList(),",
        "IntersectionRules = intersectionRules.Select(CloneIntersectionRule).ToList()",
    ], "clone")
    missing += require(normalize, [
        "RequireCollectionCardinality(CategoryRules, IntersectionRules);",
        "var observedCategoryCodes = new HashSet<int>();",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Category);",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Source);",
        "AddObservedCategoryCode(observedCategoryCodes, rule.Target);",
        "var pairs = new HashSet<long>();",
        "var key = PairKey(rule.Source, rule.Target);",
    ], "normalize")
    missing += require(helper, [
        "List<QuantityCategoryRuleSetting> categoryRules",
        "List<QuantityIntersectionRuleSetting> intersectionRules",
        "if (categoryRules.Count > MaxObservedCategoryCodeCount)",
        "if (intersectionRules.Count > MaxDirectedIntersectionRuleCount)",
    ], "shared cardinality helper")
    missing += require(smoke, [
        "DefaultSettingsRemainValid();",
        "ImportedTwentyEightCodeMatrixRemainsValid();",
        "ExactUnknownIntegerCodesRemainValid();",
        "ExactCategoryUniverseBoundaryRemainsValid();",
        "CategoryRuleOverflowFailsClosed();",
        "DirectedRuleCountOverflowFailsClosed();",
        "SparseDistinctObservedCodeOverflowFailsClosed();",
        "settings.CategoryRules.Add(CategoryRule(int.MaxValue));",
        "Throws<InvalidOperationException>(() => settings.Clone());",
        "QuantityCalculationSettings.MaxDirectedIntersectionRuleCount + 1",
        "settings.IntersectionRules.Count < QuantityCalculationSettings.MaxDirectedIntersectionRuleCount",
    ], "cardinality smoke")
    missing += require(clone_smoke, [
        "OversizedCategoryCollectionFailsBeforeEntryClone();",
        "OversizedIntersectionCollectionFailsBeforeEntryClone();",
        "ThrowsInvalid(() => settings.Clone(), CategoryLimitMessage);",
        "ThrowsInvalid(() => settings.Clone(), IntersectionLimitMessage);",
        "null!",
    ], "clone smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityCalculationSettingsCardinalitySmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: Quantity Settings clone/cardinality boundary is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    clone_guard = clone.find("RequireCollectionCardinality(categoryRules, intersectionRules);")
    category_clone = clone.find("categoryRules.Select(CloneCategoryRule).ToList()")
    intersection_clone = clone.find("intersectionRules.Select(CloneIntersectionRule).ToList()")
    if not (0 <= clone_guard < category_clone and clone_guard < intersection_clone):
        print("ERROR: Clone() must guard raw collection cardinality before deep-copy enumeration.")
        return 1

    normalize_guard = normalize.find("RequireCollectionCardinality(CategoryRules, IntersectionRules);")
    category_loop = normalize.find("foreach (var rule in CategoryRules)")
    intersection_loop = normalize.find("foreach (var rule in IntersectionRules)")
    if not (0 <= normalize_guard < category_loop and normalize_guard < intersection_loop):
        print("ERROR: NormalizeAndValidate() must guard collection cardinality before rule traversal.")
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
    present = [token for token in forbidden if token in clone or token in normalize]
    if present:
        print("ERROR: clone/cardinality validation inferred category semantics or mutated the rule payload:")
        for item in present:
            print(" -", item)
        return 1

    print("PASS: Quantity Settings cardinality is guarded before Clone deep-copy amplification and before validation traversal while exact unknown integer codes remain supported.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
