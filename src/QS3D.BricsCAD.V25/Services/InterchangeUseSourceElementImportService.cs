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
    internal sealed class InterchangeUseSourceElementImportPlan
    {
        public string SourceProjectId { get; set; } = string.Empty;
        public int ZonesToAdd { get; set; }
        public int FloorsToAdd { get; set; }
        public int FamiliesToAdd { get; set; }
        public int ElementsToAdd { get; set; }
        public int ElementsToReplace { get; set; }
        public int SourceHandlesToDiscard { get; set; }
        public int TargetSourceHandlesToPreserve { get; set; }
        public int ValidationWarnings { get; set; }
        public IReadOnlyList<string> ReplacementElementIds { get; set; } = Array.Empty<string>();
    }

    internal sealed class InterchangeUseSourceElementImportResult
    {
        public int ZonesAdded { get; set; }
        public int FloorsAdded { get; set; }
        public int FamiliesAdded { get; set; }
        public int ElementsAdded { get; set; }
        public int ElementsReplaced { get; set; }
        public int GeneratedElementsInvalidated { get; set; }
        public int SourceHandlesDiscarded { get; set; }
        public int TargetSourceHandlesPreserved { get; set; }
    }

    internal static class InterchangeUseSourceElementImportService
    {
        private sealed class PreparedImport
        {
            public ProjectInterchangeValidatedSnapshot Source { get; set; } = null!;
            public ProjectInterchangeImportResolutionPlan Resolution { get; set; } = null!;
            public InterchangeUseSourceElementImportPlan Plan { get; set; } = null!;
        }

        private const string ImportMode = "UseSourceElementSemanticData";
        private const string LastElementsReplacedKey = "Interchange.LastImport.ElementsReplaced";
        private const string LastTargetSourceHandlesPreservedKey = "Interchange.LastImport.TargetSourceHandlesPreserved";

        public static InterchangeUseSourceElementImportPlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return Prepare(target, json).Plan;
        }

        public static InterchangeUseSourceElementImportResult Import(
            Document document,
            ProjectState authorizedProject,
            string json)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = InterchangeMutationTargetGuard.RequireExact(
                document,
                authorizedProject,
                "Interchange source-element import");
            var prepared = Prepare(project, json);
            EnsureActive(document, "Interchange source-element import / mutation");

            var replacementTargets = prepared.Plan.ReplacementElementIds
                .Select(id => project.FindElement(id) ?? throw new InvalidOperationException("Replacement target disappeared before mutation: " + id + "."))
                .ToList();
            var invalidationTargets = ExpandInvalidationTargets(project, replacementTargets, prepared.Source, prepared.Resolution);

            // Fail closed before taking a native transaction if Core recognizes an owner slot that
            // the current BricsCAD invalidator cannot prove it can erase safely.
            GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);

            ProjectStateSnapshot? rollback = null;
            var cadCommitted = false;
            var generatedElementsInvalidated = 0;

            try
            {
                using (document.LockDocument())
                {
                    EnsureActive(document, "Interchange source-element import / locked mutation");
                    var lockedProject = InterchangeMutationTargetGuard.RequireExact(
                        document,
                        project,
                        "Interchange source-element import / locked mutation");
                    var lockedReplacementTargets = prepared.Plan.ReplacementElementIds
                        .Select(id => lockedProject.FindElement(id) ?? throw new InvalidOperationException("Replacement target disappeared under the document lock: " + id + "."))
                        .ToList();
                    var lockedInvalidationTargets = ExpandInvalidationTargets(
                        lockedProject,
                        lockedReplacementTargets,
                        prepared.Source,
                        prepared.Resolution);

                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        rollback = ProjectStateSnapshot.Capture(lockedProject);

                        // Repeat against the exact locked closure immediately before native preparation.
                        GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-element import / pre-native cleanup");

                        var invalidation = GeneratedDependentGeometryInvalidator.Prepare(
                            document,
                            transaction,
                            lockedProject,
                            lockedInvalidationTargets);

                        // The document lock cannot prevent another process replacing the sidecar.
                        // Refuse semantic mutation after native prepare if the authoritative revision moved.
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-element import / pre-semantic apply");

                        var targetHadZones = lockedProject.Zones.Count > 0;
                        var targetHadFloors = lockedProject.Floors.Count > 0;
                        var targetHadFamilies = lockedProject.Families.Count > 0;
                        var previousActiveZoneId = lockedProject.ActiveZoneId ?? string.Empty;
                        var previousActiveFloorId = lockedProject.ActiveFloorId ?? string.Empty;
                        var hadActiveFamilyMetadata = lockedProject.Metadata.TryGetValue("ActiveFamilyId", out var previousActiveFamilyId);
                        previousActiveFamilyId = previousActiveFamilyId ?? string.Empty;

                        ApplyCatalogAdds(lockedProject, prepared.Source, prepared.Resolution);
                        ApplyElements(lockedProject, prepared.Source, prepared.Resolution);
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

                        // Native erasure was prepared from the old target-owned handle metadata while the
                        // CAD transaction is still rollback-capable. Clearing semantic ownership now keeps
                        // DWG and project state atomic if a later validation/commit step fails.
                        invalidation.CommitMetadata();

                        ValidateCombinedProject(lockedProject, json);
                        RecordImportMetadata(lockedProject, prepared.Source, prepared.Plan, invalidation.ElementCount);
                        lockedProject.Touch();

                        // Semantic state and native erasure are still rollback-capable at this point.
                        ProjectContextCoordinator.RequireBackingStoreUnchanged(
                            document,
                            lockedProject,
                            "Interchange source-element import / pre-CAD commit");

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
                            "Interchange source-element import failed before CAD commit and semantic rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new InterchangeUseSourceElementImportResult
            {
                ZonesAdded = prepared.Plan.ZonesToAdd,
                FloorsAdded = prepared.Plan.FloorsToAdd,
                FamiliesAdded = prepared.Plan.FamiliesToAdd,
                ElementsAdded = prepared.Plan.ElementsToAdd,
                ElementsReplaced = prepared.Plan.ElementsToReplace,
                GeneratedElementsInvalidated = generatedElementsInvalidated,
                SourceHandlesDiscarded = prepared.Plan.SourceHandlesToDiscard,
                TargetSourceHandlesPreserved = prepared.Plan.TargetSourceHandlesToPreserve
            };
        }

        private static PreparedImport Prepare(ProjectState target, string json)
        {
            // Reuse the existing strict target/snapshot validation without mutating the project.
            ProjectInterchangeKeepTargetImporter.Plan(target, json);
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var resolution = ProjectInterchangeImportResolutionPlanner.Plan(target, json, UseSourceElementPolicy());

            if (resolution.HasUnresolvedPolicy || resolution.HasBlocks || !resolution.CanProceedToMutationDesign)
            {
                var reasons = resolution.PolicyErrors
                    .Concat(resolution.GlobalBlocks)
                    .Concat(resolution.Items
                        .Where(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible || x.Action == InterchangeImportResolutionAction.Unresolved)
                        .Select(x => x.Kind + " " + x.Id + ": " + x.Reason))
                    .Take(8)
                    .ToArray();
                throw new InvalidOperationException("UseSource element interchange import is blocked" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons)));
            }

            foreach (var item in resolution.Items)
            {
                var allowed = item.Kind == InterchangeIdentityKind.Element
                    ? item.Action == InterchangeImportResolutionAction.AddSourceSemanticData || item.Action == InterchangeImportResolutionAction.UseSourceSemanticData
                    : item.Action == InterchangeImportResolutionAction.AddSourceSemanticData || item.Action == InterchangeImportResolutionAction.KeepTarget;
                if (!allowed)
                    throw new InvalidOperationException("UseSource element importer received unexpected resolution " + item.Action + " for " + item.Kind + " " + item.Id + ".");
                if (item.Kind == InterchangeIdentityKind.Element && item.Action == InterchangeImportResolutionAction.UseSourceSemanticData && !item.RequiresGeneratedOutputReset)
                    throw new InvalidOperationException("Element replacement lost the required generated-output reset contract for " + item.Id + ".");
            }

            var replacementIds = resolution.Items
                .Where(x => x.Kind == InterchangeIdentityKind.Element && x.Action == InterchangeImportResolutionAction.UseSourceSemanticData)
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();

            var sourceHandlesToDiscard = source.Elements.Sum(x => x.SourceHandles.Count);
            var targetSourceHandlesToPreserve = replacementIds.Sum(id =>
            {
                var targetElement = target.FindElement(id) ?? throw new InvalidOperationException("Planner selected missing replacement target " + id + ".");
                return targetElement.SourceHandles.Count;
            });

            return new PreparedImport
            {
                Source = source,
                Resolution = resolution,
                Plan = new InterchangeUseSourceElementImportPlan
                {
                    SourceProjectId = source.Project.Id,
                    ZonesToAdd = Count(resolution, InterchangeIdentityKind.Zone, InterchangeImportResolutionAction.AddSourceSemanticData),
                    FloorsToAdd = Count(resolution, InterchangeIdentityKind.Floor, InterchangeImportResolutionAction.AddSourceSemanticData),
                    FamiliesToAdd = Count(resolution, InterchangeIdentityKind.Family, InterchangeImportResolutionAction.AddSourceSemanticData),
                    ElementsToAdd = Count(resolution, InterchangeIdentityKind.Element, InterchangeImportResolutionAction.AddSourceSemanticData),
                    ElementsToReplace = replacementIds.Count,
                    SourceHandlesToDiscard = sourceHandlesToDiscard,
                    TargetSourceHandlesToPreserve = targetSourceHandlesToPreserve,
                    ValidationWarnings = source.Validation.WarningCount,
                    ReplacementElementIds = replacementIds
                }
            };
        }

        private static ProjectInterchangeImportPolicy UseSourceElementPolicy()
        {
            return new ProjectInterchangeImportPolicy
            {
                ZoneCollision = InterchangeExistingIdentityAction.KeepTarget,
                FloorCollision = InterchangeExistingIdentityAction.KeepTarget,
                FamilyCollision = InterchangeExistingIdentityAction.KeepTarget,
                ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
                ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
                DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
                SourceHandles = InterchangeSourceHandlePolicy.Discard,
                GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild
            };
        }

        private static void ApplyCatalogAdds(ProjectState project, ProjectInterchangeValidatedSnapshot source, ProjectInterchangeImportResolutionPlan resolution)
        {
            foreach (var zone in source.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                if (ShouldAdd(resolution, InterchangeIdentityKind.Zone, zone.Id))
                    ProjectZoneService.Create(project, zone.Id, zone.Name);

            foreach (var floor in source.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                if (ShouldAdd(resolution, InterchangeIdentityKind.Floor, floor.Id))
                    ProjectFloorService.Create(project, floor.Id, floor.Name, floor.ElevationM);

            foreach (var familySnapshot in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (!ShouldAdd(resolution, InterchangeIdentityKind.Family, familySnapshot.Id)) continue;
                var family = ProjectFamilyService.Create(project, familySnapshot.Id, familySnapshot.Name, familySnapshot.Category);
                foreach (var property in familySnapshot.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    family.Properties[property.Key] = property.Value ?? string.Empty;
            }
        }

        private static void ApplyElements(ProjectState project, ProjectInterchangeValidatedSnapshot source, ProjectInterchangeImportResolutionPlan resolution)
        {
            foreach (var snapshot in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Element, snapshot.Id);
                if (action == InterchangeImportResolutionAction.AddSourceSemanticData)
                {
                    var added = new ProjectElement(snapshot.Id, snapshot.Category, snapshot.FamilyId, snapshot.FloorId, snapshot.ZoneId)
                    {
                        DrawingFingerprint = string.Empty
                    };
                    CopyPortableElementState(added, snapshot);
                    project.Elements.Add(added);
                    continue;
                }

                if (action != InterchangeImportResolutionAction.UseSourceSemanticData)
                    throw new InvalidOperationException("Unexpected element mutation action " + action + " for " + snapshot.Id + ".");

                var target = project.FindElement(snapshot.Id) ?? throw new InvalidOperationException("Replacement target disappeared during mutation: " + snapshot.Id + ".");
                if (target.Category != snapshot.Category)
                    throw new InvalidOperationException("Replacement category changed after planning for " + snapshot.Id + ".");

                // Target drawing-local source ownership remains authoritative. Incoming source handles
                // are deliberately discarded, while target SourceHandles/DrawingFingerprint are preserved.
                target.FamilyId = snapshot.FamilyId;
                target.FloorId = snapshot.FloorId;
                target.ZoneId = snapshot.ZoneId;
                CopyPortableElementState(target, snapshot);
            }
        }

        private static void CopyPortableElementState(ProjectElement target, InterchangeElementSnapshot source)
        {
            target.DependsOn.Clear();
            foreach (var dependency in source.Dependencies)
                target.DependsOn.Add(dependency);

            target.Properties.Clear();
            foreach (var property in source.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                target.Properties[property.Key] = property.Value ?? string.Empty;

            target.Quantities.Clear();
            foreach (var quantity in source.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                target.Quantities[quantity.Key] = quantity.Value;

            target.MarkDirty(ElementDirtyFlags.All);
        }

        private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets(
            ProjectState project,
            IEnumerable<ProjectElement> replacementTargets,
            ProjectInterchangeValidatedSnapshot source,
            ProjectInterchangeImportResolutionPlan resolution)
        {
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<ProjectElement>();

            foreach (var element in replacementTargets)
                Enqueue(element, result, queue);

            // A newly accepted/replaced Door or Opening can make an existing target host's generated
            // solid untrustworthy even when the incoming HostWallId differs from the old target link.
            foreach (var snapshot in source.Elements)
            {
                var action = ResolutionAction(resolution, InterchangeIdentityKind.Element, snapshot.Id);
                if (action != InterchangeImportResolutionAction.AddSourceSemanticData && action != InterchangeImportResolutionAction.UseSourceSemanticData) continue;
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
                        throw new InvalidOperationException("Interchange invalidation graph returned missing semantic element " + dependentId + ".");
                    Enqueue(dependent, result, queue);
                }
            }

            return result.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void EnqueueExistingOpeningHost(ProjectElement element, DependencyGraph graph, IDictionary<string, ProjectElement> result, Queue<ProjectElement> queue)
        {
            if (element.Category != ElementCategory.Door && element.Category != ElementCategory.WallOpening) return;
            if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId)) return;
            if (!graph.TryGetElement(hostId.Trim(), out var host) || host == null)
                throw new InvalidOperationException("Opening " + element.Id + " references missing host " + hostId + ". Repair host linkage before interchange replacement.");
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
            // This re-runs the established strict target/reference validation against the now-mutated
            // project while still inside the rollback-capable native transaction.
            ProjectInterchangeKeepTargetImporter.Plan(project, json);
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);
            graph.TopologicalDirtyOrder(project.Elements);
        }

        private static void RecordImportMetadata(ProjectState project, ProjectInterchangeValidatedSnapshot source, InterchangeUseSourceElementImportPlan plan, int invalidated)
        {
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey] = ImportMode;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceProjectIdKey] = source.Project.Id;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceSchemaVersionKey] = source.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceDrawingFingerprintKey] = source.Project.DrawingFingerprint;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceUpdatedUtcKey] = source.Project.UpdatedUtcRaw;
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastImportedUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            project.Metadata[ProjectInterchangeAppendOnlyImporter.LastSourceHandlesDiscardedKey] = plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastElementsReplacedKey] = plan.ElementsToReplace.ToString(CultureInfo.InvariantCulture);
            project.Metadata[LastTargetSourceHandlesPreservedKey] = plan.TargetSourceHandlesToPreserve.ToString(CultureInfo.InvariantCulture);

            AuditTrail.ForProject(project).Record(
                "ImportInterchangeUseSourceElements",
                string.Empty,
                "Imported semantic snapshot from project " + source.Project.Id +
                " with UseSource element policy: added=" + (plan.ZonesToAdd + plan.FloorsToAdd + plan.FamiliesToAdd + plan.ElementsToAdd).ToString(CultureInfo.InvariantCulture) +
                ", replacedElements=" + plan.ElementsToReplace.ToString(CultureInfo.InvariantCulture) +
                ", invalidatedGeneratedClosure=" + invalidated.ToString(CultureInfo.InvariantCulture) +
                ", discardedIncomingSourceHandles=" + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) +
                ", preservedTargetSourceHandles=" + plan.TargetSourceHandlesToPreserve.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static int Count(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, InterchangeImportResolutionAction action) =>
            plan.Items.Count(x => x.Kind == kind && x.Action == action);

        private static bool ShouldAdd(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, string id) =>
            ResolutionAction(plan, kind, id) == InterchangeImportResolutionAction.AddSourceSemanticData;

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
