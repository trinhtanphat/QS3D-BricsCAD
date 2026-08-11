using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class InterchangeUseSourceCatalogImportPlan
    {
        public string SourceProjectId { get; set; } = string.Empty;
        public int ZonesToAdd { get; set; }
        public int FloorsToAdd { get; set; }
        public int FamiliesToAdd { get; set; }
        public int ElementsToAdd { get; set; }
        public int ZonesToReplace { get; set; }
        public int FloorsToReplace { get; set; }
        public int FamiliesToReplace { get; set; }
        public int ElementCollisionsKept { get; set; }
        public int AffectedExistingElements { get; set; }
        public int SourceHandlesToDiscard { get; set; }
        public int ValidationWarnings { get; set; }
        public IReadOnlyList<string> ReplacementZoneIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ReplacementFloorIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ReplacementFamilyIds { get; set; } = Array.Empty<string>();
    }

    internal sealed class InterchangeUseSourceCatalogImportResult
    {
        public int ZonesAdded { get; set; }
        public int FloorsAdded { get; set; }
        public int FamiliesAdded { get; set; }
        public int ElementsAdded { get; set; }
        public int ZonesReplaced { get; set; }
        public int FloorsReplaced { get; set; }
        public int FamiliesReplaced { get; set; }
        public int ElementCollisionsKept { get; set; }
        public int GeneratedElementsInvalidated { get; set; }
        public int SourceHandlesDiscarded { get; set; }
    }

    internal static class InterchangeUseSourceCatalogImportService
    {
        private sealed class PreparedImport
        {
            public ProjectInterchangeValidatedSnapshot Source { get; set; } = null!;
            public ProjectInterchangeImportResolutionPlan Resolution { get; set; } = null!;
            public InterchangeUseSourceCatalogImportPlan Plan { get; set; } = null!;
        }

        private const string ImportMode = "UseSourceCatalogSemanticData";
        private const string LastZonesReplacedKey = "Interchange.LastImport.ZonesReplaced";
        private const string LastFloorsReplacedKey = "Interchange.LastImport.FloorsReplaced";
        private const string LastFamiliesReplacedKey = "Interchange.LastImport.FamiliesReplaced";
        private const string LastAffectedElementsKey = "Interchange.LastImport.CatalogAffectedElements";

        public static InterchangeUseSourceCatalogImportPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static InterchangeUseSourceCatalogImportResult Import(
            Document document,
            ProjectState authorizedProject,
            string json)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = InterchangeMutationTargetGuard.RequireExact(
                document,
                authorizedProject,
                "Interchange source-catalog import");
            var prepared = Prepare(project, json);
            EnsureActive(document, "Interchange source-catalog import / mutation");

            var invalidationTargets = ExpandInvalidationTargets(project, prepared.Source, prepared.Resolution, prepared.Plan);
            GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);

            ProjectStateSnapshot? rollback = null;
            var cadCommitted = false;
            var generatedElementsInvalidated = 0;

            try
            {
                using (document.LockDocument())
                {
                    EnsureActive(document, "Interchange source-catalog import / locked mutation");
                    var lockedProject = InterchangeMutationTargetGuard.RequireExact(
                        document,
                        project,
                        "Interchange source-catalog import / locked mutation");
                    var lockedInvalidationTargets = ExpandInvalidationTargets(
                        lockedProject,
                        prepared.Source,
                        prepared.Resolution,
                        prepared.Plan);

                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        rollback = ProjectStateSnapshot.Capture(lockedProject);

                        GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-catalog import / pre-native cleanup");

                        var invalidation = GeneratedDependentGeometryInvalidator.Prepare(
                            document,
                            transaction,
                            lockedProject,
                            lockedInvalidationTargets);

                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-catalog import / pre-semantic apply");

                        var targetHadZones = lockedProject.Zones.Count > 0;
                        var targetHadFloors = lockedProject.Floors.Count > 0;
                        var targetHadFamilies = lockedProject.Families.Count > 0;
                        var previousActiveZoneId = lockedProject.ActiveZoneId ?? string.Empty;
                        var previousActiveFloorId = lockedProject.ActiveFloorId ?? string.Empty;
                        var hadActiveFamilyMetadata = lockedProject.Metadata.TryGetValue("ActiveFamilyId", out var previousActiveFamilyId);
                        previousActiveFamilyId = previousActiveFamilyId ?? string.Empty;

                        ApplyCatalogState(lockedProject, prepared.Source, prepared.Resolution);
                        ApplyNewElementsOnly(lockedProject, prepared.Source, prepared.Resolution);
                        RestoreExistingActiveContext(
                            lockedProject,
                            prepared.Source,
                            targetHadZones,
                            targetHadFloors,
                            targetHadFamilies,
                            previousActiveZoneId,
                            previousActiveFloorId,
                            hadActiveFamilyMetadata,
                            previousActiveFamilyId);

                        foreach (var element in lockedInvalidationTargets)
                            element.MarkDirty(ElementDirtyFlags.All);

                        // The native erasure plan was built from the pre-import target ownership map.
                        // Clear those owner slots while the CAD transaction is still rollback-capable.
                        invalidation.CommitMetadata();

                        ValidateCombinedProject(lockedProject, json);
                        RecordImportMetadata(lockedProject, prepared.Source, prepared.Plan, invalidation.ElementCount);
                        lockedProject.Touch();

                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-catalog import / pre-CAD commit");

                        transaction.Commit();
                        cadCommitted = true;
                        generatedElementsInvalidated = invalidation.ElementCount;
                    }
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted && rollback != null)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Interchange source-catalog import failed before CAD commit and semantic rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new InterchangeUseSourceCatalogImportResult
            {
                ZonesAdded = prepared.Plan.ZonesToAdd,
                FloorsAdded = prepared.Plan.FloorsToAdd,
                FamiliesAdded = prepared.Plan.FamiliesToAdd,
                ElementsAdded = prepared.Plan.ElementsToAdd,
                ZonesReplaced = prepared.Plan.ZonesToReplace,
                FloorsReplaced = prepared.Plan.FloorsToReplace,
                FamiliesReplaced = prepared.Plan.FamiliesToReplace,
                ElementCollisionsKept = prepared.Plan.ElementCollisionsKept,
                GeneratedElementsInvalidated = generatedElementsInvalidated,
                SourceHandlesDiscarded = prepared.Plan.SourceHandlesToDiscard
            };
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            ProjectInterchangeKeepTargetImporter.Plan(target, json);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var resolution = ProjectInterchangeImportResolutionPlanner.Plan(target, json, UseSourceCatalogPolicy());

            if (resolution.HasUnresolvedPolicy || resolution.HasBlocks || !resolution.CanProceedToMutationDesign)
            {
                var reasons = resolution.PolicyErrors
                    .Concat(resolution.GlobalBlocks)
                    .Concat(resolution.Items
                        .Where(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible || x.Action == InterchangeImportResolutionAction.Unresolved)
                        .Select(x => x.Kind + " " + x.Id + ": " + x.Reason))
                    .Take(8)
                    .ToArray();
                throw new InvalidOperationException("UseSource catalog interchange import is blocked" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons)));
            }

            foreach (var item in resolution.Items)
            {
                var allowed = item.Kind == InterchangeIdentityKind.Element
                    ? item.Action == InterchangeImportResolutionAction.AddSourceSemanticData || item.Action == InterchangeImportResolutionAction.KeepTarget
                    : item.Action == InterchangeImportResolutionAction.AddSourceSemanticData || item.Action == InterchangeImportResolutionAction.UseSourceSemanticData;
                if (!allowed)
                    throw new InvalidOperationException("UseSource catalog importer received unexpected resolution " + item.Action + " for " + item.Kind + " " + item.Id + ".");
                if (item.Kind != InterchangeIdentityKind.Element && item.RequiresGeneratedOutputReset)
                    throw new InvalidOperationException("Catalog replacement unexpectedly requested element generated-output reset semantics for " + item.Kind + " " + item.Id + ".");
            }

            var zoneIds = ReplacementIds(resolution, InterchangeIdentityKind.Zone);
            var floorIds = ReplacementIds(resolution, InterchangeIdentityKind.Floor);
            var familyIds = ReplacementIds(resolution, InterchangeIdentityKind.Family);
            var affected = CollectInitialAffectedElements(target, zoneIds, floorIds, familyIds);

            return new PreparedImport
            {
                Source = source,
                Resolution = resolution,
                Plan = new InterchangeUseSourceCatalogImportPlan
                {
                    SourceProjectId = source.Project.Id,
                    ZonesToAdd = Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.AddSourceSemanticData),
                    FloorsToAdd = Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.AddSourceSemanticData),
                    FamiliesToAdd = Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.AddSourceSemanticData),
                    ElementsToAdd = Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.AddSourceSemanticData),
                    ZonesToReplace = zoneIds.Count,
                    FloorsToReplace = floorIds.Count,
                    FamiliesToReplace = familyIds.Count,
                    ElementCollisionsKept = Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.KeepTarget),
                    AffectedExistingElements = affected.Count,
                    SourceHandlesToDiscard = source.Elements.Sum(x => x.SourceHandles.Count),
                    ValidationWarnings = source.Validation.WarningCount,
                    ReplacementZoneIds = zoneIds,
                    ReplacementFloorIds = floorIds,
                    ReplacementFamilyIds = familyIds
                }
            };
        }

        private static ProjectInterchangeImportPolicy UseSourceCatalogPolicy()
        {
            return new ProjectInterchangeImportPolicy
            {
                ZoneCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                FloorCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                ElementCollision = InterchangeExistingIdentityAction.KeepTarget,
                ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
                DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
                SourceHandles = InterchangeSourceHandlePolicy.Discard,
                GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild
            };
        }

        private static void ApplyCatalogState(ProjectState project, ProjectInterchangeValidatedSnapshot source, ProjectInterchangeImportResolutionPlan resolution)
        {
            foreach (var snapshot in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Zone, snapshot.Id);
                if (action == InterchangeImportResolutionAction.AddSourceSemanticData)
                {
                    ProjectZoneService.Create(project, snapshot.Id, snapshot.Name);
                    continue;
                }
                if (action != InterchangeImportResolutionAction.UseSourceSemanticData)
                    throw new InvalidOperationException("Unexpected Zone action " + action + " for " + snapshot.Id + ".");
                var target = project.FindZone(snapshot.Id) ?? throw new InvalidOperationException("Replacement Zone disappeared during mutation: " + snapshot.Id + ".");
                target.Name = snapshot.Name;
            }

            foreach (var snapshot in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Floor, snapshot.Id);
                if (action == InterchangeImportResolutionAction.AddSourceSemanticData)
                {
                    ProjectFloorService.Create(project, snapshot.Id, snapshot.Name, snapshot.ElevationM);
                    continue;
                }
                if (action != InterchangeImportResolutionAction.UseSourceSemanticData)
                    throw new InvalidOperationException("Unexpected Floor action " + action + " for " + snapshot.Id + ".");
                var target = project.FindFloor(snapshot.Id) ?? throw new InvalidOperationException("Replacement Floor disappeared during mutation: " + snapshot.Id + ".");
                target.Name = snapshot.Name;
                target.ElevationM = snapshot.ElevationM;
            }

            foreach (var snapshot in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Family, snapshot.Id);
                if (action == InterchangeImportResolutionAction.AddSourceSemanticData)
                {
                    InterchangeFamilySemanticApplier.Add(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties);
                    continue;
                }
                if (action != InterchangeImportResolutionAction.UseSourceSemanticData)
                    throw new InvalidOperationException("Unexpected Family action " + action + " for " + snapshot.Id + ".");
                InterchangeFamilySemanticApplier.Replace(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties);
            }
        }

        private static void ApplyNewElementsOnly(ProjectState project, ProjectInterchangeValidatedSnapshot source, ProjectInterchangeImportResolutionPlan resolution)
        {
            foreach (var snapshot in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Element, snapshot.Id);
                if (action == InterchangeImportResolutionAction.KeepTarget) continue;
                if (action != InterchangeImportResolutionAction.AddSourceSemanticData)
                    throw new InvalidOperationException("Unexpected Element action " + action + " for " + snapshot.Id + ".");

                var added = new ProjectElement(snapshot.Id, snapshot.Category, snapshot.FamilyId, snapshot.FloorId, snapshot.ZoneId)
                {
                    DrawingFingerprint = string.Empty
                };
                foreach (var dependency in snapshot.Dependencies) added.DependsOn.Add(dependency);
                foreach (var property in snapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    added.Properties[property.Key] = property.Value ?? string.Empty;
                foreach (var quantity in snapshot.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    added.Quantities[quantity.Key] = quantity.Value;
                added.MarkDirty(ElementDirtyFlags.All);
                project.Elements.Add(added);
            }
        }

        private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets(
            ProjectState project,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeImportResolutionPlan resolution,
            InterchangeUseSourceCatalogImportPlan plan)
        {
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<ProjectElement>();

            foreach (var element in CollectInitialAffectedElements(project, plan.ReplacementZoneIds, plan.ReplacementFloorIds, plan.ReplacementFamilyIds))
                Enqueue(element, result, queue);

            // New Door/Opening semantics can change an already generated existing host even though no
            // existing Element collision is replaced in this catalog-focused policy.
            foreach (var snapshot in source.Elements)
            {
                if (ResolutionAction(resolution, InterchangeIdentityKind.Element, snapshot.Id) != InterchangeImportResolutionAction.AddSourceSemanticData) continue;
                if (snapshot.Category != ElementCategory.Door && snapshot.Category != ElementCategory.WallOpening) continue;
                if (!snapshot.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId)) continue;
                var host = project.FindElement(hostId.Trim());
                if (host != null) Enqueue(host, result, queue);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                EnqueueExistingOpeningHost(current, graph, result, queue);
                foreach (var dependentId in graph.GetDirectDependents(current.Id))
                {
                    if (!graph.TryGetElement(dependentId, out var dependent) || dependent == null)
                        throw new InvalidOperationException("Catalog invalidation graph returned missing semantic element " + dependentId + ".");
                    Enqueue(dependent, result, queue);
                }
            }

            return result.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> CollectInitialAffectedElements(
            ProjectState project,
            IReadOnlyCollection<string> zoneIds,
            IReadOnlyCollection<string> floorIds,
            IReadOnlyCollection<string> familyIds)
        {
            var zones = new HashSet<string>(zoneIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var floors = new HashSet<string>(floorIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var families = new HashSet<string>(familyIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            return project.Elements
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.ZoneId) && zones.Contains(x.ZoneId)) ||
                    (!string.IsNullOrWhiteSpace(x.FloorId) && floors.Contains(x.FloorId)) ||
                    (!string.IsNullOrWhiteSpace(x.FamilyId) && families.Contains(x.FamilyId)))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static void EnqueueExistingOpeningHost(ProjectElement element, DependencyGraph graph, IDictionary<string, ProjectElement> result, Queue<ProjectElement> queue)
        {
            if (element.Category != ElementCategory.Door && element.Category != ElementCategory.WallOpening) return;
            if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId)) return;
            if (!graph.TryGetElement(hostId.Trim(), out var host) || host == null)
                throw new InvalidOperationException("Opening " + element.Id + " references missing host " + hostId + ". Repair host linkage before catalog replacement.");
            Enqueue(host, result, queue);
        }

        private static void Enqueue(ProjectElement element, IDictionary<string, ProjectElement> result, Queue<ProjectElement> queue)
        {
            if (result.ContainsKey(element.Id)) return;
            result.Add(element.Id, element);
            queue.Enqueue(element);
        }

        private static void ValidateCombinedProject(ProjectState project, string json)
        {
            ProjectInterchangeKeepTargetImporter.Plan(project, json);
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);
            graph.TopologicalDirtyOrder(project.Elements);
        }

        private static void RecordImportMetadata(ProjectState project, ProjectInterchangeValidatedSnapshot source, InterchangeUseSourceCatalogImportPlan plan, int invalidated)
        {
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey] = source.Project.Id;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceSchemaVersionKey] = source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey] = source.Project.DrawingFingerprint;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceUpdatedUtcKey] = source.Project.UpdatedUtcRaw;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey] = plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastZonesReplacedKey] = plan.ZonesToReplace.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastFloorsReplacedKey] = plan.FloorsToReplace.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastFamiliesReplacedKey] = plan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastAffectedElementsKey] = invalidated.ToString(CultureInfo.InvariantCulture);

            AuditTrail.ForProject(project).Record(
                "ImportInterchangeUseSourceCatalog",
                string.Empty,
                "Imported source catalog semantic data from project " + source.Project.Id +
                ": zonesReplaced=" + plan.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                ", floorsReplaced=" + plan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                ", familiesReplaced=" + plan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) +
                ", existingElementCollisionsKept=" + plan.ElementCollisionsKept.ToString(CultureInfo.InvariantCulture) +
                ", invalidatedGeneratedClosure=" + invalidated.ToString(CultureInfo.InvariantCulture) +
                ", discardedIncomingSourceHandles=" + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static IReadOnlyList<string> ReplacementIds(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind) =>
            plan.Items
                .Where(x => x.Kind == kind && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData)
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

        private static int Count(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, InterchangeImportResolutionAction action) =>
            plan.Items.Count(x => x.Kind == kind && x.Action == action);

        private static InterchangeImportResolutionAction ResolutionAction(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, string id) =>
            plan.Items.Single(x => x.Kind == kind && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)).Action;

        private static void RestoreExistingActiveContext(
            ProjectState target,
            ProjectInterchangeValidatedSnapshot source,
            bool targetHadZones,
            bool targetHadFloors,
            bool targetHadFamilies,
            string previousActiveZoneId,
            string previousActiveFloorId,
            bool hadActiveFamilyMetadata,
            string previousActiveFamilyId)
        {
            if (targetHadZones) target.ActiveZoneId = previousActiveZoneId;
            if (targetHadFloors) target.ActiveFloorId = previousActiveFloorId;
            if (targetHadFamilies)
            {
                if (hadActiveFamilyMetadata) target.Metadata["ActiveFamilyId"] = previousActiveFamilyId;
                else target.Metadata.Remove("ActiveFamilyId");
            }
            else if (source.Families.Count > 0 && (!hadActiveFamilyMetadata || string.IsNullOrWhiteSpace(previousActiveFamilyId)))
            {
                var firstAddedFamily = source.Families
                    .Where(x => target.Families.Any(y => string.Equals(y.Id, x.Id, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (firstAddedFamily != null) ProjectFamilyActivationService.SetActive(target, firstAddedFamily.Id);
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }
    }
}
