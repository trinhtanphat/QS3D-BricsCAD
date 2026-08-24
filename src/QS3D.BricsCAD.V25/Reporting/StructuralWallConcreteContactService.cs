using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using BrepFace = Teigha.BoundaryRepresentation.Face;

namespace QS3D.BricsCAD.V25.Reporting
{
    /// <summary>
    /// Measures the union of live concrete-contact regions on StructuralWall vertical faces.
    /// Bounding boxes are used only as a broad-phase rejection. The deduction itself is
    /// produced by Solid3d/BREP boolean residuals, including a small native offset probe for
    /// zero-volume face contacts.
    /// </summary>
    internal static class StructuralWallConcreteContactService
    {
        private const double HorizontalFaceNormalZ = 0.70710678118d;
        private const string GeneratedHostSolidOwnerSlot = "GeneratedSolidHandle";

        private sealed class FaceSeed
        {
            public FaceSeed(PlanarEntity plane, double grossAreaCad)
            {
                Plane = plane;
                GrossAreaCad = grossAreaCad;
            }

            public PlanarEntity Plane { get; }
            public double GrossAreaCad { get; }
        }

        public static bool IsConcreteContactCategory(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.StructuralWall:
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryMeasureM2(
            Document document,
            ProjectState project,
            ProjectElement wall,
            out double deductionM2)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (wall.Category != ElementCategory.StructuralWall)
                throw new ArgumentException("Concrete-contact measurement requires a StructuralWall target.", nameof(wall));

            deductionM2 = 0d;
            var tolerances = new QuantityGeometryTolerances();
            var lengthToMeter = LengthToMeter(document.Database.Insunits);
            var areaScale = lengthToMeter * lengthToMeter;
            var volumeScale = areaScale * lengthToMeter;
            var distanceCad = tolerances.Distance / lengthToMeter;
            var volumeCadTolerance = tolerances.Volume / volumeScale;

            // Contact is a live-BREP concern, not a Locate concern. Prefer the generated host
            // Solid3d owner even when the semantic element also has an authoritative LINE or
            // POLYLINE SourceHandle. Fall back to SourceHandles only when the source entity is
            // itself a live Solid3d (for example a native solid captured directly).
            var targetHandles = ResolveLiveSolidHandles(document, wall);
            if (targetHandles.Count == 0) return false;

            var targetIds = CadHandleService.Resolve(document, targetHandles);
            var targetHandleSet = new HashSet<string>(
                targetIds.Select(x => x.Handle.ToString()),
                StringComparer.OrdinalIgnoreCase);
            var targets = CloneSolids(document, targetIds);
            if (targets.Count == 0) return false;

            var candidates = new List<Solid3d>();
            try
            {
                foreach (var neighbor in project.Elements)
                {
                    if (string.Equals(neighbor.Id, wall.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsConcreteContactCategory(neighbor.Category)) continue;

                    var handles = ResolveLiveSolidHandles(document, neighbor);
                    if (handles.Count == 0) continue;

                    var ids = CadHandleService.Resolve(document, handles)
                        .Where(x => !targetHandleSet.Contains(x.Handle.ToString()))
                        .ToList();
                    candidates.AddRange(CloneSolids(document, ids));
                }

                var grossVerticalAreaCad = 0d;
                var residualVerticalAreaCad = 0d;
                foreach (var target in targets)
                {
                    var seeds = ReadVerticalFaces(target);
                    grossVerticalAreaCad += seeds.Sum(x => x.GrossAreaCad);
                    if (seeds.Count == 0) continue;

                    using (var residual = Clone(target))
                    {
                        foreach (var candidate in candidates)
                        {
                            if (!BoundingBoxesMayOverlap(target, candidate, distanceCad)) continue;

                            var volumeIntersection = false;
                            using (var intersection = TryIntersection(target, candidate))
                            {
                                if (intersection != null && SafeVolumeCad(intersection) > volumeCadTolerance)
                                {
                                    volumeIntersection = true;
                                    TrySubtract(residual, candidate);
                                }
                            }
                            if (volumeIntersection) continue;

                            using (var contactProbe = Clone(candidate))
                            {
                                if (!TryOffset(contactProbe, distanceCad)) continue;
                                using (var contact = TryIntersection(target, contactProbe))
                                {
                                    if (contact == null || SafeVolumeCad(contact) <= volumeCadTolerance) continue;
                                    TrySubtract(residual, contactProbe);
                                }
                            }
                        }

                        residualVerticalAreaCad += ReadResidualAreaOnOriginalVerticalFaces(
                            residual,
                            seeds,
                            distanceCad);
                    }
                }

                var deductionCad = Math.Max(0d, grossVerticalAreaCad - residualVerticalAreaCad);
                deductionM2 = deductionCad * areaScale;
                if (double.IsNaN(deductionM2) || double.IsInfinity(deductionM2) || deductionM2 < 0d)
                    throw new InvalidOperationException("Structural wall concrete-contact deduction is not finite and non-negative.");
                return true;
            }
            finally
            {
                foreach (var solid in candidates) solid.Dispose();
                foreach (var solid in targets) solid.Dispose();
            }
        }

        private static IReadOnlyList<string> ResolveLiveSolidHandles(Document document, ProjectElement element)
        {
            var generated = GeneratedHandleOwnershipPolicy
                .EnumerateLogicalOwnerHandles(element)
                .Where(x => GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(x.Value, GeneratedHostSolidOwnerSlot))
                .Select(x => x.Key)
                .ToList();
            var liveGenerated = CadHandleService.GetLiveSolidHandles(document, generated);
            if (liveGenerated.Count > 0)
                return liveGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();

            var liveSources = CadHandleService.GetLiveSolidHandles(document, element.SourceHandles);
            return liveSources.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static List<Solid3d> CloneSolids(Document document, IEnumerable<ObjectId> ids)
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

        private static List<FaceSeed> ReadVerticalFaces(Solid3d solid)
        {
            var result = new List<FaceSeed>();
            using (var brep = new Brep(solid))
            {
                foreach (BrepFace face in brep.Faces)
                {
                    var plane = face.Surface as PlanarEntity;
                    if (plane == null) continue;
                    var normal = plane.Normal.GetNormal();
                    if (Math.Abs(normal.Z) >= HorizontalFaceNormalZ) continue;
                    var areaCad = SafeAreaCad(face);
                    if (!(areaCad > 0d)) continue;
                    result.Add(new FaceSeed(
                        new Plane(plane.PointOnPlane, plane.Normal),
                        areaCad));
                }
            }
            return result;
        }

        private static double ReadResidualAreaOnOriginalVerticalFaces(
            Solid3d residual,
            IReadOnlyList<FaceSeed> seeds,
            double distanceCad)
        {
            var areaCad = 0d;
            using (var brep = new Brep(residual))
            {
                foreach (BrepFace face in brep.Faces)
                {
                    var plane = face.Surface as PlanarEntity;
                    if (plane == null) continue;
                    if (!seeds.Any(seed => SamePlane(seed.Plane, plane, distanceCad))) continue;
                    areaCad += SafeAreaCad(face);
                }
            }
            return areaCad;
        }

        private static bool SamePlane(PlanarEntity left, PlanarEntity right, double contactToleranceCad)
        {
            var leftNormal = left.Normal.GetNormal();
            var rightNormal = right.Normal.GetNormal();
            if (Math.Abs(leftNormal.DotProduct(rightNormal)) < 1d - 1e-7d) return false;

            // Keep plane identity much tighter than the positive contact-probe offset. This
            // prevents the inward cut face of the probe from being counted as the original
            // wall face, which would erase the contact deduction.
            var planeToleranceCad = Math.Max(contactToleranceCad * 1e-3d, 1e-12d);
            return Math.Abs((right.PointOnPlane - left.PointOnPlane).DotProduct(leftNormal)) <= planeToleranceCad;
        }

        private static Solid3d? TryIntersection(Solid3d target, Solid3d candidate)
        {
            try
            {
                var intersection = Clone(target);
                using (var cutter = Clone(candidate))
                    intersection.BooleanOperation(BooleanOperationType.BoolIntersect, cutter);
                return intersection;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                return null;
            }
        }

        private static void TrySubtract(Solid3d target, Solid3d cutterSource)
        {
            try
            {
                using (var cutter = Clone(cutterSource))
                    target.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                // Match the existing quantity-geometry service's recoverable native-boolean
                // behavior: leave the residual unchanged rather than inventing contact area.
            }
        }

        private static bool TryOffset(Solid3d solid, double distanceCad)
        {
            if (!(distanceCad > 0d)) return false;
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
                // If extents are unavailable, do not decide contact from the broad phase.
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

        private static double SafeAreaCad(BrepFace face)
        {
            try
            {
                var value = face.GetArea();
                return double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value;
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

        private static bool Recoverable(Exception ex) =>
            !(ex is OutOfMemoryException) &&
            !(ex is StackOverflowException) &&
            !(ex is AccessViolationException);
    }
}
