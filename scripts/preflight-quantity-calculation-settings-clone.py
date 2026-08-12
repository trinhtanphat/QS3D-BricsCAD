#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationSettings.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCloneValidationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationSettingsCloneValidationSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        'private const string NullCategoryRuleMessage = "CategoryRules cannot contain null entries.";',
        'private const string NullIntersectionRuleMessage = "IntersectionRules cannot contain null entries.";',
        'var categoryRules = CategoryRules ?? new List<QuantityCategoryRuleSetting>();',
        'var intersectionRules = IntersectionRules ?? new List<QuantityIntersectionRuleSetting>();',
        'RequireCollectionCardinality(categoryRules, intersectionRules);',
        'CategoryRules = categoryRules.Select(CloneCategoryRule).ToList()',
        'IntersectionRules = intersectionRules.Select(CloneIntersectionRule).ToList()',
        'private static QuantityCategoryRuleSetting CloneCategoryRule(QuantityCategoryRuleSetting? rule)',
        'private static QuantityIntersectionRuleSetting CloneIntersectionRule(QuantityIntersectionRuleSetting? rule)',
        'if (rule == null) throw new InvalidOperationException(NullCategoryRuleMessage);',
        'if (rule == null) throw new InvalidOperationException(NullIntersectionRuleMessage);',
        'private static void RequireCollectionCardinality(',
        'if (categoryRules.Count > MaxObservedCategoryCodeCount)',
        'if (intersectionRules.Count > MaxDirectedIntersectionRuleCount)',
    ]
    required_smoke = [
        'ValidRulesAreDeepCloned();',
        'NullCollectionsRetainEmptyCloneBehavior();',
        'NullCategoryEntriesFailExplicitly();',
        'NullIntersectionEntriesFailExplicitly();',
        'OversizedCategoryCollectionFailsBeforeEntryClone();',
        'OversizedIntersectionCollectionFailsBeforeEntryClone();',
        'settings.CategoryRules.Add(null!);',
        'settings.IntersectionRules.Add(null!);',
        'new QuantityCalculationRuleSet(settings)',
    ]
    required_registration = [
        '[ModuleInitializer]',
        'QuantityCalculationSettingsCloneValidationSmoke.Run();',
    ]

    missing = ["source: " + token for token in required_source if token not in source]
    missing += ["smoke: " + token for token in required_smoke if token not in smoke]
    missing += ["registration: " + token for token in required_registration if token not in registration]
    if missing:
        print("ERROR: quantity settings clone-validation contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = [
        '.Select(x => x.Clone()).ToList()',
        '.Select(rule => rule.Clone()).ToList()',
    ]
    for token in unsafe:
        if token in source:
            print("ERROR: unsafe clone-before-null-guard pattern returned:", token)
            return 1

    clone_start = source.find("public QuantityCalculationSettings Clone()")
    validate_start = source.find("public void NormalizeAndValidate()", clone_start)
    if clone_start < 0 or validate_start < 0:
        print("ERROR: cannot isolate QuantityCalculationSettings.Clone().")
        return 1
    clone_body = source[clone_start:validate_start]
    category_snapshot = clone_body.find("var categoryRules = CategoryRules ??")
    intersection_snapshot = clone_body.find("var intersectionRules = IntersectionRules ??")
    cardinality = clone_body.find("RequireCollectionCardinality(categoryRules, intersectionRules);")
    category_clone = clone_body.find("categoryRules.Select(CloneCategoryRule)")
    intersection_clone = clone_body.find("intersectionRules.Select(CloneIntersectionRule)")
    if min(category_snapshot, intersection_snapshot, cardinality, category_clone, intersection_clone) < 0:
        print("ERROR: Clone() must snapshot, bound, then clone both rule collections.")
        return 1
    if not category_snapshot < cardinality or not intersection_snapshot < cardinality:
        print("ERROR: Clone() must snapshot both collections before cardinality validation.")
        return 1
    if not cardinality < category_clone or not cardinality < intersection_clone:
        print("ERROR: Clone() must reject oversized collections before per-entry cloning.")
        return 1

    print("PASS: QuantityCalculationSettings Clone bounds collection cardinality before per-entry cloning, fails explicitly on null rules, preserves deep clones for valid rules, and remains module-smoke covered.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
