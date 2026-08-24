using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using BrepFace = Teigha.BoundaryRepresentation.Face;

namespace QS3D.BricsCAD.V25.Reporting
{
    internal static class QuantityGeometryExplanationService
    {
        private sealed class OwnedSolid : IDisposable
        {
            public string ElementId = string.Empty;
            public string ElementName = string.Empty;
            public IReadOnlyList<string> Handles = Array.Empty<string>();
            public ObjectId SourceObjectId = ObjectId.Null;
            public Solid3d Solid = null!;
            public void Dispose() => Solid?.Dispose();
        }

        private sealed class FaceSeed
        {
            public int GlobalIndex;
            public int ComponentIndex;
            public string Id = string.Empty;
            public string Type = "Other";
            public double GrossAreaCad;
            public PlanarEntity? Plane;
            public bool IsOuterHorizontal;
            public string MeasurementKind = string.Empty;
            public double MeasurementLengthCad;
            public double MeasurementHeightCad;
        }

        public static QuantityGeometryExplanation Build(
            Document document,
            ProjectState project,
            string elementId,
            QuantityGeometryTolerances? tolerances = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("Quantity geometry element id is required.", nameof(elementId));
            tolerances ??= new QuantityGeometryTolerances();

            var targetElement = project.FindElement(elementId.Trim())
                ?? throw new InvalidOperationException("Quantity geometry element no longer exists: " + elementId.Trim());
            var diagnostics = new List<string>();
            var lengthToMeter = LengthToMeter(document.Database.Insunits, diagnostics);
            var areaScale = lengthToMeter * lengthToMeter;
            var volumeScale = areaScale * lengthToMeter;
            var distanceCad = tolerances.Distance / lengthToMeter;
            var areaCadTolerance = tolerances.Area / areaScale;
            var volumeCadTolerance = tolerances.Volume / volumeScale;

            var targetHandles = SourceHandleResolver.Resolve(project, new[] { targetElement.Id });
            var targetIds = CadHandleService.Resolve(document, targetHandles);
            var targetHandleSet = new HashSet<string>(targetIds.Select(x => x.Handle.ToString()), StringComparer.OrdinalIgnoreCase);
            var targetSolids = CloneSolids(document, targetIds, targetElement.Id, ElementName(project, targetElement), targetHandles);
            if (targetSolids.Count == 0)
                throw new InvalidOperationException("Cấu kiện " + targetElement.Id + " không có Solid3d live để diễn giải hình học.");

            var candidates = new List<OwnedSolid>();
            var residualForFormwork = new List<Solid3d>();
            try
            {
                foreach (var element in project.Elements)
                {
                    if (string.Equals(element.Id, targetElement.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    IReadOnlyList<string> handles;
                    try { handles = SourceHandleResolver.Resolve(project, new[] { element.Id }); }
                    catch (InvalidOperationException ex) { diagnostics.Add(element.Id + ": " + ex.Message); continue; }
                    var ids = CadHandleService.Resolve(document, handles)
                        .Where(id => !targetHandleSet.Contains(id.Handle.ToString()))
                        .ToList();
                    candidates.AddRange(CloneSolids(document, ids, element.Id, ElementName(project, element), handles));
                }

                var individualVolumeCad = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var individualAreaCad = new Dictionary<string, Dictionary<int, double>>(StringComparer.OrdinalIgnoreCase);
                var relation = new Dictionary<string, QuantityGeometryRelation>(StringComparer.OrdinalIgnoreCase);
                var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var faceSeeds = new List<FaceSeed>();
                var grossVolumeCad = 0d;
                var netVolumeCad = 0d;

                for (var componentIndex = 0; componentIndex < targetSolids.Count; componentIndex++)
                {
                    var targetOwned = targetSolids[componentIndex];
                    var target = targetOwned.Solid;
                    grossVolumeCad += SafeVolumeCad(target);
                    faceSeeds.AddRange(ReadFaces(
                        document,
                        targetOwned,
                        targetElement.Category,
                        componentIndex,
                        faceSeeds.Count,
                        areaCadTolerance,
                        distanceCad,
                        diagnostics));

                    using (var volumeResidual = Clone(target))
                    {
                        var formworkResidual = Clone(target);
                        residualForFormwork.Add(formworkResidual);

                        foreach (var candidate in candidates)
                        {
                            if (!BoundingBoxesMayOverlap(target, candidate.Solid, distanceCad)) continue;
                            dependencies.Add(candidate.ElementId);
                            using (var intersection = TryIntersection(target, candidate.Solid))
                            {
                                var intersectionVolumeCad = intersection == null ? 0d : SafeVolumeCad(intersection);
                                if (intersection != null && intersectionVolumeCad > volumeCadTolerance)
                                {
                                    Add(individualVolumeCad, candidate.ElementId, intersectionVolumeCad);
                                    relation[candidate.ElementId] = QuantityGeometryRelation.VolumeIntersection;
                                    AccumulateFaceCoverage(
                                        intersection,
                                        componentIndex,
                                        faceSeeds,
                                        individualAreaCad,
                                        candidate.ElementId,
                                        areaCadTolerance,
                                        distanceCad,
                                        diagnostics);
                                    TrySubtract(volumeResidual, candidate.Solid, diagnostics, "volume/" + candidate.ElementId);
                                    TrySubtract(formworkResidual, candidate.Solid, diagnostics, "formwork/" + candidate.ElementId);
                                    continue;
                                }
                            }

                            using (var contactProbe = Clone(candidate.Solid))
                            {
                                if (!TryOffset(contactProbe, distanceCad)) continue;
                                using (var contact = TryIntersection(target, contactProbe))
                                {
                                    if (contact == null || SafeVolumeCad(contact) <= volumeCadTolerance) continue;
                                    var coveredCad = AccumulateFaceCoverage(
                                        contact,
                                        componentIndex,
                                        faceSeeds,
                                        individualAreaCad,
                                        candidate.ElementId,
                                        areaCadTolerance,
                                        distanceCad,
                                        diagnostics);
                                    if (coveredCad <= areaCadTolerance) continue;
                                    if (!relation.ContainsKey(candidate.ElementId)) relation[candidate.ElementId] = QuantityGeometryRelation.FaceContact;
                                    TrySubtract(formworkResidual, contactProbe, diagnostics, "contact/" + candidate.ElementId);
                                }
                            }
                        }
                        netVolumeCad += SafeVolumeCad(volumeResidual);
                    }
                }

                var faces = BuildFaceResults(
                    targetElement.Id,
                    targetElement.Category,
                    faceSeeds,
                    residualForFormwork,
                    candidates,
                    individualAreaCad,
                    relation,
                    areaScale,
                    lengthToMeter,
                    areaCadTolerance,
                    distanceCad,
                    diagnostics);
                var deductions = BuildVolumeDeductions(targetElement.Id, candidates, individualVolumeCad, relation, volumeScale);
                var deductionVolumeCad = Math.Max(0d, grossVolumeCad - netVolumeCad);
                if (individualVolumeCad.Values.Sum() + volumeCadTolerance < deductionVolumeCad)
                    diagnostics.Add("Union deduction exceeded the sum of individual intersections; geometry was retained fail-closed for review.");
                if (individualVolumeCad.Values.Sum() > deductionVolumeCad + volumeCadTolerance)
                    diagnostics.Add("Các vùng giao chồng nhau đã được trừ đúng một lần bằng residual boolean semantics.");

                var dependencyIds = dependencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
                var result = new QuantityGeometryExplanation
                {
                    ElementId = targetElement.Id,
                    ElementName = ElementName(project, targetElement),
                    SourceHandles = targetHandles.ToList().AsReadOnly(),
                    Dependencies = dependencyIds,
                    GeometryFingerprint = BuildFingerprint(targetElement, targetSolids, candidates, dependencyIds, lengthToMeter, tolerances),
                    IsDirty = IsSemanticallyDirty(project, targetElement, dependencyIds),
                    GrossVolume = grossVolumeCad * volumeScale,
                    DeductionVolume = deductionVolumeCad * volumeScale,
                    NetVolume = Math.Max(0d, netVolumeCad) * volumeScale,
                    VolumeDeductions = deductions,
                    FormworkFaces = faces,
                    Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToList().AsReadOnly()
                };
                result.Validate(tolerances);
                return result;
            }
            finally
            {
                foreach (var solid in residualForFormwork) solid.Dispose();
                foreach (var solid in targetSolids) solid.Dispose();
                foreach (var solid in candidates) solid.Dispose();
            }
        }

        private static List<OwnedSolid> CloneSolids(
            Document document,
            IEnumerable<ObjectId> ids,
            string elementId,
            string elementName,
            IReadOnlyList<string> handles)
        {
            var result = new List<OwnedSolid>();
            using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) continue;
                    result.Add(new OwnedSolid
                    {
                        ElementId = elementId,
                        ElementName = elementName,
                        Handles = handles,
                        SourceObjectId = id,
                        Solid = Clone(solid)
                    });
                }
                tr.Commit();
            }
            return result;
        }

