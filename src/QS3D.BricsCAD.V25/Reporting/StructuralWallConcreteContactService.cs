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
    /// Bounding boxes are used only as a broad-phase rejection. Native Solid3d/BREP booleans
    /// union-resolve the cutters, while deduction authority remains the area of original wall
    /// face patches reached from the cutter's exterior side. This prevents penetration-created
    /// side strips from being misreported as formwork contact. A small native offset probe is
    /// retained for zero-volume face contacts.
    /// </summary>
    internal static class StructuralWallConcreteContactService
    {
        private const double HorizontalFaceNormalZ = 0.70710678118d;
        private const string GeneratedHostSolidOwnerSlot = "GeneratedSolidHandle";

        internal sealed class StructuralWallConcreteContactDiagnostics
        {
            public int TargetSolidCount { get; internal set; }
            public int CandidateSolidCount { get; internal set; }
            public int VerticalFaceSeedCount { get; internal set; }
            public int PositiveVolumeCutCount { get; internal set; }
            public int ContactProbeCutCount { get; internal set; }
            public int FailedNativeCutCount { get; internal set; }
            public int DirectIntersectionFailureCount { get; internal set; }
            public int ContactProbeOffsetFailureCount { get; internal set; }
            public int ContactProbeIntersectionFailureCount { get; internal set; }
            public int ContactProbeEmptyRegionCount { get; internal set; }
            public int ContactProbeFaceReadFailureCount { get; internal set; }
            public int ContactProbeNoEligibleFaceCount { get; internal set; }
            public int ContactProbeSubtractFailureCount { get; internal set; }
            public double GrossVerticalAreaM2 { get; internal set; }
            public double ResidualVerticalAreaM2 { get; internal set; }
            public double DeductionM2 { get; internal set; }
        }

        private sealed class FaceSeed
        {
            public FaceSeed(PlanarEntity plane, double grossAreaCad, int interiorSide)
            {
                Plane = plane;
                GrossAreaCad = grossAreaCad;
                InteriorSide = interiorSide;
            }

            public PlanarEntity Plane { get; }
            public double GrossAreaCad { get; }
            public int InteriorSide { get; }
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
            return TryMeasureM2(document, project, wall, out deductionM2, out _);
        }

        internal static bool TryMeasureM2(
            Document document,
            ProjectState project,
            ProjectElement wall,
            out double deductionM2,
            out StructuralWallConcreteContactDiagnostics diagnostics)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (wall.Category != ElementCategory.StructuralWall)
                throw new ArgumentException("Concrete-contact measurement requires a StructuralWall target.", nameof(wall));

            deductionM2 = 0d;
            diagnostics = new StructuralWallConcreteContactDiagnostics();
            var tolerances = new QuantityGeometryTolerances();
            var lengthToMeter = LengthToMeter(document.Database.Insunits);
            var areaScale = lengthToMeter * lengthToMeter;
            var volumeScale = areaScale * lengthToMeter;
            var distanceCad = tolerances.Distance / lengthToMeter;
            // BricsCAD V25's native ACIS OffsetBody rejects the 1e-6 m quantity tolerance on
            // otherwise valid touching solids. Keep topology/plane tests on distanceCad, but give
            // only the native positive-volume probe a unit-aware 10 micrometre modeler-stable floor.
            var contactProbeDistanceCad = Math.Max(distanceCad, 1e-5d / lengthToMeter);
            var areaCadTolerance = tolerances.Area / areaScale;
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
            diagnostics.TargetSolidCount = targets.Count;
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
                diagnostics.CandidateSolidCount = candidates.Count;

                var grossVerticalAreaCad = 0d;
                var contactAreaCad = 0d;
                foreach (var target in targets)
                {
                    var seeds = ReadVerticalFaces(target, distanceCad);
                    diagnostics.VerticalFaceSeedCount += seeds.Count;
                    grossVerticalAreaCad += seeds.Sum(x => x.GrossAreaCad);
                    if (seeds.Count == 0) continue;

                    using (var residual = Clone(target))
                    {
                        foreach (var candidate in candidates)
                        {
                            if (!BoundingBoxesMayOverlap(target, candidate, distanceCad)) continue;

                            // Always clip a cutter to the *current residual* before subtraction.
                            // This keeps contact patches union-resolved. However, the area authority
                            // is not gross-minus-residual: a penetrating cutter creates extra side
                            // boundaries in the residual. Measure only original target-face patches
                            // for which the original candidate actually reaches the exterior side.
                            // A zero-volume touching BoolIntersect can throw in BricsCAD V25. Defer
                            // that preliminary failure to the contact probe instead of rejecting the
                            // candidate before the positive-offset touching path gets a chance to run.
                            var directIntersectionFailed = false;
                            using (var overlap = TryIntersection(residual, candidate, out var intersectionFailed))
                            {
                                directIntersectionFailed = intersectionFailed;
                                if (intersectionFailed) diagnostics.DirectIntersectionFailureCount++;
                                if (!intersectionFailed && overlap != null && SafeVolumeCad(overlap) > volumeCadTolerance)
                                {
                                    var overlapContactAreaCad = ReadEligibleOriginalFaceArea(
                                        overlap,
                                        candidate,
                                        seeds,
                                        distanceCad,
                                        out var overlapFaceReadFailed);
                                    if (overlapFaceReadFailed)
                                    {
                                        diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }

                                    // A positive volume overlap entirely on the interior side of all
                                    // original wall faces is embedded concrete, not formwork contact.
                                    if (overlapContactAreaCad <= areaCadTolerance) continue;

                                    if (!TrySubtract(residual, overlap))
                                    {
                                        diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }
                                    contactAreaCad += overlapContactAreaCad;
                                    diagnostics.PositiveVolumeCutCount++;
                                    continue;
                                }
                            }

                            using (var contactProbe = Clone(candidate))
                            {
                                if (!TryOffset(contactProbe, contactProbeDistanceCad))
                                {
                                    diagnostics.ContactProbeOffsetFailureCount++;
                                    diagnostics.FailedNativeCutCount++;
                                    continue;
                                }

                                using (var contact = TryIntersection(residual, contactProbe, out var contactIntersectionFailed))
                                {
                                    if (contactIntersectionFailed)
                                    {
                                        diagnostics.ContactProbeIntersectionFailureCount++;
                                        diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }
                                    if (contact == null || SafeVolumeCad(contact) <= volumeCadTolerance)
                                    {
                                        diagnostics.ContactProbeEmptyRegionCount++;
                                        if (directIntersectionFailed) diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }

                                    // The positive OffsetBody exists only to make zero-volume touching
                                    // topology modeler-stable. Never use its expanded tangential faces
                                    // as deduction authority: a 10 um offset grows a finite partial
                                    // patch and produced #3770's 0.080004 m2 instead of 0.080000 m2.
                                    var touchingSeeds = ReadEligibleTouchingFaceSeeds(
                                        contact,
                                        candidate,
                                        seeds,
                                        distanceCad,
                                        out var touchingSeedReadFailed);
                                    if (touchingSeedReadFailed)
                                    {
                                        diagnostics.ContactProbeFaceReadFailureCount++;
                                        diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }
                                    if (touchingSeeds.Count == 0)
                                    {
                                        diagnostics.ContactProbeNoEligibleFaceCount++;
                                        if (directIntersectionFailed) diagnostics.FailedNativeCutCount++;
                                        continue;
                                    }

                                    var resolvedTouchingCandidate = false;
                                    var footprintFailed = false;
                                    foreach (var touchingSeed in touchingSeeds)
                                    {
                                        using (var footprintContact = TryCreateFootprintContact(
                                            residual,
                                            candidate,
                                            touchingSeed,
                                            contactProbeDistanceCad,
                                            out var footprintIntersectionFailed))
                                        {
                                            if (footprintIntersectionFailed)
                                            {
                                                diagnostics.ContactProbeIntersectionFailureCount++;
                                                diagnostics.FailedNativeCutCount++;
                                                footprintFailed = true;
                                                break;
                                            }
                                            if (footprintContact == null || SafeVolumeCad(footprintContact) <= volumeCadTolerance)
                                                continue;

                                            var footprintContactAreaCad = ReadEligibleOriginalFaceArea(
                                                footprintContact,
                                                candidate,
                                                new[] { touchingSeed },
                                                distanceCad,
                                                out var footprintFaceReadFailed);
                                            if (footprintFaceReadFailed)
                                            {
                                                diagnostics.ContactProbeFaceReadFailureCount++;
                                                diagnostics.FailedNativeCutCount++;
                                                footprintFailed = true;
                                                break;
                                            }
                                            if (footprintContactAreaCad <= areaCadTolerance)
                                                continue;

                                            if (!TrySubtract(residual, footprintContact))
                                            {
                                                diagnostics.ContactProbeSubtractFailureCount++;
                                                diagnostics.FailedNativeCutCount++;
                                                footprintFailed = true;
                                                break;
                                            }

                                            contactAreaCad += footprintContactAreaCad;
                                            diagnostics.ContactProbeCutCount++;
                                            resolvedTouchingCandidate = true;
                                        }
                                    }

                                    if (footprintFailed) continue;
                                    if (!resolvedTouchingCandidate && directIntersectionFailed)
                                        diagnostics.FailedNativeCutCount++;
                                }
                            }
                        }
                    }
                }

                diagnostics.GrossVerticalAreaM2 = grossVerticalAreaCad * areaScale;
                diagnostics.ResidualVerticalAreaM2 = Math.Max(0d, grossVerticalAreaCad - contactAreaCad) * areaScale;

                // Once a broad-phase candidate reached a native contact operation, a failed
                // intersect/subtract/offset/topology-side read makes the measurement unavailable.
                // Publishing zero here would turn a modeling-kernel failure into a false no-contact.
                if (diagnostics.FailedNativeCutCount > 0) return false;

                deductionM2 = Math.Min(grossVerticalAreaCad, contactAreaCad) * areaScale;
                diagnostics.DeductionM2 = deductionM2;
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
            // A generated host can remain physically live after semantic/source edits while
            // its stale marker says it no longer represents current geometry. Never publish
            // contact deductions from that old BREP. A direct Solid3d source remains a valid
            // fallback because the source object itself is authoritative.
            if (!element.IsGeneratedSolidStale())
            {
                var generated = GeneratedHandleOwnershipPolicy
                    .EnumerateLogicalOwnerHandles(element)
                    .Where(x => string.Equals(x.Value, GeneratedHostSolidOwnerSlot, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Key)
                    .ToList();
                var liveGenerated = CadHandleService.GetLiveSolidHandles(document, generated);
                if (liveGenerated.Count > 0)
                    return liveGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            }

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

        private static List<FaceSeed> ReadVerticalFaces(Solid3d solid, double distanceCad)
        {
            var result = new List<FaceSeed>();
            using (var brep = new Brep(solid))
            {
                foreach (BrepFace face in brep.Faces)
                {
                    var plane = ReadFacePlane(face);
                    if (plane == null) continue;
                    var normal = plane.Normal.GetNormal();
                    if (Math.Abs(normal.Z) >= HorizontalFaceNormalZ) continue;
                    var areaCad = SafeAreaCad(face);
                    if (!(areaCad > 0d)) continue;
                    result.Add(new FaceSeed(plane, areaCad, ReadBoundaryInteriorSide(solid, plane, distanceCad)));
                }
            }
            return result;
        }

        private static double ReadEligibleOriginalFaceArea(
            Solid3d contactRegion,
            Solid3d candidate,
            IReadOnlyList<FaceSeed> seeds,
            double distanceCad,
            out bool failed)
        {
            failed = false;
            var areaCad = 0d;
            try
            {
                using (var brep = new Brep(contactRegion))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = ReadFacePlane(face);
                        if (plane == null) continue;
                        var seed = seeds.FirstOrDefault(x => SamePlane(x.Plane, plane, distanceCad));
                        if (seed == null) continue;
                        if (!CandidateReachesExterior(candidate, seed, distanceCad, out var sideReadFailed))
                        {
                            if (sideReadFailed)
                            {
                                failed = true;
                                return 0d;
                            }
                            continue;
                        }
                        areaCad += SafeAreaCad(face);
                    }
                }
                return areaCad;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                failed = true;
                return 0d;
            }
        }

        private static IReadOnlyList<FaceSeed> ReadEligibleTouchingFaceSeeds(
            Solid3d contactRegion,
            Solid3d candidate,
            IReadOnlyList<FaceSeed> seeds,
            double distanceCad,
            out bool failed)
        {
            failed = false;
            var result = new List<FaceSeed>();
            try
            {
                using (var brep = new Brep(contactRegion))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = ReadFacePlane(face);
                        if (plane == null) continue;
                        var seed = seeds.FirstOrDefault(x => SamePlane(x.Plane, plane, distanceCad));
                        if (seed == null || result.Contains(seed)) continue;
                        if (!CandidateReachesExterior(candidate, seed, distanceCad, out var sideReadFailed))
                        {
                            if (sideReadFailed)
                            {
                                failed = true;
                                return result.AsReadOnly();
                            }
                            continue;
                        }
                        result.Add(seed);
                    }
                }
                return result.AsReadOnly();
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                failed = true;
                return result.AsReadOnly();
            }
        }

        private static Solid3d? TryCreateFootprintContact(
            Solid3d residual,
            Solid3d candidate,
            FaceSeed seed,
            double contactProbeDistanceCad,
            out bool failed)
        {
            failed = false;
            if (seed.InteriorSide == 0 || !(contactProbeDistanceCad > 0d))
            {
                failed = true;
                return null;
            }

            try
            {
                using (var footprintProbe = Clone(candidate))
                {
                    var displacement = seed.Plane.Normal.GetNormal()
                        .MultiplyBy(seed.InteriorSide * contactProbeDistanceCad);
                    footprintProbe.TransformBy(Matrix3d.Displacement(displacement));
                    return TryIntersection(residual, footprintProbe, out failed);
                }
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                failed = true;
                return null;
            }
        }

        private static bool CandidateReachesExterior(
            Solid3d candidate,
            FaceSeed seed,
            double distanceCad,
            out bool failed)
        {
            failed = false;
            if (seed.InteriorSide == 0)
            {
                failed = true;
                return false;
            }

            if (!TryReadSolidVertexSideRange(candidate, seed.Plane, out var minDistance, out var maxDistance))
            {
                failed = true;
                return false;
            }

            // InteriorSide is the half-space occupied by the target solid. Contact is eligible
            // only when the cutter has exact BREP topology on the opposite half-space. Vertices
            // that are merely coplanar plus interior (the #3697 penetration side strips) do not
            // satisfy this test. Touching-only neighbors still have their body on the exterior.
            return seed.InteriorSide > 0
                ? minDistance < -distanceCad
                : maxDistance > distanceCad;
        }

        private static int ReadBoundaryInteriorSide(Solid3d solid, PlanarEntity plane, double distanceCad)
        {
            if (!TryReadSolidVertexSideRange(solid, plane, out var minDistance, out var maxDistance)) return 0;
            var hasNegative = minDistance < -distanceCad;
            var hasPositive = maxDistance > distanceCad;
            if (hasNegative == hasPositive) return 0;
            return hasPositive ? 1 : -1;
        }

        private static bool TryReadSolidVertexSideRange(
            Solid3d solid,
            PlanarEntity plane,
            out double minDistance,
            out double maxDistance)
        {
            minDistance = double.PositiveInfinity;
            maxDistance = double.NegativeInfinity;
            var count = 0;
            try
            {
                var normal = plane.Normal.GetNormal();
                using (var brep = new Brep(solid))
                {
                    foreach (var vertex in brep.Vertices)
                    {
                        var signedDistance = (vertex.Point - plane.PointOnPlane).DotProduct(normal);
                        if (double.IsNaN(signedDistance) || double.IsInfinity(signedDistance)) return false;
                        minDistance = Math.Min(minDistance, signedDistance);
                        maxDistance = Math.Max(maxDistance, signedDistance);
                        count++;
                    }
                }
                return count > 0 &&
                       !double.IsInfinity(minDistance) &&
                       !double.IsInfinity(maxDistance);
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                return false;
            }
        }

        private static PlanarEntity? ReadFacePlane(BrepFace face)
        {
            var surface = face.Surface;
            if (surface is PlanarEntity planar)
                return new Plane(planar.PointOnPlane, planar.Normal);

            // BricsCAD V25 exposes ACIS BREP faces as ExternalBoundedSurface even when the
            // underlying geometry is planar. Unwrap the native base surface before deciding
            // that the face is non-planar; otherwise every wall face can be silently skipped.
            if (surface is ExternalBoundedSurface external &&
                external.IsPlane &&
                external.BaseSurface is PlanarEntity basePlane)
            {
                return new Plane(basePlane.PointOnPlane, basePlane.Normal);
            }

            return null;
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

        private static Solid3d? TryIntersection(Solid3d target, Solid3d candidate, out bool failed)
        {
            failed = false;
            try
            {
                var intersection = Clone(target);
                using (var cutter = Clone(candidate))
                    intersection.BooleanOperation(BooleanOperationType.BoolIntersect, cutter);
                return intersection;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                failed = true;
                return null;
            }
        }

        private static bool TrySubtract(Solid3d target, Solid3d cutterSource)
        {
            try
            {
                using (var cutter = Clone(cutterSource))
                    target.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                return true;
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                return false;
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
            // EntitySnapshotReader uses the host Solid3d mass-properties volume as the
            // authoritative native volume metric. Use the same surface for transient boolean
            // results first; some V25 transient BREP wrappers can report zero/unavailable
            // GetVolume even when the Solid3d mass properties are valid.
            try
            {
                var value = Math.Abs(solid.MassProperties.Volume);
                if (!double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d) return value;
            }
            catch
            {
            }

            try
            {
                using (var brep = new Brep(solid))
                {
                    var value = Math.Abs(brep.GetVolume());
                    return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
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
