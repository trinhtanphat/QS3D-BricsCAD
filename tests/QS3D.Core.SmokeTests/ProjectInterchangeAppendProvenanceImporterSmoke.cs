using System;
using System.IO;
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
            ImportPreservesHandlesOnlyInLedger();
            MissingSourceFingerprintFailsBeforeMutation();
            CorruptExistingLedgerRollsBackSemanticAppend();
        }

        private static void PlanIsReadOnlyAndAccountsForHandles()
        {
            var target = new ProjectState("TARGET", "Target");
            var updated = new DateTime(2026, 8, 11, 0, 40, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var source = SourceProject(withFingerprint: true);

            var plan = ProjectInterchangeAppendProvenanceImporter.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            Equal(1, plan.SemanticPlan.ElementsToAdd);
            Equal(2, plan.SemanticPlan.SourceHandlesToDiscard);
            Equal(1, plan.ProvenanceRecordCount);
            Equal(2, plan.ProvenanceHandleCount);
            Equal(0, target.Elements.Count);
            Equal(0, target.Metadata.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportPreservesHandlesOnlyInLedger()
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = "TARGET-DWG"
            };
            var sourceJson = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true));

            var result = ProjectInterchangeAppendProvenanceImporter.Import(target, sourceJson);

            Equal(1, result.SemanticResult.ElementsAdded);
            Equal(2, result.SemanticResult.SourceHandlesDiscarded);
            Equal(1, result.ProvenanceRecordCount);
            Equal(2, result.ProvenanceHandleCount);

            var element = target.FindElement("E-SOURCE") ?? throw new Exception("Imported element missing.");
            Equal(0, element.SourceHandles.Count);
            Equal(string.Empty, element.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, element.Dirty);

            var ledger = ProjectInterchangeSourceHandleProvenanceStore.Load(target);
            Equal(1, ledger.Records.Count);
            Equal(2, ledger.HandleCount);
            var record = ledger.Records[0];
            Equal("SOURCE", record.SourceProjectId);
            Equal("SOURCE-DWG", record.SourceDrawingFingerprint);
            Equal("E-SOURCE", record.SourceElementId);
            Equal("E-SOURCE", record.TargetElementId);
            Equal("1A2B|3C4D", string.Join("|", record.SourceHandles));

            Equal(ProjectInterchangeAppendProvenanceImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal("1", target.Metadata[ProjectInterchangeAppendProvenanceImporter.LastProvenanceRecordCountKey]);
            Equal("2", target.Metadata[ProjectInterchangeAppendProvenanceImporter.LastProvenanceHandleCountKey]);
            Equal("ImportInterchangeAppendSourceHandleProvenance", target.AuditEvents.Last().Action);

            var portableTarget = ProjectInterchangeJsonExporter.Build(target);
            False(portableTarget.Contains("1A2B", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains("3C4D", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains(ProjectInterchangeSourceHandleProvenanceStore.MetadataKey, StringComparison.Ordinal));
        }

        private static void MissingSourceFingerprintFailsBeforeMutation()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false));

            Throws<InvalidOperationException>(() => ProjectInterchangeAppendProvenanceImporter.Plan(target, json));
            Throws<InvalidOperationException>(() => ProjectInterchangeAppendProvenanceImporter.Import(target, json));
            Equal(0, target.Elements.Count);
            Equal(0, target.Metadata.Count);
        }

        private static void CorruptExistingLedgerRollsBackSemanticAppend()
        {
            var target = new ProjectState("TARGET", "Target");
            target.Metadata[ProjectInterchangeSourceHandleProvenanceStore.MetadataKey] = "<broken";
            var beforeUpdated = new DateTime(2026, 8, 11, 0, 45, 0, DateTimeKind.Utc);
            target.UpdatedUtc = beforeUpdated;

            Throws<InvalidDataException>(() => ProjectInterchangeAppendProvenanceImporter.Import(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true))));

            Equal(0, target.Elements.Count);
            Equal("<broken", target.Metadata[ProjectInterchangeSourceHandleProvenanceStore.MetadataKey]);
            Equal(1, target.Metadata.Count);
            Equal(0, target.AuditEvents.Count);
            Equal(beforeUpdated, target.UpdatedUtc);
        }

        private static ProjectState SourceProject(bool withFingerprint)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 0, 35, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E-SOURCE", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.SourceHandles.Add("3C4D");
            element.SourceHandles.Add("1A2B");
            element.Properties["Mark"] = "B1";
            element.Quantities["LengthM"] = 4.5;
            source.Elements.Add(element);
            return source;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
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
