using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Legacy;
using QS3D.Core.Model;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationBltCompatibilityPresetSmoke
    {
        internal static void Run()
        {
            var preset = QuantityCalculationBltCompatibilityPreset.Create();
            Equal(QuantityCalculationSettings.CurrentSchemaVersion, preset.SchemaVersion, "schema");
            Equal(28, preset.CategoryRules.Count, "category rule count");
            Equal(784, preset.IntersectionRules.Count, "intersection rule count");
            Near(10d, preset.FormworkTolerance, "formwork tolerance");
            Near(100d, preset.BlindingConcreteOffset, "blinding offset");
            Near(10d, preset.MinSubtractAreaMm2, "minimum subtract area");
            Near(1000d, preset.MinFormworkAreaMm2, "minimum formwork area");
            Near(0.0001d, preset.MinConcreteVolumeM3, "minimum concrete volume");
            Near(1d, preset.EngulfRelPercent, "engulf relative percent");
            Near(1000d, preset.EngulfMinAreaMm2, "engulf minimum area");
            Near(50d, preset.RoomGapFillMm, "room gap fill");
            Near(40000d, preset.RoomSearchRadiusMm, "room search radius");
            Equal("#FFFFFF", preset.DimColor, "dimension color");
            Near(30d, preset.DimTextHeight, "dimension text height");

            var room = RequireCategory(preset, 201);
            True(room.ExtractSide && room.ExtractBottom, "201 extraction flags");
            var floorFinish = RequireCategory(preset, 202);
            True(!floorFinish.ExtractSide && !floorFinish.ExtractBottom, "202 extraction flags");
            var ramp = RequireCategory(preset, 501);
            True(ramp.ExtractSide && !ramp.ExtractBottom, "501 extraction flags");

            AssertRule(preset, 201, 207, false, true, true, false, false);
            AssertRule(preset, 207, 201, true, true, true, true, true);
            AssertRule(preset, 701, 601, false, true, true, false, false);
            AssertRule(preset, 601, 701, true, true, true, true, true);
            AssertRule(preset, 1301, 1302, false, true, true, false, false);
            AssertRule(preset, 1302, 1301, true, true, true, true, true);

            var native = QuantityCalculationSettings.CreateDefault();
            True(native.CategoryRules.All(x => x.Category != 201), "native default must not be replaced by BLT codes");
            True(native.IntersectionRules.All(IsConservative), "native default intersections remain conservative");

            ExplicitCategoryCodeRequiresExactKeyAlias();
            CategoryTextMetadataKeyRequiresExplicitBoundary();
            LegacyMetricRequiresExactKeyAlias();
        }

        private static void ExplicitCategoryCodeRequiresExactKeyAlias()
        {
            var lookalikeKeys = new[] { "NotCategory", "SubCategory", "CategorySuffix", "MyCategoryCode" };
            for (var i = 0; i < lookalikeKeys.Length; i++)
            {
                var snapshot = CreateBltSnapshot("LOOKALIKE-" + i);
                snapshot.Metadata[lookalikeKeys[i]] = "601";
                var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
                True(!candidate.Category.HasValue, lookalikeKeys[i] + " must not create explicit category-code evidence");
            }

            var embeddedLookalike = CreateBltSnapshot("EMBEDDED-LOOKALIKE");
            embeddedLookalike.Metadata["LegacyProbe.XData.000.Value"] = "BLT3D; NotCategory=601";
            True(!BltLegacyEntityAdapter.Adapt(embeddedLookalike).Category.HasValue,
                "embedded lookalike category key must not create explicit category-code evidence");

            var exactCategory = CreateBltSnapshot("EXACT-CATEGORY");
            exactCategory.Metadata["Category"] = "601";
            True(BltLegacyEntityAdapter.Adapt(exactCategory).Category == ElementCategory.Column,
                "exact Category key must preserve established code 601");

            var exactCategoryCode = CreateBltSnapshot("EXACT-CATEGORYCODE");
            exactCategoryCode.Metadata["Category Code"] = "701";
            True(BltLegacyEntityAdapter.Adapt(exactCategoryCode).Category == ElementCategory.StructuralWall,
                "normalized exact CategoryCode key must preserve established code 701");

            var embeddedExact = CreateBltSnapshot("EMBEDDED-EXACT");
            embeddedExact.Metadata["LegacyProbe.XData.000.Value"] = "BLT3D; Category=601";
            True(BltLegacyEntityAdapter.Adapt(embeddedExact).Category == ElementCategory.Column,
                "embedded exact Category key must preserve established code 601");
        }

        private static void CategoryTextMetadataKeyRequiresExplicitBoundary()
        {
            var lookalikeKeys = new[] { "ColumnSpacing", "BeamLength", "StructuralWallThickness" };
            for (var i = 0; i < lookalikeKeys.Length; i++)
            {
                var snapshot = CreateBltSnapshot("CATEGORY-TEXT-LOOKALIKE-" + i);
                snapshot.Metadata[lookalikeKeys[i]] = "400";
                var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
                True(!candidate.Category.HasValue,
                    lookalikeKeys[i] + " must not create category text evidence from a compound metadata key");
            }

            var exactKey = CreateBltSnapshot("CATEGORY-TEXT-EXACT-KEY");
            exactKey.Metadata["Column"] = "present";
            True(BltLegacyEntityAdapter.Adapt(exactKey).Category == ElementCategory.Column,
                "exact category-bearing metadata key must remain supported");

            var bltPrefixedKey = CreateBltSnapshot("CATEGORY-TEXT-BLT-KEY");
            bltPrefixedKey.Metadata["BLTColumnData"] = "present";
            True(BltLegacyEntityAdapter.Adapt(bltPrefixedKey).Category == ElementCategory.Column,
                "BLT-prefixed category metadata key must remain supported");

            var categoryValue = CreateBltSnapshot("CATEGORY-TEXT-VALUE");
            categoryValue.Metadata["LegacyObjectKind"] = "StructuralWall";
            True(BltLegacyEntityAdapter.Adapt(categoryValue).Category == ElementCategory.StructuralWall,
                "category-bearing metadata value must remain supported");

            var runtimeClass = new EntitySnapshot("CATEGORY-TEXT-RUNTIME", "BLTBeamProxy", "LEGACY");
            runtimeClass.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();
            True(BltLegacyEntityAdapter.Adapt(runtimeClass).Category == ElementCategory.Beam,
                "BLT-prefixed runtime class must remain supported");

            var ambiguous = CreateBltSnapshot("CATEGORY-TEXT-AMBIGUOUS");
            ambiguous.Metadata["Column"] = "present";
            ambiguous.Metadata["Beam"] = "present";
            True(!BltLegacyEntityAdapter.Adapt(ambiguous).Category.HasValue,
                "multiple explicit category metadata keys must remain fail-closed as ambiguous");
        }

        private static void LegacyMetricRequiresExactKeyAlias()
        {
            var concreteLookalike = CreateBltSnapshot("LOOKALIKE-CONCRETE");
            concreteLookalike.Metadata["NotConcreteM3"] = "1.25";
            var concreteCandidate = BltLegacyEntityAdapter.Adapt(concreteLookalike);
            True(!concreteCandidate.LegacyConcreteM3.HasValue,
                "NotConcreteM3 must not create explicit legacy concrete quantity evidence");
            True(concreteCandidate.EvidenceMode == BltLegacyEvidenceMode.ExactGeometry,
                "concrete lookalike key must not promote ExactGeometry to ExactLegacyQuantity");

            var formworkLookalike = CreateBltSnapshot("LOOKALIKE-FORMWORK");
            formworkLookalike.Metadata["MyFormworkM2Suffix"] = "2.5";
            var formworkCandidate = BltLegacyEntityAdapter.Adapt(formworkLookalike);
            True(!formworkCandidate.LegacyFormworkM2.HasValue,
                "MyFormworkM2Suffix must not create explicit legacy formwork quantity evidence");
            True(formworkCandidate.EvidenceMode == BltLegacyEvidenceMode.ExactGeometry,
                "formwork lookalike key must not promote ExactGeometry to ExactLegacyQuantity");

            var embeddedLookalike = CreateBltSnapshot("EMBEDDED-METRIC-LOOKALIKE");
            embeddedLookalike.Metadata["LegacyProbe.XData.000.Value"] = "BLT3D; NotConcreteM3=1.75";
            var embeddedCandidate = BltLegacyEntityAdapter.Adapt(embeddedLookalike);
            True(!embeddedCandidate.LegacyConcreteM3.HasValue,
                "embedded concrete lookalike key must not create explicit legacy quantity evidence");
            True(embeddedCandidate.EvidenceMode == BltLegacyEvidenceMode.ExactGeometry,
                "embedded metric lookalike key must not promote ExactGeometry to ExactLegacyQuantity");

            var exactConcrete = CreateBltSnapshot("EXACT-CONCRETE");
            exactConcrete.Metadata["Concrete M3"] = "1.25";
            var exactConcreteCandidate = BltLegacyEntityAdapter.Adapt(exactConcrete);
            True(exactConcreteCandidate.LegacyConcreteM3.HasValue,
                "normalized exact ConcreteM3 alias must remain supported");
            Near(1.25d, exactConcreteCandidate.LegacyConcreteM3.GetValueOrDefault(), "exact ConcreteM3 quantity");
            True(exactConcreteCandidate.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity,
                "exact ConcreteM3 alias must retain ExactLegacyQuantity evidence");
            exactConcrete.Metadata.Remove("Concrete M3");
            var canonicalConcreteCandidate = BltLegacyEntityAdapter.Adapt(exactConcrete);
            True(canonicalConcreteCandidate.LegacyConcreteM3.HasValue,
                "canonical BLT.LegacyConcreteM3 must remain valid exact metric evidence on re-adaptation");
            Near(1.25d, canonicalConcreteCandidate.LegacyConcreteM3.GetValueOrDefault(),
                "canonical BLT.LegacyConcreteM3 quantity");

            var exactNetConcrete = CreateBltSnapshot("EXACT-NET-CONCRETE");
            exactNetConcrete.Metadata["NetConcreteM3"] = "1.5";
            var exactNetCandidate = BltLegacyEntityAdapter.Adapt(exactNetConcrete);
            True(exactNetCandidate.LegacyConcreteM3.HasValue,
                "exact NetConcreteM3 alias must remain supported");
            Near(1.5d, exactNetCandidate.LegacyConcreteM3.GetValueOrDefault(), "exact NetConcreteM3 quantity");

            var exactFormwork = CreateBltSnapshot("EXACT-FORMWORK");
            exactFormwork.Metadata["FormworkM2"] = "2.5";
            var exactFormworkCandidate = BltLegacyEntityAdapter.Adapt(exactFormwork);
            True(exactFormworkCandidate.LegacyFormworkM2.HasValue,
                "exact FormworkM2 alias must remain supported");
            Near(2.5d, exactFormworkCandidate.LegacyFormworkM2.GetValueOrDefault(), "exact FormworkM2 quantity");
            exactFormwork.Metadata.Remove("FormworkM2");
            var canonicalFormworkCandidate = BltLegacyEntityAdapter.Adapt(exactFormwork);
            True(canonicalFormworkCandidate.LegacyFormworkM2.HasValue,
                "canonical BLT.LegacyFormworkM2 must remain valid exact metric evidence on re-adaptation");
            Near(2.5d, canonicalFormworkCandidate.LegacyFormworkM2.GetValueOrDefault(),
                "canonical BLT.LegacyFormworkM2 quantity");
        }

        private static EntitySnapshot CreateBltSnapshot(string id)
        {
            var snapshot = new EntitySnapshot(id, "ProxyEntity", "LEGACY");
            snapshot.Metadata["LegacyProbe.ProxyOriginalClass"] = "BLT_OBJECT";
            snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();
            return snapshot;
        }

        private static QuantityCategoryRuleSetting RequireCategory(QuantityCalculationSettings settings, int code)
        {
            var rule = settings.FindCategoryRule(code);
            if (rule == null) throw new InvalidOperationException("Missing BLT category rule " + code + ".");
            Near(30d, rule.FaceAngleThresholdDeg, code + " face threshold");
            return rule;
        }

        private static void AssertRule(
            QuantityCalculationSettings settings,
            int source,
            int target,
            bool concrete,
            bool sideByConcrete,
            bool bottomByConcrete,
            bool sideBySide,
            bool bottomByBottom)
        {
            var rule = settings.FindIntersectionRule(source, target);
            if (rule == null) throw new InvalidOperationException("Missing BLT intersection rule " + source + "->" + target + ".");
            True(rule.SubtractConcrete == concrete, source + "->" + target + " concrete");
            True(rule.SubtractSideFormworkByConcrete == sideByConcrete, source + "->" + target + " side/concrete");
            True(rule.SubtractBottomFormworkByConcrete == bottomByConcrete, source + "->" + target + " bottom/concrete");
            True(rule.SubtractSideFormworkBySideFormwork == sideBySide, source + "->" + target + " side/side");
            True(rule.SubtractBottomFormworkByBottomFormwork == bottomByBottom, source + "->" + target + " bottom/bottom");
        }

        private static bool IsConservative(QuantityIntersectionRuleSetting rule)
        {
            return !rule.SubtractConcrete &&
                   !rule.SubtractSideFormworkByConcrete &&
                   !rule.SubtractBottomFormworkByConcrete &&
                   !rule.SubtractSideFormworkBySideFormwork &&
                   !rule.SubtractBottomFormworkByBottomFormwork;
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("BLT quantity preset regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException("BLT quantity preset regression: " + message + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("BLT quantity preset regression: " + message + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException("BLT quantity preset regression: " + message + ".");
        }
    }
}