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

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class SourceReconcileResult
    {
        public int Elements { get; set; }
        public int Regenerated { get; set; }
    }

    internal static class SourceReconcileService
    {
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

            var project = ProjectContextCoordinator.GetOrCreate(document);
            var targets = ResolveTargets(project, snapshots);
            if (targets.Count == 0) return new SourceReconcileResult();
            EnsureActive(document, "Source reconcile / mutation");

            var invalidationTargets = ExpandInvalidationTargets(project, targets.Select(x => x.Element));
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            var regenerated = 0;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var invalidation = GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, invalidationTargets);
                    var units = CadUnitService.GetPolicy(document);
                    foreach (var target in targets)
                        RefreshSourceDerivedState(project, target.Element, target.Snapshot, units);

                    regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                    invalidation.CommitMetadata();
                    project.Touch();
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
            foreach (var snapshot in snapshots)
            {
                var ownership = GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle, out var generatedOwner, out var generatedSlot);
                if (ownership == GeneratedHandleOwnershipLookupStatus.Ambiguous)
                    throw new InvalidOperationException("Selected handle " + snapshot.Handle + " has ambiguous generated ownership. Resolve Model Health before source reconcile.");
                if (ownership == GeneratedHandleOwnershipLookupStatus.Owned)
                    throw new InvalidOperationException("Selected handle " + snapshot.Handle + " is QS3D-generated output owned by " + generatedOwner!.Id + "/" + generatedSlot + ". Select the authoritative source CAD instead.");

                var matches = project.Elements
                    .Where(x => x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                    .Take(2)
                    .ToList();
                if (matches.Count == 0)
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

        private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets(ProjectState project, IEnumerable<ProjectElement> sourceTargets)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in sourceTargets)
            {
                result[element.Id] = element;
                if (element.Category != ElementCategory.Door && element.Category != ElementCategory.WallOpening) continue;
                if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId)) continue;
                var host = project.FindElement(hostId.Trim());
                if (host == null)
                    throw new InvalidOperationException("Opening " + element.Id + " references missing host " + hostId + ". Repair host linkage before source reconcile.");
                result[host.Id] = host;
            }
            return result.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void RefreshSourceDerivedState(ProjectState project, ProjectElement element, EntitySnapshot snapshot, CadUnitPolicy units)
        {
            if (string.IsNullOrWhiteSpace(snapshot.Handle) || string.IsNullOrWhiteSpace(snapshot.EntityType))
                throw new InvalidOperationException("Source snapshot is missing required Handle/EntityType for " + element.Id + ".");
            if (!element.SourceHandles.Any(x => string.Equals(x, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Source snapshot no longer belongs to semantic element " + element.Id + ".");

            element.SetProperty("Layer", snapshot.Layer ?? string.Empty);
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

            if (snapshot.VolumeDrawingUnitsCubed.HasValue)
                element.SetProperty("VolumeM3", units.VolumeToCubicMeters(RequireFiniteNonNegative(snapshot.VolumeDrawingUnitsCubed.Value, element.Id + "/Volume")).ToString("R", CultureInfo.InvariantCulture));

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
