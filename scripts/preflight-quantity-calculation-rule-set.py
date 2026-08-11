#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationRuleSet.cs"


def require(text, tokens, label):
    missing = [token for token in tokens if token not in text]
    if not missing:
        return []
    return [label + ": " + token for token in missing]


def main():
    text = CODE.read_text(encoding="utf-8")
    missing = require(text, [
        "public sealed class QuantityCalculationRuleSet",
        "_settings = settings.Clone();",
        "_settings.NormalizeAndValidate();",
        "public QuantityCalculationSettings Snapshot => _settings.Clone();",
        "public bool TryGetCategoryRule(int categoryCode",
        "public bool TryGetCategoryRule(ElementCategory category",
        "public bool TryGetIntersectionRule(int sourceCode, int targetCode",
        "public bool TryGetIntersectionRule(ElementCategory source, ElementCategory target",
        "case ElementCategory.Room: return new[] { native, 201 };",
        "case ElementCategory.FloorFinish: return new[] { native, 202 };",
        "case ElementCategory.Skirting: return new[] { native, 204 };",
        "case ElementCategory.WallFinish: return new[] { native, 205 };",
        "case ElementCategory.Railing: return new[] { native, 207 };",
        "case ElementCategory.Column: return new[] { native, 601 };",
        "case ElementCategory.StructuralWall: return new[] { native, 701 };",
        "default: return new[] { native };",
        "PairKey(sourceCode, targetCode)",
    ], "rule-set")
    if missing:
        print("ERROR: QuantityCalculationRuleSet contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    # The supplied BLT payload contains additional codes, but the existing source
    # does not establish exact native equivalence for these labels. Keep them
    # addressable only through integer-code lookup until the owner/reference data
    # supplies an explicit mapping contract.
    forbidden = [
        "case ElementCategory.Beam: return new[] { native, 301",
        "case ElementCategory.Beam: return new[] { native, 302",
        "case ElementCategory.Beam: return new[] { native, 703",
        "case ElementCategory.Slab: return new[] { native, 401",
        "case ElementCategory.Stair: return new[] { native, 501",
        "case ElementCategory.ArchitecturalWall: return new[] { native, 704",
        "TryGetIntersectionRule(targetCode, sourceCode",
        "new QuantityIntersectionRuleSetting { Source =",
        "new QuantityCategoryRuleSetting { Category =",
    ]
    present = [token for token in forbidden if token in text]
    if present:
        print("ERROR: runtime rule lookup inferred or synthesized semantics outside the established compatibility contract:")
        for item in present:
            print(" -", item)
        return 1

    category_method = text[text.find("public bool TryGetCategoryRule(ElementCategory category"):text.find("public bool TryGetIntersectionRule(int sourceCode")]
    if "foreach (var code in LookupCodes(category))" not in category_method:
        print("ERROR: native category lookup must use deterministic native-first lookup codes.")
        return 1

    intersection_method = text[text.find("public bool TryGetIntersectionRule(ElementCategory source"):text.find("private static int[] LookupCodes")]
    if "foreach (var sourceCode in sourceCodes)" not in intersection_method or "foreach (var targetCode in targetCodes)" not in intersection_method:
        print("ERROR: directed native/compatibility pair resolution is missing.")
        return 1
    if "TryGetIntersectionRule(sourceCode, targetCode, out rule)" not in intersection_method:
        print("ERROR: directed intersection lookup must preserve source -> target order.")
        return 1

    print("PASS: quantity calculation rule resolution is defensive, native-first, exact-label compatibility-limited, exact for unknown codes and directed without synthetic missing rules.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
