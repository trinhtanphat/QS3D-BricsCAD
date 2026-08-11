using System;
using System.Globalization;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeAppendProvenancePlan
    {
        internal ProjectInterchangeAppendProvenancePlan(
            ProjectInterchangeAppendOnlyImportPlan semanticPlan,
            ProjectInterchangeSourceHandleProvenancePlan provenancePlan)
        {
            SemanticPlan = semanticPlan ?? throw new ArgumentNullException(nameof(semanticPlan));
            ProvenancePlan = provenancePlan ?? throw new ArgumentNullException(nameof(provenancePlan));
        }

        public ProjectInterchangeAppendOnlyImportPlan SemanticPlan { get; }
        public ProjectInterchangeSourceHandleProvenancePlan ProvenancePlan { get; }
        public int ProvenanceElementCount => ProvenancePlan.ElementsWithHandles;
        public int ProvenanceHandleCount => ProvenancePlan.SourceHandleCount;
    }

    public sealed class ProjectInterchangeAppendProvenanceResult
    {
        internal ProjectInterchangeAppendProvenanceResult(
            ProjectInterchangeAppendOnlyImportResult semanticResult,
            ProjectInterchangeSourceHandleProvenanceResult provenanceResult)
        {
            SemanticResult = semanticResult ?? throw new ArgumentNullException(nameof(semanticResult));
            ProvenanceResult = provenanceResult ?? throw new ArgumentNullException(nameof(provenanceResult));
        }

        public ProjectInterchangeAppendOnlyImportResult SemanticResult { get; }
        public ProjectInterchangeSourceHandleProvenanceResult ProvenanceResult { get; }
        public int ProvenanceElementCount => ProvenanceResult.ElementsStored;
        public int ProvenanceHandleCount => ProvenanceResult.SourceHandlesStored;
    }

    /// <summary>
    /// Executes append-only semantic import plus PreserveAsProvenanceOnly as one rollback-protected
    /// project operation. The existing provenance store remains the single record format; this class
    /// only composes its mutation atomically with the canonical append importer.
    /// </summary>
    public static class ProjectInterchangeAppendProvenanceImporter
    {
        public const string ImportMode = "AppendOnlyPreserveSourceHandleProvenance";
        public const string LastProvenanceElementCountKey = "Interchange.LastImport.SourceHandleProvenanceElements";
        public const string LastProvenanceHandleCountKey = "Interchange.LastImport.SourceHandlesPreservedAsProvenance";

        public static ProjectInterchangeAppendProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var semanticPlan = ProjectInterchangeAppendOnlyImporter.Plan(target, json);
            var provenancePlan = ProjectInterchangeSourceHandleProvenance.Plan(target, json);
            EnsureProvenanceCanBeScoped(provenancePlan);
            if (provenancePlan.SourceHandleCount != semanticPlan.SourceHandlesToDiscard)
                throw new InvalidOperationException("Append provenance accounting does not match the canonical append-only source-handle count.");
            return new ProjectInterchangeAppendProvenancePlan(semanticPlan, provenancePlan);
        }

        public static ProjectInterchangeAppendProvenanceResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var plan = Plan(target, json);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var rollback = ProjectStateSnapshot.Capture(target);
            try
            {
                var semanticResult = ProjectInterchangeAppendOnlyImporter.Import(target, json);
                EnsureImportedElementsDoNotOwnSourceCad(target, source);

                var provenanceResult = ProjectInterchangeSourceHandleProvenance.Store(target, json);
                if (provenanceResult.SourceHandlesStored != plan.ProvenanceHandleCount ||
                    provenanceResult.ElementsStored != plan.ProvenanceElementCount)
                    throw new InvalidOperationException("Append provenance execution no longer matches the pre-mutation provenance plan.");

                target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
                target.Metadata[LastProvenanceElementCountKey] = plan.ProvenanceElementCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastProvenanceHandleCountKey] = plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeAppendWithSourceHandleProvenance",
                    string.Empty,
                    "Append-only semantic import preserved source handles as non-owning provenance for project " + plan.ProvenancePlan.SourceProjectId +
                    ": elements=" + plan.ProvenanceElementCount.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture) + ".");
                target.Touch();

                return new ProjectInterchangeAppendProvenanceResult(semanticResult, provenanceResult);
            }
            catch (Exception operationError)
            {
                try
                {
                    rollback.Restore(target);
                }
                catch (Exception rollbackError)
                {
                    throw new InvalidOperationException(
                        "Interchange append-with-provenance import failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        private static void EnsureProvenanceCanBeScoped(ProjectInterchangeSourceHandleProvenancePlan plan)
        {
            if (plan.SourceHandleCount > 0 && string.IsNullOrWhiteSpace(plan.SourceDrawingFingerprint))
                throw new InvalidOperationException(
                    "Append-with-provenance requires a source drawing fingerprint when drawing-local source handles are present. " +
                    "The handles remain provenance only and cannot be safely scoped to an unnamed/unknown source drawing.");
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
