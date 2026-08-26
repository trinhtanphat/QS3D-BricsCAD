using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GridSystemNativeMaterializer
    {
        private const string OperationName = "Grid system materialization";

        public static int Materialize(
            Document document,
            IReadOnlyList<GridReferenceCurve> plannedCurves,
            double elevationM = 0d)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (double.IsNaN(elevationM) || double.IsInfinity(elevationM))
                throw new ArgumentOutOfRangeException(nameof(elevationM), "Grid system elevation must be finite.");

            var plan = GridSystemMaterializationPlan.Create(plannedCurves);
            var project = ExistingProjectMutationContext.Require(document, OperationName);
            EnsureSemanticIdsAvailable(project, plan);
            var rollback = ProjectStateSnapshot.Capture(project);
            var units = CadUnitService.GetPolicy(document);
            var elevation = units.FromMeters(elevationM);
            var created = new List<PendingCapture>(plan.Count);

            try
            {
                using (document.LockDocument())
                {
                    // Commit the native source batch before semantic capture. Capture/regeneration owns
                    // its own transaction lifecycle and must never execute inside an uncommitted CAD
                    // transaction. If any later capture fails, the catch path erases every source from
                    // this committed batch and restores the semantic snapshot.
                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord
                            ?? throw new InvalidOperationException("Current drawing space is unavailable for Grid system materialization.");

                        foreach (var item in plan)
                        {
                            var native = CreateNativeCurve(item.Curve, units, elevation);
                            space.AppendEntity(native.Entity);
                            transaction.AddNewlyCreatedDBObject(native.Entity, true);

                            var handle = native.Entity.Handle.ToString();
                            if (string.IsNullOrWhiteSpace(handle))
                                throw new InvalidOperationException("Grid system native source did not receive a CAD handle for " + item.ElementId + ".");

                            var snapshot = new EntitySnapshot(handle, native.EntityType, native.Entity.Layer)
                            {
                                LengthDrawingUnits = native.LengthDrawingUnits
                            };
                            created.Add(new PendingCapture(item.ElementId, native.Entity.ObjectId, snapshot));
                        }

                        transaction.Commit();
                    }

                    foreach (var capture in created)
                    {
                        // Pre-register the planner-owned semantic id so canonical capture resolves this
                        // exact Grid instead of deriving GRID-<handle>. Capture remains responsible for
                        // family/default/metric/regeneration behavior.
                        var element = new ProjectElement(
                            capture.ElementId,
                            ElementCategory.Grid,
                            string.Empty,
                            project.ActiveFloorId,
                            project.ActiveZoneId);
                        element.SourceHandles.Add(capture.Snapshot.Handle);
                        project.Elements.Add(element);

                        if (!SemanticCaptureService.CaptureSnapshot(document, capture.Snapshot, ElementCategory.Grid))
                            throw new InvalidOperationException("Canonical Grid semantic capture returned no result for " + capture.ElementId + ".");
                    }

                    return created.Count;
                }
            }
            catch (Exception operationError)
            {
                Exception? cleanupError = null;
                try
                {
                    EraseCreatedSources(document, created);
                }
                catch (Exception error)
                {
                    cleanupError = error;
                }

                Exception? rollbackError = null;
                try
                {
                    rollback.Restore(project);
                }
                catch (Exception error)
                {
                    rollbackError = error;
                }

                if (cleanupError != null || rollbackError != null)
                {
                    var failures = new List<Exception> { operationError };
                    if (cleanupError != null) failures.Add(cleanupError);
                    if (rollbackError != null) failures.Add(rollbackError);
                    throw new InvalidOperationException(
                        OperationName + " failed and rollback/cleanup was incomplete.",
                        new AggregateException(failures));
                }
                throw;
            }
        }

        private static void EraseCreatedSources(Document document, IReadOnlyList<PendingCapture> created)
        {
            if (created.Count == 0) return;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var capture in created)
                {
                    if (capture.ObjectId.IsNull || capture.ObjectId.IsErased) continue;
                    var entity = transaction.GetObject(capture.ObjectId, OpenMode.ForWrite, false) as Entity;
                    if (entity != null && !entity.IsErased) entity.Erase();
                }
                transaction.Commit();
            }
        }

        private static void EnsureSemanticIdsAvailable(
            ProjectState project,
            IReadOnlyList<GridSystemMaterializationItem> plan)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element before Grid system materialization.");
                if (!existing.Add(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + element.Id + ".");
            }

            foreach (var item in plan)
                if (existing.Contains(item.ElementId))
                    throw new InvalidOperationException(
                        "Grid system semantic element id already exists: " + item.ElementId +
                        ". Reuse/review the existing Grid instead of creating a duplicate source.");
        }

        private static NativeCurve CreateNativeCurve(
            GridReferenceCurve curve,
            QS3D.Core.Units.ProjectUnitPolicy units,
            double elevationDrawingUnits)
        {
            if (curve.Kind == GridReferenceCurveKind.Line)
            {
                var start = new Point3d(units.FromMeters(curve.Start.X), units.FromMeters(curve.Start.Y), elevationDrawingUnits);
                var end = new Point3d(units.FromMeters(curve.End.X), units.FromMeters(curve.End.Y), elevationDrawingUnits);
                var entity = new Line(start, end);
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (!(length > 0d) || double.IsNaN(length) || double.IsInfinity(length))
                    throw new InvalidOperationException("Native Grid LINE length is not finite and positive for " + curve.ElementId + ".");
                return new NativeCurve(entity, "Line", length);
            }

            if (curve.Kind == GridReferenceCurveKind.Arc)
            {
                var center = new Point3d(units.FromMeters(curve.Center.X), units.FromMeters(curve.Center.Y), elevationDrawingUnits);
                var radius = units.FromMeters(curve.Radius);
                var endAngle = curve.StartAngleRad + curve.SweepAngleRad;
                if (!(radius > 0d) || double.IsNaN(radius) || double.IsInfinity(radius) ||
                    double.IsNaN(endAngle) || double.IsInfinity(endAngle))
                    throw new InvalidOperationException("Native Grid ARC geometry is not finite and positive for " + curve.ElementId + ".");
                var entity = new Arc(center, radius, curve.StartAngleRad, endAngle);
                var length = radius * curve.SweepAngleRad;
                if (!(length > 0d) || double.IsNaN(length) || double.IsInfinity(length))
                    throw new InvalidOperationException("Native Grid ARC length is not finite and positive for " + curve.ElementId + ".");
                return new NativeCurve(entity, "Arc", length);
            }

            throw new InvalidOperationException("Unsupported Grid system native curve kind for " + curve.ElementId + ".");
        }

        private sealed class PendingCapture
        {
            public PendingCapture(string elementId, ObjectId objectId, EntitySnapshot snapshot)
            {
                ElementId = elementId;
                ObjectId = objectId;
                Snapshot = snapshot;
            }

            public string ElementId { get; }
            public ObjectId ObjectId { get; }
            public EntitySnapshot Snapshot { get; }
        }

        private sealed class NativeCurve
        {
            public NativeCurve(Entity entity, string entityType, double lengthDrawingUnits)
            {
                Entity = entity ?? throw new ArgumentNullException(nameof(entity));
                EntityType = entityType;
                LengthDrawingUnits = lengthDrawingUnits;
            }

            public Entity Entity { get; }
            public string EntityType { get; }
            public double LengthDrawingUnits { get; }
        }
    }
}
