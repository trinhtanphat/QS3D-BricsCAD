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
        'CategoryRules = (CategoryRules ?? new List<QuantityCategoryRuleSetting>()).Select(CloneCategoryRule).ToList()',
        'IntersectionRules = (IntersectionRules ?? new List<QuantityIntersectionRuleSetting>()).Select(CloneIntersectionRule).ToList()',
        'private static QuantityCategoryRuleSetting CloneCategoryRule(QuantityCategoryRuleSetting? rule)',
        'private static QuantityIntersectionRuleSetting CloneIntersectionRule(QuantityIntersectionRuleSetting? rule)',
        'throw new InvalidOperationException(NullCategoryRuleMessage);',
        'throw new InvalidOperationException(NullIntersectionRuleMessage);',
        'if (rule == null) throw new InvalidOperationException(NullCategoryRuleMessage);',
        'if (rule == null) throw new InvalidOperationException(NullIntersectionRuleMessage);',
    ]
    required_smoke = [
        'ValidRulesAreDeepCloned();',
        'NullCollectionsRetainEmptyCloneBehavior();',
        'NullCategoryEntriesFailExplicitly();',
        'NullIntersectionEntriesFailExplicitly();',
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
    if "Select(CloneCategoryRule)" not in clone_body or "Select(CloneIntersectionRule)" not in clone_body:
        print("ERROR: Clone() must route both rule lists through explicit null guards.")
        return 1

    print("PASS: QuantityCalculationSettings clone paths fail explicitly on null rule entries, preserve deep clones for valid rules, and remain covered by module-registered smoke tests.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
