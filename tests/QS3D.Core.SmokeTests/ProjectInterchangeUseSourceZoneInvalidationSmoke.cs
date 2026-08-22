using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceZoneInvalidationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ReplacedZoneInvalidatesReferencedTargetElements();
        }

        private static void ReplacedZoneInvalidatesReferencedTargetElements()
        {
            var target = new ProjectState("TARGET-P", "Target")
            {
                DrawingFingerprint = "TARGET-ZONE-INVALIDATION-DWG"
            };
            target.Zones.Add(new ZoneDefinition("Z1", "Target Zone"));
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, "Z1");
            element.Properties["GeneratedSolidHandle"] = "AA11";
            target.Elements.Add(element);

            var source = new ProjectState("SOURCE-P", "Source");
            source.Zones.Add(new ZoneDefinition("Z1", "Source Zone"));
            var json = ProjectInterchangeJsonExporter.Build(source);

            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            Equal(1, plan.ZonesToReplace);
            True(plan.AffectedTargetElementIds.Contains("E1", StringComparer.OrdinalIgnoreCase));
            True(plan.TargetElementIdsRequiringNativeCleanup.Contains("E1", StringComparer.OrdinalIgnoreCase));
            Equal(1, plan.TargetGeneratedHandlesToClean);

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(
                    target,
                    json,
                    ProjectInterchangeNativeCleanupAuthorization.None));
            Equal("Target Zone", target.FindZone("Z1")?.Name);
            Equal("AA11", element.Properties["GeneratedSolidHandle"]);

            var result = ProjectInterchangeUseSourceSemanticImporter.Import(
                target,
                json,
                ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan));

            Equal("Source Zone", target.FindZone("Z1")?.Name);
            True(!element.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(ElementDirtyFlags.All, element.Dirty);
            Equal(1, result.AffectedTargetElementsMarkedDirty);
            Equal(1, result.NativeCleanupElementsAuthorized);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeUseSourceZoneInvalidationSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value)
                throw new InvalidOperationException("ProjectInterchangeUseSourceZoneInvalidationSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("ProjectInterchangeUseSourceZoneInvalidationSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
