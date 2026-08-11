using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceProvenanceImporterSmoke
    {
        internal static void Run()
        {
            PlanPreservesCleanupRequirementAndIdentityMapping();
            MissingCleanupAuthorizationFailsBeforeProvenanceMutation();
            AuthorizedImportRetainsProvenanceWithoutCadOwnership();
            MissingSourceFingerprintFailsBeforeMutation();
        }

        private static void PlanPreservesCleanupRequirementAndIdentityMapping()
        {
            var target = TargetProject();
            var updated = new DateTime(2026, 8, 11, 1, 0, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var plan = ProjectInterchangeUseSourceProvenanceImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true)));

            True(plan.RequiresNativeCleanup);
            Equal(1, plan.SemanticPlan.TargetElementIdsRequiringNativeCleanup.Count);
            Equal("E1", plan.SemanticPlan.TargetElementIdsRequiringNativeCleanup[0]);
            Equal(2, plan.MappingCount);
            Equal("E1", plan.ElementMappings["E1"]);
            Equal("E2", plan.ElementMappings["E2"]);
            Equal(2, plan.ProvenanceHandleCount);
            Equal(1, target.Elements.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void MissingCleanupAuthorizationFailsBeforeProvenanceMutation()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true));
            var original = target.FindElement("E1") ?? throw new Exception("Target element missing.");

            Throws<InvalidOperationException>(() => ProjectInterchangeUseSourceProvenanceImporter.Import(
                target,
                json,
                ProjectInterchangeNativeCleanupAuthorization.None));

            Equal("TARGET", original.Properties["Mark"]);
            Equal("AA11", original.Properties["GeneratedSolidHandle"]);
            Equal("TARGET-H", original.SourceHandles.Single());
            Equal("TARGET-DWG", original.DrawingFingerprint);
            Equal(string.Empty, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E1"));
            Equal(0, ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E1").Count);
        }

        private static void AuthorizedImportRetainsProvenanceWithoutCadOwnership()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true));
            var plan = ProjectInterchangeUseSourceProvenanceImporter.Plan(target, json);
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan.SemanticPlan);

            var result = ProjectInterchangeUseSourceProvenanceImporter.Import(target, json, authorization);

            Equal(1, result.SemanticResult.ElementsReplaced);
            Equal(1, result.SemanticResult.ElementsAdded);
            Equal(2, result.ProvenanceResult.SourceHandlesStored);
            Equal(2, result.TargetMapResult.MappingsStored);

            var replaced = target.FindElement("E1") ?? throw new Exception("Replaced target missing.");
            var added = target.FindElement("E2") ?? throw new Exception("Added source element missing.");
            Equal("SOURCE-1", replaced.Properties["Mark"]);
            False(replaced.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(0, replaced.SourceHandles.Count);
            Equal(string.Empty, replaced.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, replaced.Dirty);
            Equal(0, added.SourceHandles.Count);
            Equal(string.Empty, added.DrawingFingerprint);
            Equal("E1", added.DependsOn.Single());

            Equal("SOURCE-H1", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E1")));
            Equal("SOURCE-H2", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E2")));
            Equal("E1", ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E1"));
            Equal("E2", ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E2"));
            Equal(ProjectInterchangeUseSourceProvenanceImporter.ImportMode, target.Metadata["Interchange.LastImport.Mode"]);
            Equal("2", target.Metadata[ProjectInterchangeUseSourceProvenanceImporter.LastMappingCountKey]);
            Equal("2", target.Metadata[ProjectInterchangeUseSourceProvenanceImporter.LastProvenanceHandleCountKey]);
            Equal("ImportInterchangeUseSourceWithSourceHandleProvenance", target.AuditEvents.Last().Action);

            var portableTarget = ProjectInterchangeJsonExporter.Build(target);
            False(portableTarget.Contains("SOURCE-H1", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains("SOURCE-H2", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains(ProjectInterchangeProvenanceTargetMap.MetadataPrefix, StringComparison.Ordinal));
        }

        private static void MissingSourceFingerprintFailsBeforeMutation()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false));
            Throws<InvalidOperationException>(() => ProjectInterchangeUseSourceProvenanceImporter.Plan(target, json));
            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Target element missing.")).Properties["Mark"]);
            Equal(1, target.Elements.Count);
        }

        private static ProjectState TargetProject()
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = "TARGET-DWG"
            };
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
            {
                DrawingFingerprint = target.DrawingFingerprint
            };
            element.SourceHandles.Add("TARGET-H");
            element.Properties["Mark"] = "TARGET";
            element.Properties["GeneratedSolidHandle"] = "AA11";
            element.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            target.Elements.Add(element);
            return target;
        }

        private static ProjectState SourceProject(bool withFingerprint)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 0, 58, 0, DateTimeKind.Utc)
            };
            var existing = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            existing.SourceHandles.Add("SOURCE-H1");
            existing.Properties["Mark"] = "SOURCE-1";
            source.Elements.Add(existing);

            var added = new ProjectElement("E2", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            added.SourceHandles.Add("SOURCE-H2");
            added.DependsOn.Add("E1");
            added.Properties["Mark"] = "SOURCE-2";
            source.Elements.Add(added);
            return source;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeUseSourceProvenanceImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeUseSourceProvenanceImporterSmoke.Run();
    }
}