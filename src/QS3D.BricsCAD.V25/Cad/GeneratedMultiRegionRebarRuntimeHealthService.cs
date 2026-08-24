using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Read-only integrity inspection for persisted LOCAL-005 multi-region rebar state.
    /// This service never mutates the DWG or the QS3D project state.
    /// </summary>
    internal static class GeneratedMultiRegionRebarRuntimeHealthService
    {
        private const double MaximumSagittaM = .002d;

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements
                    .Where(x => x.Category == ElementCategory.Slab || x.Category == ElementCategory.Foundation)
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    InspectElement(document, transaction, project, element, issues);
                }
            }

            return issues.AsReadOnly();
        }

        private static void InspectElement(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            ICollection<ModelHealthIssue> issues)
        {
            var prefix = element.Category == ElementCategory.Slab
                ? "GeneratedSlabMesh"
                : "GeneratedFoundationMesh";
            var handlesKey = prefix + "Handles";
            var sourceManifestKey = prefix + "MultiRegionSourceManifest";
            var generatedManifestKey = prefix + "MultiRegionGeneratedManifest";
            var topologyFingerprintKey = prefix + "MultiRegionTopologyFingerprint";

            var hasSourceManifest = element.Properties.TryGetValue(sourceManifestKey, out var rawSources) && !string.IsNullOrWhiteSpace(rawSources);
            var hasGeneratedManifest = element.Properties.TryGetValue(generatedManifestKey, out var rawGenerated) && !string.IsNullOrWhiteSpace(rawGenerated);
            if (!hasSourceManifest && !hasGeneratedManifest) return;

            if (!hasSourceManifest || !hasGeneratedManifest)
            {
                Add(issues, "MULTI_REGION_MANIFEST_PAIR_MISSING", element,
                    "Multi-region source/generated manifests must be present as one complete pair.");
                return;
            }

            IReadOnlyList<SourceManifestEntry> sourceManifest;
            IReadOnlyList<GeneratedManifestEntry> generatedManifest;
            try
            {
                sourceManifest = MultiRegionRebarManifest.ParseSources(rawSources!);
                generatedManifest = MultiRegionRebarManifest.ParseGenerated(rawGenerated!);
            }
            catch (Exception ex)
            {
                Add(issues, "MULTI_REGION_MANIFEST_INVALID", element,
                    "Multi-region manifest is invalid: " + ex.Message);
                return;
            }

            if (!element.Properties.TryGetValue(topologyFingerprintKey, out var topologyFingerprint) || string.IsNullOrWhiteSpace(topologyFingerprint))
                Add(issues, "MULTI_REGION_TOPOLOGY_FINGERPRINT_MISSING", element,
                    "MultiRegionTopologyFingerprint is missing for persisted multi-region output.");

            var sourceRegionIds = new HashSet<string>(sourceManifest.Select(x => x.RegionId), StringComparer.OrdinalIgnoreCase);
            var generatedRegionIds = new HashSet<string>(generatedManifest.Select(x => x.RegionId), StringComparer.OrdinalIgnoreCase);
            if (!sourceRegionIds.SetEquals(generatedRegionIds))
                Add(issues, "MULTI_REGION_REGION_SET_MISMATCH", element,
                    "Source and generated manifests do not describe the same region set.");

            var sourceHandles = sourceManifest
                .SelectMany(x => new[] { x.OuterSourceHandle }.Concat(x.HoleSourceHandles))
                .ToList();
            if (sourceHandles.Count != sourceHandles.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                Add(issues, "MULTI_REGION_SOURCE_DUPLICATE", element,
                    "DUPLICATE source handle detected across persisted multi-region topology.");

            var loops = new List<PolygonSourceLoop2>();
            foreach (var handle in sourceHandles)
            {
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count != 1)
                {
                    Add(issues, "MULTI_REGION_SOURCE_MISSING", element,
                        "Multi-region source handle does not resolve uniquely: " + handle + ".");
                    continue;
                }

                var polyline = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Polyline;
                if (polyline == null || polyline.IsErased)
                {
                    Add(issues, "MULTI_REGION_SOURCE_TYPE_INVALID", element,
                        "Multi-region source is not a live POLYLINE: " + handle + ".");
                    continue;
                }

                try
                {
                    var read = ClosedPolygonSourceLoopReader.Read(
                        document,
                        polyline,
                        MaximumSagittaM,
                        element.Id + "/multi-region health source");
                    loops.Add(new PolygonSourceLoop2(read.SourceHandle, read.Loop));
                }
                catch (Exception ex)
                {
                    Add(issues, "MULTI_REGION_SOURCE_GEOMETRY_INVALID", element,
                        "Multi-region source geometry is invalid: " + handle + " • " + ex.Message);
                }
            }

            if (loops.Count == sourceHandles.Count && loops.Count > 0)
            {
                try
                {
                    var current = PolygonSourceLoopRegionAssembler.Assemble(loops.AsReadOnly());
                    var currentRegions = new HashSet<string>(current.Regions.Select(x => x.RegionId), StringComparer.OrdinalIgnoreCase);
                    if (!currentRegions.SetEquals(sourceRegionIds))
                        Add(issues, "MULTI_REGION_TOPOLOGY_STALE", element,
                            "Current source topology no longer matches the persisted multi-region region identities.");
                }
                catch (Exception ex)
                {
                    Add(issues, "MULTI_REGION_TOPOLOGY_INVALID", element,
                        "Current source loops cannot be assembled into supported multi-region topology: " + ex.Message);
                }
            }

            var generatedHandles = generatedManifest.SelectMany(x => x.Handles).ToList();
            if (generatedHandles.Count != generatedHandles.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                Add(issues, "MULTI_REGION_GENERATED_DUPLICATE", element,
                    "DUPLICATE generated handle detected across multi-region output.");

            if (element.Properties.TryGetValue(handlesKey, out var aggregateRaw) && !string.IsNullOrWhiteSpace(aggregateRaw))
            {
                var aggregate = aggregateRaw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();
                if (!new HashSet<string>(aggregate, StringComparer.OrdinalIgnoreCase)
                    .SetEquals(generatedHandles))
                    Add(issues, "MULTI_REGION_AGGREGATE_MISMATCH", element,
                        "Aggregate generated-handle slot does not exactly match the generated manifest.");
            }
            else if (generatedHandles.Count > 0)
            {
                Add(issues, "MULTI_REGION_AGGREGATE_MISSING", element,
                    "Generated manifest has output but the aggregate generated-handle slot is missing.");
            }

            foreach (var region in generatedManifest)
            {
                foreach (var handle in region.Handles)
                {
                    var ids = CadHandleService.Resolve(document, new[] { handle });
                    if (ids.Count != 1)
                    {
                        Add(issues, "MULTI_REGION_GENERATED_MISSING", element,
                            "Generated multi-region handle does not resolve uniquely: " + handle + ".");
                        continue;
                    }

                    var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                    {
                        Add(issues, "MULTI_REGION_GENERATED_INVALID", element,
                            "Generated multi-region handle is not a live Entity: " + handle + ".");
                        continue;
                    }

                    if (!GeneratedRebarNativeOwnershipService.HasMatchingOwnership(entity, project, element, handlesKey))
                        Add(issues, "MULTI_REGION_OWNER_MISMATCH", element,
                            "Generated rebar ownership marker does not match project/element/owner slot: " + handle + ".");
                    if (!GeneratedRebarRegionOwnershipService.HasMatchingOwnership(entity, project, element, handlesKey, region.RegionId))
                        Add(issues, "MULTI_REGION_REGION_OWNER_MISMATCH", element,
                            "Generated rebar region ownership marker does not match manifest region " + region.RegionId + ": " + handle + ".");
                }
            }
        }

        private static void Add(
            ICollection<ModelHealthIssue> issues,
            string code,
            ProjectElement element,
            string message)
        {
            issues.Add(new ModelHealthIssue(code, HealthSeverity.Error, message, element.Id));
        }
    }
}
