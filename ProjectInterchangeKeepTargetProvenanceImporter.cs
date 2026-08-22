using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeKeepTargetProvenancePlan
    {
        internal ProjectInterchangeKeepTargetProvenancePlan(
            ProjectInterchangeKeepTargetImportPlan semanticPlan,
            ProjectInterchangeSourceHandleProvenancePlan provenancePlan,
            IReadOnlyDictionary<string, string> addedElementMappings,
            int collidedSourceElementsWithoutTargetLineage)
        {
            SemanticPlan = semanticPlan ?? throw new ArgumentNullException(nameof(semanticPlan));
            ProvenancePlan = provenancePlan ?? throw new ArgumentNullException(nameof(provenancePlan));
            AddedElementMappings = addedElementMappings ?? throw new ArgumentNullException(nameof(addedElementMappings));
            CollidedSourceElementsWithoutTargetLineage = collidedSourceElementsWithoutTargetLineage;
        }

        public ProjectInterchangeKeepTargetImportPlan SemanticPlan { get; }
        public ProjectInterchangeSourceHandleProvenancePlan ProvenancePlan { get; }
        public IReadOnlyDictionary<string, string> AddedElementMappings { get; }
        public int AddedElementMappingCount => AddedElementMappings.Count;
        public int CollidedSourceElementsWithoutTargetLineage { get; }
        public int ProvenanceHandleCount => ProvenancePlan.SourceHandleCount;
    }

    public sealed class ProjectInterchangeKeepTargetProvenanceResult
    {
        internal ProjectInterchangeKeepTargetProvenanceResult(
            ProjectInterchangeKeepTargetImportResult semanticResult,
            ProjectInterchangeSourceHandleProvenanceResult provenanceResult,
            ProjectInterchangeProvenanceTargetMapResult targetMapResult,
            int collidedSourceElementsWithoutTargetLineage)
        {
            SemanticResult = semanticResult ?? throw new ArgumentNullException(nameof(semanticResult));
            ProvenanceResult = provenanceResult ?? throw new ArgumentNullException(nameof(provenanceResult));
            TargetMapResult = targetMapResult ?? throw new ArgumentNullException(nameof(targetMapResult));
            CollidedSourceElementsWithoutTargetLineage = collidedSourceElementsWithoutTargetLineage;
        }

        public ProjectInterchangeKeepTargetImportResult SemanticResult { get; }
        public ProjectInterchangeSourceHandleProvenanceResult ProvenanceResult { get; }
        public ProjectInterchangeProvenanceTargetMapResult TargetMapResult { get; }
        public int CollidedSourceElementsWithoutTargetLineage { get; }
    }

    /// <summary>
    /// Composes KeepTarget import with non-owning source-handle provenance. Only source Elements that
    /// are actually appended receive source->target lineage. A collided source Element that KeepTarget
    /// discards must never be mapped to the pre-existing target Element with the same id.
    /// </summary>
    public static class ProjectInterchangeKeepTargetProvenanceImporter
    {
        public const string ImportMode = "KeepTargetPreserveSourceHandleProvenance";
        public const string LastAddedMappingCountKey = "Interchange.LastImport.ProvenanceTargetMappings";
        public const string LastCollidedWithoutLineageKey = "Interchange.LastImport.SourceElementCollisionsWithoutTargetLineage";
        public const string LastProvenanceHandleCountKey = "Interchange.LastImport.SourceHandlesPreservedAsProvenance";
        private const string LastModeKey = "Interchange.LastImport.Mode";

        public static ProjectInterchangeKeepTargetProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var semanticPlan = ProjectInterchangeKeepTargetImporter.Plan(target, json);
            var provenancePlan = ProjectInterchangeSourceHandleProvenance.Plan(target, json);
            EnsureProvenanceCanBeScoped(provenancePlan);
            if (provenancePlan.SourceHandleCount != semanticPlan.SourceHandlesToDiscard)
                throw new InvalidOperationException("KeepTarget provenance accounting does not match the canonical semantic import source-handle count.");

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var collisions = 0;
            foreach (var sourceElement in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (target.FindElement(sourceElement.Id) != null)
                {
                    collisions = checked(collisions + 1);
                    continue;
                }
                mappings.Add(sourceElement.Id, sourceElement.Id);
            }

            if (mappings.Count != semanticPlan.ElementsToAdd)
                throw new InvalidOperationException("KeepTarget provenance added-element lineage no longer matches the canonical KeepTarget plan.");
            if (collisions != semanticPlan.ElementsToKeep)
                throw new InvalidOperationException("KeepTarget provenance collision accounting no longer matches the canonical KeepTarget plan.");

            return new ProjectInterchangeKeepTargetProvenancePlan(
                semanticPlan,
                provenancePlan,
                new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(mappings, StringComparer.OrdinalIgnoreCase)),
                collisions);
        }

        public static ProjectInterchangeKeepTargetProvenanceResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var plan = Plan(target, json);
            var rollback = ProjectStateSnapshot.Capture(target);
            try
            {
                var semanticResult = ProjectInterchangeKeepTargetImporter.Import(target, json);
                EnsureMappedTargetsDoNotOwnSourceCad(target, plan.AddedElementMappings);

                var provenanceResult = ProjectInterchangeSourceHandleProvenance.Store(target, json);
                var targetMapResult = ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    plan.ProvenancePlan.SourceProjectId,
                    plan.ProvenancePlan.SourceDrawingFingerprint,
                    plan.AddedElementMappings);

                if (provenanceResult.SourceHandlesStored != plan.ProvenanceHandleCount ||
                    targetMapResult.MappingsStored != plan.AddedElementMappingCount)
                    throw new InvalidOperationException("KeepTarget provenance execution no longer matches the pre-mutation plan.");

                target.Metadata[LastModeKey] = ImportMode;
                target.Metadata[LastAddedMappingCountKey] = plan.AddedElementMappingCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastCollidedWithoutLineageKey] = plan.CollidedSourceElementsWithoutTargetLineage.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastProvenanceHandleCountKey] = plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture);
                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeKeepTargetWithSourceHandleProvenance",
                    string.Empty,
                    "KeepTarget import preserved non-owning source-handle provenance for project " + plan.ProvenancePlan.SourceProjectId +
                    ": addedElementMappings=" + plan.AddedElementMappingCount.ToString(CultureInfo.InvariantCulture) +
                    ", collidedSourceElementsWithoutTargetLineage=" + plan.CollidedSourceElementsWithoutTargetLineage.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture) + ".");
                target.Touch();

                return new ProjectInterchangeKeepTargetProvenanceResult(
                    semanticResult,
                    provenanceResult,
                    targetMapResult,
                    plan.CollidedSourceElementsWithoutTargetLineage);
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(target); }
                catch (Exception rollbackError)
                {
                    throw new InvalidOperationException(
                        "Interchange KeepTarget with provenance failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        private static void EnsureProvenanceCanBeScoped(ProjectInterchangeSourceHandleProvenancePlan plan)
        {
            if (plan.SourceHandleCount > 0 && string.IsNullOrWhiteSpace(plan.SourceDrawingFingerprint))
                throw new InvalidOperationException("KeepTarget with provenance requires a source drawing fingerprint when drawing-local source handles are present.");
        }

        private static void EnsureMappedTargetsDoNotOwnSourceCad(ProjectState target, IReadOnlyDictionary<string, string> mappings)
        {
            foreach (var mapping in mappings)
            {
                var element = target.FindElement(mapping.Value) ??
                    throw new InvalidOperationException("KeepTarget provenance added target Element is missing after semantic mutation: " + mapping.Value + ".");
                if (element.SourceHandles.Count != 0)
                    throw new InvalidOperationException("KeepTarget provenance must not assign imported source handles to target CAD ownership: " + mapping.Value + ".");
                if (!string.IsNullOrWhiteSpace(element.DrawingFingerprint))
                    throw new InvalidOperationException("KeepTarget provenance must not assign imported source drawing fingerprint to target CAD ownership: " + mapping.Value + ".");
            }
        }
    }
}
