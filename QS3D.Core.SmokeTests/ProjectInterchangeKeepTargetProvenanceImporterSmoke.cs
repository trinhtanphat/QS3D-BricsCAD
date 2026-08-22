using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeKeepTargetProvenanceImporterSmoke
    {
        internal static void Run()
        {
            PlanMapsOnlyActuallyAddedSourceElements();
            ImportKeepsCollisionAndMapsOnlyAppend();
            MissingSourceFingerprintFailsBeforeMutation();
            AllCollisionsProduceNoFalseTargetLineage();
        }

        private static void PlanMapsOnlyActuallyAddedSourceElements()
        {
            var target = TargetProject();
            var updated = new DateTime(2026, 8, 11, 1, 10, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var plan = ProjectInterchangeKeepTargetProvenanceImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeNew: true)));

            Equal(1, plan.SemanticPlan.ElementsToAdd);
            Equal(1, plan.SemanticPlan.ElementsToKeep);
            Equal(1, plan.AddedElementMappingCount);
            Equal(1, plan.CollidedSourceElementsWithoutTargetLineage);
            Equal("E2", plan.AddedElementMappings["E2"]);
            False(plan.AddedElementMappings.ContainsKey("E1"));
            Equal(2, plan.ProvenanceHandleCount);
            Equal(1, target.Elements.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportKeepsCollisionAndMapsOnlyAppend()
        {
            var target = TargetProject();
            var original = target.FindElement("E1") ?? throw new Exception("Target collision element missing.");
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeNew: true));

            var result = ProjectInterchangeKeepTargetProvenanceImporter.Import(target, json);

            Equal(1, result.SemanticResult.ElementsAdded);
            Equal(1, result.SemanticResult.TargetIdentitiesKept);
            Equal(2, result.ProvenanceResult.SourceHandlesStored);
            Equal(1, result.TargetMapResult.MappingsStored);
            Equal(1, result.CollidedSourceElementsWithoutTargetLineage);

            True(ReferenceEquals(original, target.FindElement("E1")));
            Equal("TARGET", original.Properties["Mark"]);
            Equal("TARGET-H", original.SourceHandles.Single());
            Equal("TARGET-DWG", original.DrawingFingerprint);

            var added = target.FindElement("E2") ?? throw new Exception("KeepTarget appended element missing.");
            Equal("SOURCE-2", added.Properties["Mark"]);
            Equal("E1", added.DependsOn.Single());
            Equal(0, added.SourceHandles.Count);
            Equal(string.Empty, added.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, added.Dirty);

            Equal("SOURCE-H1", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E1")));
            Equal("SOURCE-H2", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E2")));
            Equal(string.Empty, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E1"));
            Equal("E2", ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E2"));

            Equal(ProjectInterchangeKeepTargetProvenanceImporter.ImportMode, target.Metadata["Interchange.LastImport.Mode"]);
            Equal("1", target.Metadata[ProjectInterchangeKeepTargetProvenanceImporter.LastAddedMappingCountKey]);
            Equal("1", target.Metadata[ProjectInterchangeKeepTargetProvenanceImporter.LastCollidedWithoutLineageKey]);
            Equal("2", target.Metadata[ProjectInterchangeKeepTargetProvenanceImporter.LastProvenanceHandleCountKey]);
            Equal("ImportInterchangeKeepTargetWithSourceHandleProvenance", target.AuditEvents.Last().Action);

            var portableTarget = ProjectInterchangeJsonExporter.Build(target);
            False(portableTarget.Contains("SOURCE-H1", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains("SOURCE-H2", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains(ProjectInterchangeProvenanceTargetMap.MetadataPrefix, StringComparison.Ordinal));
        }

        private static void MissingSourceFingerprintFailsBeforeMutation()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false, includeNew: true));
            Throws<InvalidOperationException>(() => ProjectInterchangeKeepTargetProvenanceImporter.Plan(target, json));
            Throws<InvalidOperationException>(() => ProjectInterchangeKeepTargetProvenanceImporter.Import(target, json));
            Equal(1, target.Elements.Count);
            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Target missing.")).Properties["Mark"]);
        }

        private static void AllCollisionsProduceNoFalseTargetLineage()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeNew: false));
            var result = ProjectInterchangeKeepTargetProvenanceImporter.Import(target, json);

            Equal(0, result.SemanticResult.ElementsAdded);
            Equal(1, result.CollidedSourceElementsWithoutTargetLineage);
            Equal(0, result.TargetMapResult.MappingsStored);
            Equal(string.Empty, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E1"));
            Equal("SOURCE-H1", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E1")));
            Equal("TARGET-H", (target.FindElement("E1") ?? throw new Exception("Target missing.")).SourceHandles.Single());
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
            target.Elements.Add(element);
            return target;
        }

        private static ProjectState SourceProject(bool withFingerprint, bool includeNew)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 1, 5, 0, DateTimeKind.Utc)
            };
            var collision = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            collision.SourceHandles.Add("SOURCE-H1");
            collision.Properties["Mark"] = "SOURCE-1";
            source.Elements.Add(collision);

            if (includeNew)
            {
                var added = new ProjectElement("E2", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
                {
                    DrawingFingerprint = source.DrawingFingerprint
                };
                added.SourceHandles.Add("SOURCE-H2");
                added.DependsOn.Add("E1");
                added.Properties["Mark"] = "SOURCE-2";
                source.Elements.Add(added);
            }
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

    internal static class ProjectInterchangeKeepTargetProvenanceImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeKeepTargetProvenanceImporterSmoke.Run();
    }
}