        private static IReadOnlyList<QuantityGeometryDeduction> BuildVolumeDeductions(
            string targetElementId,
            IEnumerable<OwnedSolid> candidates,
            IReadOnlyDictionary<string, double> volumesCad,
            IReadOnlyDictionary<string, QuantityGeometryRelation> relations,
            double volumeScale)
        {
            var byId = candidates.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            return volumesCad.OrderByDescending(x => x.Value).Select(x =>
            {
                var candidate = byId[x.Key];
                return new QuantityGeometryDeduction
                {
                    ElementId = candidate.ElementId,
                    ElementName = candidate.ElementName,
                    Relation = relations.TryGetValue(candidate.ElementId, out var r) ? r : QuantityGeometryRelation.VolumeIntersection,
                    Volume = x.Value * volumeScale,
                    RegionKey = targetElementId + "|V|" + candidate.ElementId,
                    SourceHandles = candidate.Handles
                };
            }).ToList().AsReadOnly();
        }

        private static IReadOnlyList<QuantityFormworkFaceExplanation> BuildFaceResults(
            string targetElementId,
            ElementCategory targetCategory,
            IReadOnlyList<FaceSeed> seeds,
            IReadOnlyList<Solid3d> residuals,
            IReadOnlyList<OwnedSolid> candidates,
            IReadOnlyDictionary<string, Dictionary<int, double>> individualAreaCad,
            IReadOnlyDictionary<string, QuantityGeometryRelation> relations,
            double areaScale,
            double lengthToMeter,
            double areaCadTolerance,
            double distanceCad,
            ICollection<string> diagnostics)
        {
            var residualAreasCad = new double[seeds.Count];
            for (var componentIndex = 0; componentIndex < residuals.Count; componentIndex++)
            {
                using (var brep = new Brep(residuals[componentIndex]))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = ReadFacePlane(face);
                        if (plane == null) continue;
                        var areaCad = SafeAreaCad(face);
                        var best = FindMatchingFace(seeds, componentIndex, plane, distanceCad);
                        if (best >= 0) residualAreasCad[best] += areaCad;
                    }
                }
            }

            var candidateById = candidates.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var result = new List<QuantityFormworkFaceExplanation>();
            for (var index = 0; index < seeds.Count; index++)
            {
                var seed = seeds[index];
                if (!IncludeFormworkFace(targetCategory, seed.Type, seed.IsOuterHorizontal)) continue;
                var netCad = seed.Plane == null ? seed.GrossAreaCad : Math.Min(seed.GrossAreaCad, residualAreasCad[index]);
                if (seed.Plane == null)
                    diagnostics.Add(seed.Id + ": mặt không phẳng được giữ nguyên diện tích; cần native curved-face probe nếu phải khấu trừ mặt cong.");
                var deductionCad = Math.Max(0d, seed.GrossAreaCad - netCad);
                var rows = new List<QuantityGeometryDeduction>();
                foreach (var byElement in individualAreaCad)
                {
                    if (!byElement.Value.TryGetValue(index, out var areaCad) || areaCad <= areaCadTolerance) continue;
                    if (!candidateById.TryGetValue(byElement.Key, out var candidate)) continue;
                    rows.Add(new QuantityGeometryDeduction
                    {
                        ElementId = candidate.ElementId,
                        ElementName = candidate.ElementName,
                        Relation = relations.TryGetValue(candidate.ElementId, out var r) ? r : QuantityGeometryRelation.FaceOverlap,
                        Area = Math.Min(seed.GrossAreaCad, areaCad) * areaScale,
                        FaceId = seed.Id,
                        RegionKey = targetElementId + "|F|" + seed.Id + "|" + candidate.ElementId,
                        SourceHandles = candidate.Handles
                    });
                }
                result.Add(new QuantityFormworkFaceExplanation
                {
                    FaceId = seed.Id,
                    FaceType = seed.Type,
                    GrossArea = seed.GrossAreaCad * areaScale,
                    DeductionArea = deductionCad * areaScale,
                    NetArea = Math.Max(0d, seed.GrossAreaCad - deductionCad) * areaScale,
                    MeasurementKind = seed.MeasurementKind,
                    MeasurementLength = seed.MeasurementLengthCad * lengthToMeter,
                    MeasurementHeight = seed.MeasurementHeightCad * lengthToMeter,
                    Deductions = rows.OrderByDescending(x => x.Area).ToList().AsReadOnly()
                });
            }
            return result.AsReadOnly();
        }

        private static List<FaceSeed> ReadFaces(
            Document document,
            OwnedSolid ownedSolid,
            ElementCategory category,
            int componentIndex,
            int globalOffset,
            double areaCadTolerance,
            double distanceCad,
            ICollection<string> diagnostics)
        {
            var result = new List<FaceSeed>();
            var solid = ownedSolid.Solid;
            try
            {
                // Foundation formwork follows the canonical semantic rule S = perimeter × height:
                // all vertical perimeter faces are side faces, even for elongated rectangular pads.
                // Keeping the original BREP enumeration index preserves exact SOLID-xx/FACE-yy identity.
                var endAxis = category == ElementCategory.Foundation ? -1 : DominantHorizontalAxis(solid);
                var wallBoundsAvailable = false;
                var wallMinZ = 0d;
                var wallMaxZ = 0d;
                if (category == ElementCategory.StructuralWall)
                {
                    try
                    {
                        var ext = solid.GeometricExtents;
                        wallMinZ = ext.MinPoint.Z;
                        wallMaxZ = ext.MaxPoint.Z;
                        wallBoundsAvailable = true;
                    }
                    catch
                    {
                        diagnostics.Add("StructuralWall BREP extents unavailable: outer horizontal faces remain included to preserve opening reveals.");
                    }
                }

                var liveFaceExtents = ReadLiveFaceExtents(document, ownedSolid.SourceObjectId, diagnostics);
                using (var brep = new Brep(solid))
                {
                    var localIndex = 0;
                    foreach (BrepFace face in brep.Faces)
                    {
                        localIndex++;
                        var globalIndex = globalOffset + localIndex - 1;
                        var plane = ReadFacePlane(face);
                        var faceType = FaceType(plane, endAxis);
                        var grossAreaCad = SafeAreaCad(face);
                        var measurementKind = string.Empty;
                        var measurementLengthCad = 0d;
                        var measurementHeightCad = 0d;
                        if (liveFaceExtents.TryGetValue(localIndex, out var faceExtents) &&
                            TryBuildRectangleMeasurement(
                                faceType,
                                grossAreaCad,
                                faceExtents,
                                areaCadTolerance,
                                distanceCad,
                                out measurementLengthCad,
                                out measurementHeightCad))
                        {
                            measurementKind = "brep-rectangle-extents-v1";
                        }

                        result.Add(new FaceSeed
                        {
                            GlobalIndex = globalIndex,
                            ComponentIndex = componentIndex,
                            Id = "SOLID-" + (componentIndex + 1).ToString("00", CultureInfo.InvariantCulture) + "/FACE-" + localIndex.ToString("00", CultureInfo.InvariantCulture),
                            Type = faceType,
                            GrossAreaCad = grossAreaCad,
                            Plane = plane,
                            IsOuterHorizontal = IsOuterHorizontalFace(category, plane, wallBoundsAvailable, wallMinZ, wallMaxZ),
                            MeasurementKind = measurementKind,
                            MeasurementLengthCad = measurementLengthCad,
                            MeasurementHeightCad = measurementHeightCad
                        });
                    }
                }
            }
            catch (Exception ex) when (Recoverable(ex)) { diagnostics.Add("BREP face read: " + ex.Message); }
            return result;
        }

        private static IReadOnlyDictionary<int, Extents3d> ReadLiveFaceExtents(
            Document document,
            ObjectId sourceObjectId,
            ICollection<string> diagnostics)
        {
            var result = new Dictionary<int, Extents3d>();
            if (sourceObjectId.IsNull) return result;
            try
            {
                using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var liveSolid = tr.GetObject(sourceObjectId, OpenMode.ForRead, false) as Solid3d;
                    if (liveSolid == null || liveSolid.IsErased) return result;
                    var rootPath = new FullSubentityPath(new[] { liveSolid.ObjectId }, SubentityId.Null);
                    using (var brep = new Brep(rootPath))
                    {
                        var localIndex = 0;
                        foreach (BrepFace face in brep.Faces)
                        {
                            localIndex++;
                            try
                            {
                                result[localIndex] = liveSolid.GetSubentityGeometricExtents(face.SubentityPath);
                            }
                            catch (Exception ex) when (Recoverable(ex))
                            {
                                // Exact area remains authoritative. Missing subentity extents only
                                // suppress the optional length × height measurement trace.
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex) when (Recoverable(ex))
            {
                diagnostics.Add("BREP face measurement extents unavailable: " + ex.Message);
            }
            return result;
        }

        private static bool TryBuildRectangleMeasurement(
            string faceType,
            double grossAreaCad,
            Extents3d extents,
            double areaCadTolerance,
            double distanceCad,
            out double lengthCad,
            out double heightCad)
        {
            lengthCad = 0d;
            heightCad = 0d;
            if (!string.Equals(faceType, "Side", StringComparison.Ordinal) &&
                !string.Equals(faceType, "End", StringComparison.Ordinal))
                return false;
            if (!(grossAreaCad > areaCadTolerance)) return false;

            var dx = Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
            var dy = Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
            var dz = Math.Abs(extents.MaxPoint.Z - extents.MinPoint.Z);
            var horizontalSpan = Math.Sqrt(dx * dx + dy * dy);
            if (!(horizontalSpan > distanceCad) || !(dz > distanceCad)) return false;

            var measuredArea = horizontalSpan * dz;
            if (double.IsNaN(measuredArea) || double.IsInfinity(measuredArea)) return false;
            var tolerance = Math.Max(areaCadTolerance, Math.Abs(grossAreaCad) * 1e-8d);
            if (Math.Abs(measuredArea - grossAreaCad) > tolerance) return false;

            lengthCad = horizontalSpan;
            heightCad = dz;
            return true;
        }

        private static double AccumulateFaceCoverage(
            Solid3d intersection,
            int componentIndex,
            IReadOnlyList<FaceSeed> seeds,
            IDictionary<string, Dictionary<int, double>> accumulator,
            string elementId,
            double areaCadTolerance,
            double distanceCad,
            ICollection<string> diagnostics)
        {
            var totalCad = 0d;
            try
            {
                using (var brep = new Brep(intersection))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = ReadFacePlane(face);
                        if (plane == null) continue;
                        var seedIndex = FindMatchingFace(seeds, componentIndex, plane, distanceCad);
                        if (seedIndex < 0) continue;
                        var areaCad = SafeAreaCad(face);
                        if (areaCad <= areaCadTolerance) continue;
                        if (!accumulator.TryGetValue(elementId, out var byFace))
                            accumulator[elementId] = byFace = new Dictionary<int, double>();
                        Add(byFace, seedIndex, areaCad);
                        totalCad += areaCad;
                    }
                }
            }
            catch (Exception ex) when (Recoverable(ex)) { diagnostics.Add("BREP coverage/" + elementId + ": " + ex.Message); }
            return totalCad;
        }

        private static int FindMatchingFace(IReadOnlyList<FaceSeed> seeds, int componentIndex, PlanarEntity plane, double toleranceCad)
        {
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i].ComponentIndex != componentIndex) continue;
                var target = seeds[i].Plane;
                if (target == null) continue;
                if (SamePlane(target, plane, toleranceCad)) return seeds[i].GlobalIndex;
            }
            return -1;
        }

        private static PlanarEntity? ReadFacePlane(BrepFace face)
        {
            var surface = face.Surface;
            if (surface is PlanarEntity planar)
                return new Plane(planar.PointOnPlane, planar.Normal);

            // BricsCAD V25 can expose planar ACIS faces as ExternalBoundedSurface.
            // Unwrap the bounded base surface before deciding that a face is non-planar.
            if (surface is ExternalBoundedSurface external &&
                external.IsPlane &&
                external.BaseSurface is PlanarEntity basePlane)
            {
                return new Plane(basePlane.PointOnPlane, basePlane.Normal);
            }

            return null;
        }

        private static bool SamePlane(PlanarEntity left, PlanarEntity right, double toleranceCad)
        {
            var ln = left.Normal.GetNormal();
            var rn = right.Normal.GetNormal();
            if (Math.Abs(ln.DotProduct(rn)) < 1d - 1e-7) return false;
            // Plane identity must be materially stricter than the contact-probe offset.
            // Otherwise the probe's inward cut face can be mistaken for the original
            // target face and the contact deduction collapses back to zero.
            var planeToleranceCad = Math.Max(toleranceCad * 1e-3d, 1e-12d);
            return Math.Abs((right.PointOnPlane - left.PointOnPlane).DotProduct(ln)) <= planeToleranceCad;
        }

        private static bool IncludeFormworkFace(ElementCategory category, string faceType, bool isOuterHorizontal)
        {
            // BLT/QS3D foundation parity is intentionally side-only: top and bottom are not
            // formwork, and Foundation ReadFaces disables End classification so all four
            // vertical perimeter faces remain eligible.
            if (category == ElementCategory.Foundation)
                return string.Equals(faceType, "Side", StringComparison.Ordinal);

            // Wall formwork excludes only the wall solid's exterior top/bottom planes.
            // Internal horizontal BREP faces are opening reveals (head/soffit and optional
            // sill) and must remain eligible exactly like vertical jamb/end/broad faces.
            if (category == ElementCategory.StructuralWall)
                return !isOuterHorizontal;

            return true;
        }

        private static bool IsOuterHorizontalFace(
            ElementCategory category,
            PlanarEntity? plane,
            bool wallBoundsAvailable,
            double wallMinZ,
            double wallMaxZ)
        {
            if (category != ElementCategory.StructuralWall || plane == null || !wallBoundsAvailable) return false;
            var normal = plane.Normal.GetNormal();
            if (Math.Abs(normal.Z) < 0.70710678118d) return false;

            var span = Math.Abs(wallMaxZ - wallMinZ);
            var toleranceCad = Math.Max(span * 1e-8d, 1e-9d);
            var z = plane.PointOnPlane.Z;
            return Math.Abs(z - wallMinZ) <= toleranceCad || Math.Abs(z - wallMaxZ) <= toleranceCad;
        }

        private static int DominantHorizontalAxis(Solid3d solid)
        {
            try
            {
                var ext = solid.GeometricExtents;
                var dx = Math.Abs(ext.MaxPoint.X - ext.MinPoint.X);
                var dy = Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y);
                if (dx > dy * 1.25d) return 0;
                if (dy > dx * 1.25d) return 1;
            }
            catch { }
            return -1;
        }

        private static string FaceType(PlanarEntity? plane, int endAxis)
        {
            if (plane == null) return "Other";
            var normal = plane.Normal.GetNormal();
            if (normal.Z <= -0.70710678118d) return "Bottom";
            if (normal.Z >= 0.70710678118d) return "Top";
            if (endAxis == 0 && Math.Abs(normal.X) >= 0.70710678118d) return "End";
            if (endAxis == 1 && Math.Abs(normal.Y) >= 0.70710678118d) return "End";
            return "Side";
        }

        private static Solid3d? TryIntersection(Solid3d target, Solid3d candidate)
        {
            try
            {
                var intersection = Clone(target);
                using (var cutter = Clone(candidate)) intersection.BooleanOperation(BooleanOperationType.BoolIntersect, cutter);
                return intersection;
            }
            catch (Exception ex) when (Recoverable(ex)) { return null; }
        }

        private static void TrySubtract(Solid3d target, Solid3d cutterSource, ICollection<string> diagnostics, string label)
        {
            try { using (var cutter = Clone(cutterSource)) target.BooleanOperation(BooleanOperationType.BoolSubtract, cutter); }
            catch (Exception ex) when (Recoverable(ex)) { diagnostics.Add("Boolean subtract " + label + ": " + ex.Message); }
        }

        private static bool TryOffset(Solid3d solid, double distanceCad)
        {
            if (distanceCad <= 0d) return false;
            try { solid.OffsetBody(distanceCad); return true; }
            catch (Exception ex) when (Recoverable(ex)) { return false; }
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
            catch { return true; }
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
            catch { return 0d; }
        }

        private static double SafeAreaCad(BrepFace face)
        {
            try
            {
                var value = face.GetArea();
                return double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value;
            }
            catch { return 0d; }
        }

        private static double LengthToMeter(UnitsValue units, ICollection<string> diagnostics)
        {
            switch ((int)units)
            {
                case 0: diagnostics.Add("INSUNITS=Undefined: diễn giải hình học giả định 1 drawing unit = 1 metre."); return 1d;
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
                default: diagnostics.Add("INSUNITS không được hỗ trợ: giả định 1 drawing unit = 1 metre."); return 1d;
            }
        }

        private static bool IsSemanticallyDirty(ProjectState project, ProjectElement target, IEnumerable<string> dependencies)
        {
            const ElementDirtyFlags relevant = ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            if ((target.Dirty & relevant) != 0) return true;
            foreach (var id in dependencies)
            {
                var element = project.FindElement(id);
                if (element != null && (element.Dirty & relevant) != 0) return true;
            }
            return false;
        }

        private static string BuildFingerprint(
            ProjectElement target,
            IReadOnlyList<OwnedSolid> targetSolids,
            IReadOnlyList<OwnedSolid> candidates,
            IReadOnlyCollection<string> dependencyIds,
            double lengthToMeter,
            QuantityGeometryTolerances tolerances)
        {
            var sb = new StringBuilder();
            sb.Append(target.Id).Append('|').Append(target.UpdatedUtc.Ticks).Append('|')
                .Append(lengthToMeter.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(tolerances.Volume.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(tolerances.Distance.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(tolerances.Area.ToString("R", CultureInfo.InvariantCulture));
            AppendSolids(sb, targetSolids);
            var dependencySet = new HashSet<string>(dependencyIds, StringComparer.OrdinalIgnoreCase);
            AppendSolids(sb, candidates.Where(x => dependencySet.Contains(x.ElementId)).ToList());
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return string.Concat(bytes.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void AppendSolids(StringBuilder sb, IEnumerable<OwnedSolid> solids)
        {
            foreach (var owned in solids.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).ThenBy(x => string.Join("|", x.Handles), StringComparer.OrdinalIgnoreCase))
            {
                sb.Append('|').Append(owned.ElementId).Append('|').Append(string.Join(",", owned.Handles));
                try
                {
                    var e = owned.Solid.GeometricExtents;
                    sb.Append('|').Append(e.MinPoint.X.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',').Append(e.MinPoint.Y.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',').Append(e.MinPoint.Z.ToString("R", CultureInfo.InvariantCulture))
                        .Append('|').Append(e.MaxPoint.X.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',').Append(e.MaxPoint.Y.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',').Append(e.MaxPoint.Z.ToString("R", CultureInfo.InvariantCulture));
                }
                catch { sb.Append("|extents-unavailable"); }
                sb.Append('|').Append(SafeVolumeCad(owned.Solid).ToString("R", CultureInfo.InvariantCulture));
            }
        }

        private static Solid3d Clone(Solid3d source) => (Solid3d)source.Clone();
        private static bool Recoverable(Exception ex) => !(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException);
        private static void Add(IDictionary<string, double> values, string key, double value) => values[key] = (values.TryGetValue(key, out var current) ? current : 0d) + value;
        private static void Add(IDictionary<int, double> values, int key, double value) => values[key] = (values.TryGetValue(key, out var current) ? current : 0d) + value;

        private static string ElementName(ProjectState project, ProjectElement element)
        {
            if (element.Properties.TryGetValue("Name", out var name) && !string.IsNullOrWhiteSpace(name)) return name.Trim();
            var family = project.Families.FirstOrDefault(x => string.Equals(x.Id, element.FamilyId, StringComparison.OrdinalIgnoreCase));
            return family == null || string.IsNullOrWhiteSpace(family.Name) ? element.Id : family.Name.Trim();
        }
    }
}
