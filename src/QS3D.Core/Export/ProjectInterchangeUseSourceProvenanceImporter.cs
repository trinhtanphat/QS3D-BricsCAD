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
    public sealed class ProjectInterchangeUseSourceProvenancePlan
    {
        internal ProjectInterchangeUseSourceProvenancePlan(
            ProjectInterchangeUseSourceSemanticPlan semanticPlan,
            ProjectInterchangeSourceHandleProvenancePlan provenancePlan,
            IReadOnlyDictionary<string, string> elementMappings)
        {
            SemanticPlan = semanticPlan ?? throw new ArgumentNullException(nameof(semanticPlan));
            ProvenancePlan = provenancePlan ?? throw new ArgumentNullException(nameof(provenancePlan));
            ElementMappings = elementMappings ?? throw new ArgumentNullException(nameof(elementMappings));
        }

        public ProjectInterchangeUseSourceSemanticPlan SemanticPlan { get; }
        public ProjectInterchangeSourceHandleProvenancePlan ProvenancePlan { get; }
        public IReadOnlyDictionary<string, string> ElementMappings { get; }
        public int MappingCount => ElementMappings.Count;
        public int ProvenanceHandleCount => ProvenancePlan.SourceHandleCount;
        public bool RequiresNativeCleanup => SemanticPlan.RequiresNativeCleanup;
    }

    public sealed class ProjectInterchangeUseSourceProvenanceResult
    {
        internal ProjectInterchangeUseSourceProvenanceResult(
            ProjectInterchangeUseSourceSemanticResult semanticResult,
            ProjectInterchangeSourceHandleProvenanceResult provenanceResult,
            ProjectInterchangeProvenanceTargetMapResult targetMapResult)
        {
            SemanticResult = semanticResult ?? throw new ArgumentNullException(nameof(semanticResult));
            ProvenanceResult = provenanceResult ?? throw new ArgumentNullException(nameof(provenanceResult));
            TargetMapResult = targetMapResult ?? throw new ArgumentNullException(nameof(targetMapResult));
        }

        public ProjectInterchangeUseSourceSemanticResult SemanticResult { get; }
        public ProjectInterchangeSourceHandleProvenanceResult ProvenanceResult { get; }
        public ProjectInterchangeProvenanceTargetMapResult TargetMapResult { get; }
    }

    /// <summary>
    /// Composes UseSourceSemanticData with canonical non-owning source-handle provenance.
    /// Native cleanup authorization is forwarded unchanged to the canonical UseSource executor;
    /// this wrapper never treats provenance retention as permission to erase native CAD.
    /// </summary>
    public static class ProjectInterchangeUseSourceProvenanceImporter
    {
        public const string ImportMode = "UseSourceSemanticDataPreserveSourceHandleProvenance";
        public const string LastMappingCountKey = "Interchange.LastImport.ProvenanceTargetMappings";
        public const string LastProvenanceHandleCountKey = "Interchange.LastImport.SourceHandlesPreservedAsProvenance";
        private const string LastModeKey = "Interchange.LastImport.Mode";

        public static ProjectInterchangeUseSourceProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var semanticPlan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            var provenancePlan = ProjectInterchangeSourceHandleProvenance.Plan(target, json);
            EnsureProvenanceCanBeScoped(provenancePlan);
            if (provenancePlan.SourceHandleCount != semanticPlan.SourceHandlesToDiscard)
                throw new InvalidOperationException("UseSource provenance accounting does not match the canonical semantic replacement source-handle count.");

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (map.ContainsKey(element.Id))
                    throw new InvalidOperationException("UseSource provenance contains duplicate source Element mapping for " + element.Id + ".");
                map.Add(element.Id, element.Id);
            }
            return new ProjectInterchangeUseSourceProvenancePlan(
                semanticPlan,
                provenancePlan,
                new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase)));
        }

        public static ProjectInterchangeUseSourceProvenanceResult Import(
            ProjectState target,
            string json,
            ProjectInterchangeNativeCleanupAuthorization nativeCleanupAuthorization)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (nativeCleanupAuthorization == null) throw new ArgumentNullException(nameof(nativeCleanupAuthorization));

            var plan = Plan(target, json);
            var rollback = ProjectStateSnapshot.Capture(target);
            try
            {
                var semanticResult = ProjectInterchangeUseSourceSemanticImporter.Import(target, json, nativeCleanupAuthorization);
                EnsureSourceElementsDoNotOwnImportedCad(target, plan.ElementMappings);

                var provenanceResult = ProjectInterchangeSourceHandleProvenance.Store(target, json);
                var targetMapResult = ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    plan.ProvenancePlan.SourceProjectId,
                    plan.ProvenancePlan.SourceDrawingFingerprint,
                    plan.ElementMappings);

                if (provenanceResult.SourceHandlesStored != plan.ProvenanceHandleCount ||
                    targetMapResult.MappingsStored != plan.MappingCount)
                    throw new InvalidOperationException("UseSource provenance execution no longer matches the pre-mutation plan.");

                target.Metadata[LastModeKey] = ImportMode;
                target.Metadata[LastMappingCountKey] = plan.MappingCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastProvenanceHandleCountKey] = plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture);
                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeUseSourceWithSourceHandleProvenance",
                    string.Empty,
                    "UseSource semantic import preserved non-owning source-handle provenance for project " + plan.ProvenancePlan.SourceProjectId +
                    ": mappings=" + plan.MappingCount.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ", nativeCleanupElements=" + plan.SemanticPlan.TargetElementIdsRequiringNativeCleanup.Count.ToString(CultureInfo.InvariantCulture) + ".");
                target.Touch();

                return new ProjectInterchangeUseSourceProvenanceResult(semanticResult, provenanceResult, targetMapResult);
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(target); }
                catch (Exception rollbackError)
                {
                    throw new InvalidOperationException(
                        "Interchange UseSource with provenance failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        private static void EnsureProvenanceCanBeScoped(ProjectInterchangeSourceHandleProvenancePlan plan)
        {
            if (plan.SourceHandleCount > 0 && string.IsNullOrWhiteSpace(plan.SourceDrawingFingerprint))
                throw new InvalidOperationException("UseSource with provenance requires a source drawing fingerprint when drawing-local source handles are present.");
        }

        private static void EnsureSourceElementsDoNotOwnImportedCad(ProjectState target, IReadOnlyDictionary<string, string> mappings)
        {
            foreach (var mapping in mappings)
            {
                var element = target.FindElement(mapping.Value) ??
                    throw new InvalidOperationException("UseSource provenance target Element is missing after semantic mutation: " + mapping.Value + ".");
                if (element.SourceHandles.Count != 0)
                    throw new InvalidOperationException("UseSource provenance must not assign imported source handles to target CAD ownership: " + mapping.Value + ".");
                if (!string.IsNullOrWhiteSpace(element.DrawingFingerprint))
                    throw new InvalidOperationException("UseSource provenance must not assign imported source drawing fingerprint to target CAD ownership: " + mapping.Value + ".");
            }
        }
    }
}
