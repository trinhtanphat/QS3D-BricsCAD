using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeAppendProvenancePlan
    {
        internal ProjectInterchangeAppendProvenancePlan(
            ProjectInterchangeAppendOnlyImportPlan semanticPlan,
            int provenanceRecordCount,
            int provenanceHandleCount)
        {
            SemanticPlan = semanticPlan ?? throw new ArgumentNullException(nameof(semanticPlan));
            ProvenanceRecordCount = provenanceRecordCount;
            ProvenanceHandleCount = provenanceHandleCount;
        }

        public ProjectInterchangeAppendOnlyImportPlan SemanticPlan { get; }
        public int ProvenanceRecordCount { get; }
        public int ProvenanceHandleCount { get; }
    }

    public sealed class ProjectInterchangeAppendProvenanceResult
    {
        internal ProjectInterchangeAppendProvenanceResult(
            ProjectInterchangeAppendOnlyImportResult semanticResult,
            ProjectInterchangeAppendProvenancePlan plan)
        {
            SemanticResult = semanticResult ?? throw new ArgumentNullException(nameof(semanticResult));
            ProvenanceRecordCount = plan?.ProvenanceRecordCount ?? throw new ArgumentNullException(nameof(plan));
            ProvenanceHandleCount = plan.ProvenanceHandleCount;
        }

        public ProjectInterchangeAppendOnlyImportResult SemanticResult { get; }
        public int ProvenanceRecordCount { get; }
        public int ProvenanceHandleCount { get; }
    }

    public static class ProjectInterchangeAppendProvenanceImporter
    {
        public const string ImportMode = "AppendOnlyPreserveSourceHandleProvenance";
        public const string LastProvenanceRecordCountKey = "Interchange.LastImport.SourceHandleProvenanceRecords";
        public const string LastProvenanceHandleCountKey = "Interchange.LastImport.SourceHandlesPreservedAsProvenance";

        private sealed class PreparedImport
        {
            public PreparedImport(
                ProjectInterchangeValidatedSnapshot source,
                ProjectInterchangeAppendProvenancePlan plan,
                IReadOnlyList<ProjectInterchangeSourceHandleProvenanceRecord> provenanceRecords)
            {
                Source = source;
                Plan = plan;
                ProvenanceRecords = provenanceRecords;
            }

            public ProjectInterchangeValidatedSnapshot Source { get; }
            public ProjectInterchangeAppendProvenancePlan Plan { get; }
            public IReadOnlyList<ProjectInterchangeSourceHandleProvenanceRecord> ProvenanceRecords { get; }
        }

        public static ProjectInterchangeAppendProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static ProjectInterchangeAppendProvenanceResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var prepared = Prepare(target, json);
            var snapshot = ProjectStateSnapshot.Capture(target);
            try
            {
                var semanticResult = ProjectInterchangeAppendOnlyImporter.Import(target, json);
                EnsureImportedElementsDoNotOwnSourceCad(target, prepared.Source);
                ProjectInterchangeSourceHandleProvenanceStore.Append(target, prepared.ProvenanceRecords);

                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
                target.Metadata[LastProvenanceRecordCountKey] = prepared.Plan.ProvenanceRecordCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastProvenanceHandleCountKey] = prepared.Plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeAppendSourceHandleProvenance",
                    string.Empty,
                    "Preserved source CAD handles as non-owning provenance after append-only semantic import from project " + prepared.Source.Project.Id +
                    ": records=" + prepared.Plan.ProvenanceRecordCount.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + prepared.Plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture) + ".");

                return new ProjectInterchangeAppendProvenanceResult(semanticResult, prepared.Plan);
            }
            catch (Exception operationError)
            {
                try
                {
                    snapshot.Restore(target);
                }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Interchange append-with-provenance import failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            var semanticPlan = ProjectInterchangeAppendOnlyImporter.Plan(target, json);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var mapping = source.Elements.ToDictionary(x => x.Id, x => x.Id, StringComparer.OrdinalIgnoreCase);
            var records = ProjectInterchangeSourceHandleProvenanceStore.BuildRecords(source, mapping);
            var handleCount = records.Sum(x => x.SourceHandles.Count);
            if (handleCount != semanticPlan.SourceHandlesToDiscard)
                throw new InvalidOperationException("Append source-handle provenance accounting does not match the validated append-only source-handle count.");
            return new PreparedImport(
                source,
                new ProjectInterchangeAppendProvenancePlan(semanticPlan, records.Count, handleCount),
                records);
        }

        private static void EnsureImportedElementsDoNotOwnSourceCad(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source)
        {
            foreach (var sourceElement in source.Elements)
            {
                var targetElement = target.FindElement(sourceElement.Id) ??
                    throw new InvalidOperationException("Append-with-provenance target element is missing after semantic import: " + sourceElement.Id + ".");
                if (targetElement.SourceHandles.Count != 0)
                    throw new InvalidOperationException("Append-with-provenance must not copy source handles into target CAD ownership: " + targetElement.Id + ".");
                if (!string.IsNullOrWhiteSpace(targetElement.DrawingFingerprint))
                    throw new InvalidOperationException("Append-with-provenance must not copy the source drawing fingerprint into target CAD ownership: " + targetElement.Id + ".");
            }
        }
    }
}
