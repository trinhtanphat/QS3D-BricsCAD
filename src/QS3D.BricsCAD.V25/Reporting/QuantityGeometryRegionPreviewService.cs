using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Reporting
{
    /// <summary>
    /// Builds disposable, non-database Solid3d clones that visualize an already-validated
    /// QuantityGeometryDeduction. This service never writes CAD/project state and never
    /// recalculates authoritative quantity values; it only reconstructs the native region
    /// represented by the current deduction for transient display.
    /// </summary>
    internal static class QuantityGeometryRegionPreviewService
    {
        public static IReadOnlyList<Solid3d> Build(
            Document document,
            ProjectState geometryProject,
            string targetElementId,
            QuantityGeometryDeduction deduction,
            QuantityGeometryTolerances? tolerances = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (geometryProject == null) throw new ArgumentNullException(nameof(geometryProject));
            if (string.IsNullOrWhiteSpace(targetElementId)) throw new ArgumentException("Target element id is required.", nameof(targetElementId));
            if (deduction == null) throw new ArgumentNullException(nameof(deduction));
            if (string.IsNullOrWhiteSpace(deduction.ElementId)) throw new ArgumentException("Deduction element id is required.", nameof(deduction));
            tolerances ??= new QuantityGeometryTolerances();

            var target = geometryProject.FindElement(targetElementId.Trim())
                ?? throw new InvalidOperationException("Target element no longer exists: " + targetElementId.Trim());
            var cause = geometryProject.FindElement(deduction.ElementId.Trim())
                ?? throw new InvalidOperationException("Deduction element no longer exists: " + deduction.ElementId.Trim());

            var targetHandles = SourceHandleResolver.Resolve(geometryProject, new[] { target.Id });
            var causeHandles = SourceHandleResolver.Resolve(geometryProject, new[] { cause.Id });
            var targetIds = CadHandleService.Resolve(document, targetHandles);
            var causeIds = CadHandleService.Resolve(document, causeHandles);
            var targetSolids = CloneLiveSolids(document, targetIds);
            var causeSolids = CloneLiveSolids(document, causeIds);
            var regions = new List<Solid3d>();

            var lengthToMeter = LengthToMeter(document.Database.Insunits);
            var distanceCad = tolerances.Distance / lengthToMeter;
            var volumeCadTolerance = tolerances.Volume / (lengthToMeter * lengthToMeter * lengthToMeter);
            var contactLike = deduction.Relation == QuantityGeometryRelation.FaceContact ||
                              deduction.Relation == QuantityGeometryRelation.FaceOverlap;

            try
            {
                foreach (var targetSolid in targetSolids)
                {
                    foreach (var causeSolid in causeSolids)
                    {
                        if (!BoundingBoxesMayOverlap(targetSolid, causeSolid, distanceCad)) continue;
                        Solid3d? region = null;
                        try
                        {
                            if (contactLike)
                            {
                                using (var probe = Clone(causeSolid))
                                {
                                    if (!TryOffset(probe, distanceCad)) continue;
                                    region = TryIntersection(targetSolid, probe);
                                }
                            }
                            else
                            {
                                region = TryIntersection(targetSolid, causeSolid);
                            }

                            if (region == null) continue;
                            if (SafeVolumeCad(region) <= volumeCadTolerance)
                            {
                                region.Dispose();
                                region = null;
                                continue;
                            }

                            regions.Add(region);
                            region = null;
                        }
                        finally
                        {
                            region?.Dispose();
                        }
                    }
                }
                return regions.AsReadOnly();
            }
            catch
            {
                foreach (var region in regions) region.Dispose();
                throw;
            }
            finally
            {
                foreach (var solid in targetSolids) solid.Dispose();
                foreach (var solid in causeSolids) solid.Dispose();
            }
        }

        private static List<Solid3d> CloneLiveSolids(Document document, IEnumerable<ObjectId> ids)
        {
            var result = new List<Solid3d>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) continue;
                    result.Add(Clone(solid));
                }
                transaction.Commit();
            }
            return result;
        }

        private static Solid3d? TryIntersection(Solid3d target, Solid3d candidate)
        {
            try
            {
                var region = Clone(target);
                using (var cutter = Clone(candidate)) region.BooleanOperation(BooleanOperationType.BoolIntersect, cutter);
                return region;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                return null;
            }
        }

        private static bool TryOffset(Solid3d solid, double distanceCad)
        {
            if (distanceCad <= 0d) return false;
            try
            {
                solid.OffsetBody(distanceCad);
                return true;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                return false;
            }
        }

        private static bool BoundingBoxesMayOverlap(Solid3d left, Solid3d right, double toleranceCad)
        {
            try
            {
                var a = left.GeometricExtents;
                var b = right.GeometricExtents;
                return a.MinPoint.X <= b.MaxPoint.X + toleranceCad && a.MaxPoint.X + toleranceCad >= b.MinPoint.X &&
                       a.MinPoint.Y <= b.MaxPoint.Y + toleranceCad && a.MaxPoint.Y + toleranceCad >= b.MinPoint.Y &&
                       a.MinPoint.Z <= b.MaxPoint.Z + toleranceCad && a.MaxPoint.Z + toleranceCad >= b.MinPoint.Z;
            }
            catch
            {
                return true;
            }
        }

        private static double SafeVolumeCad(Solid3d solid)
        {
            try
            {
                using (var brep = new Brep(solid))
                {
                    var value = brep.GetVolume();
                    return double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value;
                }
            }
            catch
            {
                return 0d;
            }
        }

        private static double LengthToMeter(UnitsValue units)
        {
            switch ((int)units)
            {
                case 0: return 1d;
                case 1: return 0.0254d;
                case 2: return 0.3048d;
                case 3: return 1609.344d;
                case 4: return 0.001d;
                case 5: return 0.01d;
                case 6: return 1d;
                case 7: return 1000d;
                case 8: return 2.54e-8d;
                case 9: return 2.54e-5d;
                case 10: return 0.9144d;
                case 11: return 1e-10d;
                case 12: return 1e-9d;
                case 13: return 1e-6d;
                case 14: return 0.1d;
                case 15: return 10d;
                case 16: return 100d;
                case 17: return 1e9d;
                case 18: return 149597870700d;
                case 19: return 9.4607304725808e15d;
                case 20: return 3.0856775814913673e16d;
                case 21: return 1200d / 3937d;
                case 22: return 100d / 3937d;
                case 23: return 3600d / 3937d;
                case 24: return 6336000d / 3937d;
                default: return 1d;
            }
        }

        private static Solid3d Clone(Solid3d source) => (Solid3d)source.Clone();
        private static bool Recoverable(Exception ex) => !(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException);
    }
}
