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
    public sealed class ProjectInterchangeRemapProvenancePlan
    {
        internal ProjectInterchangeRemapProvenancePlan(
            ProjectInterchangeRemapAppendPlan semanticPlan,
            ProjectInterchangeSourceHandleProvenancePlan provenancePlan,
            IReadOnlyDictionary<string, string> elementMappings)
        {
            SemanticPlan = semanticPlan ?? throw new ArgumentNullException(nameof(semanticPlan));
            ProvenancePlan = provenancePlan ?? throw new ArgumentNullException(nameof(provenancePlan));
            ElementMappings = elementMappings ?? throw new ArgumentNullException(nameof(elementMappings));
        }

        public ProjectInterchangeRemapAppendPlan SemanticPlan { get; }
        public ProjectInterchangeSourceHandleProvenancePlan ProvenancePlan { get; }
        public IReadOnlyDictionary<string, string> ElementMappings { get; }
        public int MappingCount => ElementMappings.Count;
        public int ProvenanceHandleCount => ProvenancePlan.SourceHandleCount;
        public bool CanImport => SemanticPlan.CanImport;
    }

    public sealed class ProjectInterchangeRemapProvenanceResult
    {
        internal ProjectInterchangeRemapProvenanceResult(
            ProjectInterchangeRemapAppendResult semanticResult,
            ProjectInterchangeSourceHandleProvenanceResult provenanceResult,
            ProjectInterchangeProvenanceTargetMapResult targetMapResult)
        {
            SemanticResult = semanticResult ?? throw new ArgumentNullException(nameof(semanticResult));
            ProvenanceResult = provenanceResult ?? throw new ArgumentNullException(nameof(provenanceResult));
            TargetMapResult = targetMapResult ?? throw new ArgumentNullException(nameof(targetMapResult));
        }

        public ProjectInterchangeRemapAppendResult SemanticResult { get; }
        public ProjectInterchangeSourceHandleProvenanceResult ProvenanceResult { get; }
        public ProjectInterchangeProvenanceTargetMapResult TargetMapResult { get; }
    }

    public static class ProjectInterchangeRemapProvenanceImporter
    {
        public const string ImportMode = "RemapAppendAsNewPreserveSourceHandleProvenance";
        public const string LastMappingCountKey = "Interchange.LastImport.ProvenanceTargetMappings";
        public const string LastProvenanceHandleCountKey = "Interchange.LastImport.SourceHandlesPreservedAsProvenance";
        private const string LastModeKey = "Interchange.LastImport.Mode";

        public static ProjectInterchangeRemapProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var semanticPlan = ProjectInterchangeRemapAppendImporter.Plan(target, json);
            var provenancePlan = ProjectInterchangeSourceHandleProvenance.Plan(target, json);
            EnsureProvenanceCanBeScoped(provenancePlan);
            if (provenancePlan.SourceHandleCount != semanticPlan.SourceHandleCount)
                throw new InvalidOperationException("Import As New provenance accounting does not match the canonical remap source-handle count.");

            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var targetId = semanticPlan.Remap.MapId(InterchangeRemapIdentityKind.Element, element.Id);
                if (map.ContainsKey(element.Id))
                    throw new InvalidOperationException("Import As New provenance contains duplicate source Element mapping for " + element.Id + ".");
                map.Add(element.Id, targetId);
            }
            if (map.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != map.Count)
                throw new InvalidOperationException("Import As New provenance target mapping is not one-to-one.");

            var immutableMap = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase));
            return new ProjectInterchangeRemapProvenancePlan(semanticPlan, provenancePlan, immutableMap);
        }

        public static ProjectInterchangeRemapProvenanceResult Import(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var plan = Plan(target, json);
            if (!plan.CanImport)
                throw new InvalidOperationException("Import As New with provenance is blocked by the canonical remap plan; resolve all compatibility/reference blockers first.");

            var rollback = ProjectStateSnapshot.Capture(target);
            try
            {
                var semanticResult = ProjectInterchangeRemapAppendImporter.Import(target, json);
                EnsureMappedTargetsDoNotOwnSourceCad(target, plan.ElementMappings);

                var provenanceResult = ProjectInterchangeSourceHandleProvenance.Store(target, json);
                var targetMapResult = ProjectInterchangeProvenanceTargetMap.Store(
                    target,
                    plan.ProvenancePlan.SourceProjectId,
                    plan.ProvenancePlan.SourceDrawingFingerprint,
                    plan.ElementMappings);

                if (provenanceResult.SourceHandlesStored != plan.ProvenanceHandleCount ||
                    targetMapResult.MappingsStored != plan.MappingCount)
                    throw new InvalidOperationException("Import As New provenance execution no longer matches the pre-mutation plan.");

                target.Metadata[LastModeKey] = ImportMode;
                target.Metadata[LastMappingCountKey] = plan.MappingCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastProvenanceHandleCountKey] = plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture);
                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeRemapWithSourceHandleProvenance",
                    string.Empty,
                    "Import As New preserved non-owning source-handle provenance and source-to-target semantic lineage for project " + plan.ProvenancePlan.SourceProjectId +
                    ": mappings=" + plan.MappingCount.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + plan.ProvenanceHandleCount.ToString(CultureInfo.InvariantCulture) + ".");
                target.Touch();

                return new ProjectInterchangeRemapProvenanceResult(semanticResult, provenanceResult, targetMapResult);
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(target); }
                catch (Exception rollbackError)
                {
                    throw new InvalidOperationException(
                        "Interchange Import As New with provenance failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        private static void EnsureProvenanceCanBeScoped(ProjectInterchangeSourceHandleProvenancePlan plan)
        {
            if (plan.SourceHandleCount > 0 && string.IsNullOrWhiteSpace(plan.SourceDrawingFingerprint))
                throw new InvalidOperationException(
                    "Import As New with provenance requires a source drawing fingerprint when drawing-local source handles are present.");
        }

        private static void EnsureMappedTargetsDoNotOwnSourceCad(ProjectState target, IReadOnlyDictionary<string, string> mappings)
        {
            foreach (var mapping in mappings)
            {
                var element = target.FindElement(mapping.Value) ??
                    throw new InvalidOperationException("Import As New provenance target Element is missing after semantic mutation: " + mapping.Value + ".");
                if (element.SourceHandles.Count != 0)
                    throw new InvalidOperationException("Import As New provenance must not assign source handles to target CAD ownership: " + mapping.Value + ".");
                if (!string.IsNullOrWhiteSpace(element.DrawingFingerprint))
                    throw new InvalidOperationException("Import As New provenance must not assign the source drawing fingerprint to target CAD ownership: " + mapping.Value + ".");
            }
        }
    }
}
