using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeAppendProvenanceImporterSmoke
    {
        internal static void Run()
        {
            PlanIsReadOnlyAndAccountsForHandles();
            ImportPreservesHandlesOnlyAsCanonicalProvenance();
            MissingSourceFingerprintFailsBeforeMutation();
            EmptyHandleSetDoesNotRequireFingerprint();
            ExistingSourceProvenanceIsReplacedByCombinedImport();
        }

        private static void PlanIsReadOnlyAndAccountsForHandles()
        {
            var target = new ProjectState("TARGET", "Target");
            var updated = new DateTime(2026, 8, 11, 0, 40, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            var plan = ProjectInterchangeAppendProvenanceImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeHandles: true)));

            Equal(1, plan.SemanticPlan.ElementsToAdd);
            Equal(2, plan.SemanticPlan.SourceHandlesToDiscard);
            Equal(1, plan.ProvenanceElementCount);
            Equal(2, plan.ProvenanceHandleCount);
            Equal("SOURCE-DWG", plan.ProvenancePlan.SourceDrawingFingerprint);
            Equal(0, target.Elements.Count);
            Equal(0, target.Metadata.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportPreservesHandlesOnlyAsCanonicalProvenance()
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = "TARGET-DWG"
            };
            var sourceJson = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeHandles: true));

            var result = ProjectInterchangeAppendProvenanceImporter.Import(target, sourceJson);

            Equal(1, result.SemanticResult.ElementsAdded);
            Equal(2, result.SemanticResult.SourceHandlesDiscarded);
            Equal(1, result.ProvenanceElementCount);
            Equal(2, result.ProvenanceHandleCount);

            var element = target.FindElement("E-SOURCE") ?? throw new Exception("Imported element missing.");
            Equal(0, element.SourceHandles.Count);
            Equal(string.Empty, element.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, element.Dirty);

            var handles = ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E-SOURCE");
            Equal("1A2B|3C4D", string.Join("|", handles));
            True(target.Metadata.Keys.Any(x => x.StartsWith(ProjectInterchangeSourceHandleProvenance.MetadataPrefix, StringComparison.OrdinalIgnoreCase)));

            Equal(ProjectInterchangeAppendProvenanceImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal("1", target.Metadata[ProjectInterchangeAppendProvenanceImporter.LastProvenanceElementCountKey]);
            Equal("2", target.Metadata[ProjectInterchangeAppendProvenanceImporter.LastProvenanceHandleCountKey]);
            Equal("ImportInterchangeAppendWithSourceHandleProvenance", target.AuditEvents.Last().Action);

            var portableTarget = ProjectInterchangeJsonExporter.Build(target);
            False(portableTarget.Contains("1A2B", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains("3C4D", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains(ProjectInterchangeSourceHandleProvenance.MetadataPrefix, StringComparison.Ordinal));
        }

        private static void MissingSourceFingerprintFailsBeforeMutation()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false, includeHandles: true));

            Throws<InvalidOperationException>(() => ProjectInterchangeAppendProvenanceImporter.Plan(target, json));
            Throws<InvalidOperationException>(() => ProjectInterchangeAppendProvenanceImporter.Import(target, json));
            Equal(0, target.Elements.Count);
            Equal(0, target.Metadata.Count);
            Equal(0, target.AuditEvents.Count);
        }

        private static void EmptyHandleSetDoesNotRequireFingerprint()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false, includeHandles: false));

            var plan = ProjectInterchangeAppendProvenanceImporter.Plan(target, json);
            Equal(0, plan.ProvenanceHandleCount);
            var result = ProjectInterchangeAppendProvenanceImporter.Import(target, json);
            Equal(0, result.ProvenanceHandleCount);
            Equal(1, target.Elements.Count);
        }

        private static void ExistingSourceProvenanceIsReplacedByCombinedImport()
        {
            var target = new ProjectState("TARGET", "Target");
            var oldSource = SourceProject(withFingerprint: true, includeHandles: true);
            oldSource.Elements[0].SourceHandles.Clear();
            oldSource.Elements[0].SourceHandles.Add("OLD1");
            ProjectInterchangeSourceHandleProvenance.Store(target, ProjectInterchangeJsonExporter.Build(oldSource));
            Equal("OLD1", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E-SOURCE")));

            var result = ProjectInterchangeAppendProvenanceImporter.Import(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true, includeHandles: true)));

            Equal(2, result.ProvenanceHandleCount);
            Equal("1A2B|3C4D", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E-SOURCE")));
            Equal(1, target.Elements.Count);
        }

        private static ProjectState SourceProject(bool withFingerprint, bool includeHandles)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 0, 35, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E-SOURCE", ElementCategory.Beam, string.Empty, string.Empty, string.Empty)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            if (includeHandles)
            {
                element.SourceHandles.Add("3C4D");
                element.SourceHandles.Add("1A2B");
            }
            element.Properties["Mark"] = "B1";
            element.Quantities["LengthM"] = 4.5;
            source.Elements.Add(element);
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

    internal static class ProjectInterchangeAppendProvenanceImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeAppendProvenanceImporterSmoke.Run();
    }
}
