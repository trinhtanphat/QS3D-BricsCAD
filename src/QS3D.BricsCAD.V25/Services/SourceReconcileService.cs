using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class SourceReconcileResult
    {
        public int Elements { get; set; }
        public int Regenerated { get; set; }
    }

    internal static class SourceReconcileService
    {
        private const int MaxStableRegenerationPasses = 8;

        private sealed class Target
        {
            public EntitySnapshot Snapshot { get; set; } = null!;
            public ProjectElement Element { get; set; } = null!;
        }

        public static SourceReconcileResult ReconcileSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            EnsureActive(document, "Source reconcile");

            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document)
                .GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            if (snapshots.Count == 0) return new SourceReconcileResult();

            var project = ExistingProjectMutationContext.Require(document, "Source Reconcile");
            var targets = ResolveTargets(project, snapshots);
            if (targets.Count == 0) return new SourceReconcileResult();
            EnsureActive(document, "Source reconcile / mutation");

            var invalidationTargets = ExpandInvalidationTargets(project, targets.Select(x => x.Element));
            var annotatedGridTargets = invalidationTargets.Where(HasGridAnnotationIntent).ToList();
            var sourceTargetIds = new HashSet<string>(targets.Select(x => x.Element.Id), StringComparer.OrdinalIgnoreCase);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            var regenerated = 0;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var invalidation = GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, invalidationTargets);
                    if (!CadUnitService.TryGetPolicy(document, out var units, out var unitResolution))
                        throw new InvalidOperationException("Drawing units are unresolved. Run QS3DUNITS before source reconcile.");
                    DrawingUnitResolutionPolicy.BindQuantityUnit(
                        project.Metadata,
                        project.Elements.Count > 0,
                        unitResolution.Unit,
                        unitResolution.Source);
                    foreach (var target in targets)
                        RefreshSourceDerivedState(project, target.Element, target.Snapshot, units);

                    foreach (var dependent in invalidationTargets.Where(x => !sourceTargetIds.Contains(x.Id)))
                        dependent.MarkDirty(ElementDirtyFlags.All);

                    regenerated = RegenerateAffectedToStable(project, invalidationTargets);

                    // Clear metadata for invalidated outputs before rebuilding optional Grid annotations.
                    // Only Grids that already had generated annotation before reconcile are rebuilt, so
                    // QS3DSYNCSOURCE preserves user intent without forcing annotation onto every Grid.
                    invalidation.CommitMetadata();
                    foreach (var grid in annotatedGridTargets)
                        GridAnnotationBuilder.RebuildInTransaction(document, transaction, project, grid);

                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Source reconcile failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new SourceReconcileResult { Elements = targets.Count, Regenerated = regenerated };
        }

        private static List<Target> ResolveTargets(ProjectState project, IReadOnlyList<EntitySnapshot> snapshots)
        {
            var targets = new List<Target>();
            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var generatedOwners = GeneratedHandleOwnershipIndex.Build(project);
            var sourceOwners = BuildSourceOwnerIndex(project);

            foreach (var snapshot in snapshots)
            {
                if (generatedOwners.TryFindOwner(snapshot.Handle, out var generatedOwner, out var generatedSlot))
                    throw new InvalidOperationException("Selected handle " + snapshot.Handle + " is QS3D-generated output owned by " + generatedOwner!.Id + "/" + generatedSlot + ". Select the authoritative source CAD instead.");

                if (!sourceOwners.TryGetValue(snapshot.Handle, out var matches) || matches.Count == 0)
                    throw new InvalidOperationException("Selected CAD source is not tracked by QS3D: " + snapshot.Handle + ". Capture it first instead of reconciling an unknown source.");
                if (matches.Count > 1)
                    throw new InvalidOperationException("Selected source handle " + snapshot.Handle + " belongs to multiple semantic elements. Repair source ownership before reconcile.");

                var element = matches[0];
                if (element.SourceHandles.Count != 1)
                    throw new InvalidOperationException("Source reconcile P0 requires exactly one authoritative source handle per semantic element: " + element.Id + ".");
                if (!seenElements.Add(element.Id))
                    throw new InvalidOperationException("Multiple selected CAD objects resolve to semantic element " + element.Id + ". Reconcile one authoritative source per element.");
                targets.Add(new Target { Snapshot = snapshot, Element = element });
            }
            return targets;
        }

        private static Dictionary<string, List<ProjectElement>> BuildSourceOwnerIndex(ProjectState project)
        {
            var index = new Dictionary<string, List<ProjectElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(handle)) continue;
                    if (!index.TryGetValue(handle, out var owners))
                    {
                        owners = new List<ProjectElement>(2);
                        index.Add(handle, owners);
                    }
                    if (owners.Count < 2) owners.Add(element);
                }
            }
            return index;
        }

        private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets(ProjectState project, IEnumerable<ProjectElement> sourceTargets)
        {
            var graph = new DependencyGraph();
            graph.Rebuild(project.Elements);

            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<ProjectElement>();
            foreach (var element in sourceTargets)
                EnqueueInvalidationTarget(element, result, queue);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                EnqueueOpeningHost(current, graph, result, queue);

                foreach (var dependentId in graph.GetDirectDependents(current.Id))
                {
                    if (!graph.TryGetElement(dependentId, out var dependent) || dependent == null)
                        throw new InvalidOperationException("Source reconcile dependency graph returned missing semantic element " + dependentId + ".");
                    EnqueueInvalidationTarget(dependent, result, queue);
                }
            }

            return result.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void EnqueueInvalidationTarget(ProjectElement element, IDictionary<string, ProjectElement> result, Queue<ProjectElement> queue)
        {
            if (result.ContainsKey(element.Id)) return;
            result.Add(element.Id, element);
            queue.Enqueue(element);
        }

        private static void EnqueueOpeningHost(
            ProjectElement element,
            DependencyGraph graph,
            IDictionary<string, ProjectElement> result,
            Queue<ProjectElement> queue)
        {
            if (element.Category != ElementCategory.Door && element.Category != ElementCategory.WallOpening) return;
            if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId)) return;
            var normalizedHostId = hostId.Trim();
            if (!graph.TryGetElement(normalizedHostId, out var host) || host == null)
                throw new InvalidOperationException("Opening " + element.Id + " references missing host " + hostId + ". Repair host linkage before source reconcile.");
            EnqueueInvalidationTarget(host, result, queue);
        }

        private static int RegenerateAffectedToStable(ProjectState project, IReadOnlyList<ProjectElement> affected)
        {
            var affectedIds = new HashSet<string>(affected.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            var total = 0;

            for (var pass = 0; pass < MaxStableRegenerationPasses; pass++)
            {
                var pending = affected.Count(HasSemanticDirty);
                if (pending == 0) return total;

                var regenerated = engine.RegenerateDirtySubset(project, affectedIds);
                total += regenerated;

                var remaining = affected.Count(HasSemanticDirty);
                if (remaining == 0) return total;
                if (regenerated == 0)
                    throw new InvalidOperationException("Source reconcile affected semantic closure could not regenerate to a stable state; " + remaining + " element(s) remain dirty.");
            }

            var unresolved = affected
                .Where(HasSemanticDirty)
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (unresolved.Length > 0)
                throw new InvalidOperationException("Source reconcile did not converge within " + MaxStableRegenerationPasses + " passes: " + string.Join(", ", unresolved) + ".");
            return total;
        }

        private static bool HasSemanticDirty(ProjectElement element) =>
            (element.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) != ElementDirtyFlags.None;

        private static bool HasGridAnnotationIntent(ProjectElement element) =>
            element.Category == ElementCategory.Grid &&
            element.Properties.TryGetValue(GridAnnotationBuilder.HandlesKey, out var raw) &&
            !string.IsNullOrWhiteSpace(raw);

        private static void RefreshSourceDerivedState(ProjectState project, ProjectElement element, EntitySnapshot snapshot, ProjectUnitPolicy units)
        {
            if (string.IsNullOrWhiteSpace(snapshot.Handle) || string.IsNullOrWhiteSpace(snapshot.EntityType))
                throw new InvalidOperationException("Source snapshot is missing required Handle/EntityType for " + element.Id + ".");
            if (!element.SourceHandles.Any(x => string.Equals(x, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Source snapshot no longer belongs to semantic element " + element.Id + ".");

            element.SetProperty("Layer", snapshot.Layer ?? string.Empty);
            // CAD.* is a replace-on-capture namespace. Native edits can remove optional
            // source metadata (for example clearing DBText/MText content), so retaining a
            // key merely because the new snapshot omits it would leave stale semantic data.
            foreach (var key in element.Properties.Keys
                .Where(x => x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase))
                .ToList())
                element.Properties.Remove(key);
            element.SetProperty("CAD.EntityType", snapshot.EntityType.Trim());
            element.SetProperty("CAD.Layer", snapshot.Layer ?? string.Empty);
            UpdateOptionalCadMetadata(element, snapshot, "Color", "CAD.Color");
            UpdateOptionalCadMetadata(element, snapshot, "IsLocked", "CAD.IsLocked");
            UpdateOptionalCadMetadata(element, snapshot, "OwnerSpace", "CAD.OwnerSpace");
            UpdateOptionalCadMetadata(element, snapshot, "IsCurrentSpace", "CAD.IsCurrentSpace");

            if (IsCurveType(snapshot.EntityType))
            {
                if (!snapshot.LengthDrawingUnits.HasValue || !Finite(snapshot.LengthDrawingUnits.Value) || snapshot.LengthDrawingUnits.Value < 0d)
                    throw new InvalidOperationException("Cannot refresh source length for " + element.Id + "/" + snapshot.Handle + ".");
                element.SetProperty("LengthM", units.ToMeters(snapshot.LengthDrawingUnits.Value).ToString("R", CultureInfo.InvariantCulture));
            }
            else if (snapshot.LengthDrawingUnits.HasValue)
            {
                element.SetProperty("LengthM", units.ToMeters(RequireFiniteNonNegative(snapshot.LengthDrawingUnits.Value, element.Id + "/Length")).ToString("R", CultureInfo.InvariantCulture));
            }

            if (snapshot.AreaDrawingUnitsSquared.HasValue)
            {
                var areaM2 = units.AreaToSquareMeters(RequireFiniteNonNegative(snapshot.AreaDrawingUnitsSquared.Value, element.Id + "/Area"));
                element.SetProperty("AreaM2", areaM2.ToString("R", CultureInfo.InvariantCulture));
                if (snapshot.LengthDrawingUnits.HasValue)
                    element.SetProperty("PerimeterM", units.ToMeters(RequireFiniteNonNegative(snapshot.LengthDrawingUnits.Value, element.Id + "/Perimeter")).ToString("R", CultureInfo.InvariantCulture));
            }
            else if (string.Equals(snapshot.EntityType, "Polyline", StringComparison.OrdinalIgnoreCase))
            {
                RemoveSourceMetric(element, "AreaM2");
                RemoveSourceMetric(element, "PerimeterM");
            }

            if (snapshot.SurfaceAreaDrawingUnitsSquared.HasValue)
                element.SetProperty(MeasuredSolidQuantityPolicy.SurfaceAreaProperty, units.AreaToSquareMeters(RequireFiniteNonNegative(snapshot.SurfaceAreaDrawingUnitsSquared.Value, element.Id + "/SurfaceArea")).ToString("R", CultureInfo.InvariantCulture));
            else element.Properties.Remove(MeasuredSolidQuantityPolicy.SurfaceAreaProperty);

            if (snapshot.VolumeDrawingUnitsCubed.HasValue)
                element.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, units.VolumeToCubicMeters(RequireFiniteNonNegative(snapshot.VolumeDrawingUnitsCubed.Value, element.Id + "/Volume")).ToString("R", CultureInfo.InvariantCulture));
            else element.Properties.Remove(MeasuredSolidQuantityPolicy.VolumeProperty);
            element.Properties.Remove("VolumeM3");
            if (snapshot.SurfaceAreaDrawingUnitsSquared.HasValue || snapshot.VolumeDrawingUnitsCubed.HasValue)
                element.SetProperty("CAD.SolidMetricSource", "Solid3d.MassProperties");
            else element.Properties.Remove("CAD.SolidMetricSource");

            foreach (var pair in snapshot.Metadata)
            {
                if (pair.Key.StartsWith("Internal.", StringComparison.OrdinalIgnoreCase)) continue;
                element.SetProperty("CAD." + pair.Key, pair.Value ?? string.Empty);
            }

            // Location/orientation edits can leave Length/Area numerically unchanged. Treat every
            // explicit reconcile as an authoritative geometry change so all generated outputs are
            // invalidated even when measured metrics happen to compare equal.
            element.MarkDirty(ElementDirtyFlags.All);
            AuditTrail.ForProject(project).Record("source.reconcile", element.Id, snapshot.EntityType + " " + snapshot.Handle);
        }

        private static void UpdateOptionalCadMetadata(ProjectElement element, EntitySnapshot snapshot, string sourceKey, string targetKey)
        {
            if (snapshot.Metadata.TryGetValue(sourceKey, out var value)) element.SetProperty(targetKey, value ?? string.Empty);
        }

        private static void RemoveSourceMetric(ProjectElement element, string key)
        {
            if (element.Properties.Remove(key)) element.MarkDirty(ElementDirtyFlags.All);
        }

        private static bool IsCurveType(string entityType) =>
            string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Polyline", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Spline", StringComparison.OrdinalIgnoreCase);

        private static double RequireFiniteNonNegative(double value, string label)
        {
            if (!Finite(value) || value < 0d) throw new InvalidOperationException(label + " must be a finite non-negative value.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }
    }
}
