using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceFamilyInheritanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ReplacementPreservesOverridesAndPropagatesInheritedDefaults();
        }

        private static void ReplacementPreservesOverridesAndPropagatesInheritedDefaults()
        {
            var target = new ProjectState("TARGET-FAMILY-INHERITANCE", "Target family inheritance");
            var family = new ProjectFamily("FAM-1", "Target Beam", ElementCategory.Beam);
            family.Properties["WidthM"] = "0.4";
            family.Properties["Material"] = "C30";
            family.Properties["LegacyDefault"] = "legacy";
            target.Families.Add(family);

            var inherited = new ProjectElement("E-INHERITED", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            inherited.Properties["WidthM"] = "0.4";
            inherited.Properties["Material"] = "C30";
            inherited.Properties["LegacyDefault"] = "legacy";
            inherited.Properties["GeneratedSolidHandle"] = "AA11";
            inherited.MarkClean(ElementDirtyFlags.All);
            target.Elements.Add(inherited);

            var overridden = new ProjectElement("E-OVERRIDE", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            overridden.Properties["WidthM"] = "0.4";
            overridden.Properties["Material"] = "INSTANCE-SPECIAL";
            overridden.Properties["LegacyDefault"] = "INSTANCE-LEGACY";
            overridden.MarkClean(ElementDirtyFlags.All);
            target.Elements.Add(overridden);

            var source = new ProjectState("SOURCE-FAMILY-INHERITANCE", "Source family inheritance");
            var sourceFamily = new ProjectFamily("FAM-1", "Source Beam", ElementCategory.Beam);
            sourceFamily.Properties["WidthM"] = "0.5";
            sourceFamily.Properties["Material"] = "C40";
            sourceFamily.Properties["DepthM"] = "0.6";
            source.Families.Add(sourceFamily);

            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);

            True(plan.AffectedTargetElementIds.Contains(inherited.Id, StringComparer.OrdinalIgnoreCase));
            True(plan.AffectedTargetElementIds.Contains(overridden.Id, StringComparer.OrdinalIgnoreCase));
            True(plan.TargetElementIdsRequiringNativeCleanup.Contains(inherited.Id, StringComparer.OrdinalIgnoreCase));

            var result = ProjectInterchangeUseSourceSemanticImporter.Import(
                target,
                json,
                ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan));

            Equal(1, result.FamiliesReplaced);
            Equal("Source Beam", family.Name);
            Equal("0.5", family.Properties["WidthM"]);
            Equal("C40", family.Properties["Material"]);
            Equal("0.6", family.Properties["DepthM"]);
            True(!family.Properties.ContainsKey("LegacyDefault"));

            Equal("0.5", inherited.Properties["WidthM"]);
            Equal("C40", inherited.Properties["Material"]);
            Equal("0.6", inherited.Properties["DepthM"]);
            True(!inherited.Properties.ContainsKey("LegacyDefault"));
            True(!inherited.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(ElementDirtyFlags.All, inherited.Dirty);

            Equal("0.5", overridden.Properties["WidthM"]);
            Equal("INSTANCE-SPECIAL", overridden.Properties["Material"]);
            Equal("0.6", overridden.Properties["DepthM"]);
            Equal("INSTANCE-LEGACY", overridden.Properties["LegacyDefault"]);
            Equal(ElementDirtyFlags.All, overridden.Dirty);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeUseSourceFamilyInheritanceSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeUseSourceFamilyInheritanceSmoke assertion failed.");
        }
    }
}
