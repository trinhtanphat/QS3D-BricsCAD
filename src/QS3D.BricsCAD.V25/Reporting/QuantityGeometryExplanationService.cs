using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            public Solid3d Solid = null!;
            public void Dispose() => Solid?.Dispose();
        }

        private sealed class FaceSeed
        {
            public string Id = string.Empty;
            public string Type = "Other";
            public double GrossArea;
            public PlanarEntity? Plane;
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
            var targetHandles = SourceHandleResolver.Resolve(project, new[] { targetElement.Id });
            var targetIds = CadHandleService.Resolve(document, targetHandles);
            var targetHandleSet = new HashSet<string>(targetIds.Select(x => x.Handle.ToString()), StringComparer.OrdinalIgnoreCase);

            var targetSolids = CloneSolids(document, targetIds, targetElement.Id, ElementName(project, targetElement), targetHandles);
            if (targetSolids.Count == 0)
                throw new InvalidOperationException("Cấu kiện " + targetElement.Id + " không có Solid3d live để diễn giải hình học.");

            var candidates = new List<OwnedSolid>();
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

                var individualVolume = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var individualArea = new Dictionary<string, Dictionary<int, double>>(StringComparer.OrdinalIgnoreCase);
                var relation = new Dictionary<string, QuantityGeometryRelation>(StringComparer.OrdinalIgnoreCase);
                var faceSeeds = new List<FaceSeed>();
                var grossVolume = 0d;
                var netVolume = 0d;
                var residualForFormwork = new List<Solid3d>();

                for (var targetIndex = 0; targetIndex < targetSolids.Count; targetIndex++)
                {
                    var target = targetSolids[targetIndex].Solid;
                    grossVolume += SafeVolume(target);
                    var seeds = ReadFaces(target, faceSeeds.Count, diagnostics);
                    faceSeeds.AddRange(seeds);

                    using (var volumeResidual = Clone(target))
                    {
                        var formworkResidual = Clone(target);
                        residualForFormwork.Add(formworkResidual);

                        foreach (var candidate in candidates)
                        {
                            if (!BoundingBoxesMayOverlap(target, candidate.Solid, tolerances.Distance)) continue;
                            var intersection = TryIntersection(target, candidate.Solid);
                            var intersectionVolume = intersection == null ? 0d : SafeVolume(intersection);
                            if (intersection != null && intersectionVolume > tolerances.Volume)
                            {
                                Add(individualVolume, candidate.ElementId, intersectionVolume);
                                relation[candidate.ElementId] = QuantityGeometryRelation.VolumeIntersection;
                                AccumulateFaceCoverage(intersection, seeds, individualArea, candidate.ElementId, tolerances, diagnostics);
                                intersection.Dispose();
                                TrySubtract(volumeResidual, candidate.Solid, diagnostics, "volume/" + candidate.ElementId);
                                TrySubtract(formworkResidual, candidate.Solid, diagnostics, "formwork/" + candidate.ElementId);
                                continue;
                            }
                            intersection?.Dispose();

                            using (var contactProbe = Clone(candidate.Solid))
                            {
                                if (!TryOffset(contactProbe, tolerances.Distance)) continue;
                                using (var contact = TryIntersection(target, contactProbe))
                                {
                                    if (contact == null || SafeVolume(contact) <= tolerances.Volume) continue;
                                    var covered = AccumulateFaceCoverage(contact, seeds, individualArea, candidate.ElementId, tolerances, diagnostics);
                                    if (covered <= tolerances.Area) continue;
                                    relation[candidate.ElementId] = QuantityGeometryRelation.FaceContact;
                                    TrySubtract(formworkResidual, contactProbe, diagnostics, "contact/" + candidate.ElementId);
                                }
                            }
                        }
                        netVolume += SafeVolume(volumeResidual);
                    }
                }

                var faces = BuildFaceResults(faceSeeds, residualForFormwork, candidates, individualArea, relation, tolerances, diagnostics);
                var deductions = BuildVolumeDeductions(candidates, individualVolume, relation);
                var deductionVolume = Math.Max(0d, grossVolume - netVolume);
                if (individualVolume.Values.Sum() + tolerances.Volume < deductionVolume)
                    diagnostics.Add("Union deduction exceeded the sum of individual intersections; geometry was retained fail-closed for review.");
                if (individualVolume.Values.Sum() > deductionVolume + tolerances.Volume)
                    diagnostics.Add("Các vùng giao chồng nhau đã được trừ một lần bằng residual boolean union semantics.");

                var result = new QuantityGeometryExplanation
                {
                    ElementId = targetElement.Id,
                    ElementName = ElementName(project, targetElement),
                    SourceHandles = targetHandles.ToList().AsReadOnly(),
                    GrossVolume = grossVolume,
                    DeductionVolume = deductionVolume,
                    NetVolume = Math.Max(0d, netVolume),
                    VolumeDeductions = deductions,
                    FormworkFaces = faces,
                    Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToList().AsReadOnly()
                };
                result.Validate(tolerances);
                return result;
            }
            finally
            {
                foreach (var solid in targetSolids) solid.Dispose();
                foreach (var solid in candidates) solid.Dispose();
            }
        }

        private static List<OwnedSolid> CloneSolids(Document document, IEnumerable<ObjectId> ids, string elementId, string elementName, IReadOnlyList<string> handles)
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
                        Solid = Clone(solid)
                    });
                }
                tr.Commit();
            }
            return result;
        }

        private static IReadOnlyList<QuantityGeometryDeduction> BuildVolumeDeductions(
            IEnumerable<OwnedSolid> candidates,
            IReadOnlyDictionary<string, double> volumes,
            IReadOnlyDictionary<string, QuantityGeometryRelation> relations)
        {
            var byId = candidates.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            return volumes.OrderByDescending(x => x.Value).Select(x =>
            {
                var candidate = byId[x.Key];
                return new QuantityGeometryDeduction
                {
                    ElementId = candidate.ElementId,
                    ElementName = candidate.ElementName,
                    Relation = relations.TryGetValue(candidate.ElementId, out var r) ? r : QuantityGeometryRelation.VolumeIntersection,
                    Volume = x.Value,
                    SourceHandles = candidate.Handles
                };
            }).ToList().AsReadOnly();
        }

        private static IReadOnlyList<QuantityFormworkFaceExplanation> BuildFaceResults(
            IReadOnlyList<FaceSeed> seeds,
            IReadOnlyList<Solid3d> residuals,
            IReadOnlyList<OwnedSolid> candidates,
            IReadOnlyDictionary<string, Dictionary<int, double>> individualArea,
            IReadOnlyDictionary<string, QuantityGeometryRelation> relations,
            QuantityGeometryTolerances tolerances,
            ICollection<string> diagnostics)
        {
            var residualAreas = new double[seeds.Count];
            foreach (var residual in residuals)
            {
                using (var brep = new Brep(residual))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = face.Surface as PlanarEntity;
                        if (plane == null) continue;
                        var area = SafeArea(face);
                        var best = FindMatchingFace(seeds, plane, tolerances.Distance);
                        if (best >= 0) residualAreas[best] += area;
                    }
                }
            }

            var candidateById = candidates.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var result = new List<QuantityFormworkFaceExplanation>();
            for (var index = 0; index < seeds.Count; index++)
            {
                var seed = seeds[index];
                var net = seed.Plane == null ? seed.GrossArea : Math.Min(seed.GrossArea, residualAreas[index]);
                if (seed.Plane == null) diagnostics.Add(seed.Id + ": mặt không phẳng được giữ nguyên diện tích; cần native BREP probe nếu phải khấu trừ mặt cong.");
                var deduction = Math.Max(0d, seed.GrossArea - net);
                var rows = new List<QuantityGeometryDeduction>();
                foreach (var byElement in individualArea)
                {
                    if (!byElement.Value.TryGetValue(index, out var area) || area <= tolerances.Area) continue;
                    if (!candidateById.TryGetValue(byElement.Key, out var candidate)) continue;
                    rows.Add(new QuantityGeometryDeduction
                    {
                        ElementId = candidate.ElementId,
                        ElementName = candidate.ElementName,
                        Relation = relations.TryGetValue(candidate.ElementId, out var r) ? r : QuantityGeometryRelation.FaceOverlap,
                        Area = Math.Min(seed.GrossArea, area),
                        SourceHandles = candidate.Handles
                    });
                }
                result.Add(new QuantityFormworkFaceExplanation
                {
                    FaceId = seed.Id,
                    FaceType = seed.Type,
                    GrossArea = seed.GrossArea,
                    DeductionArea = deduction,
                    NetArea = Math.Max(0d, seed.GrossArea - deduction),
                    Deductions = rows.OrderByDescending(x => x.Area).ToList().AsReadOnly()
                });
            }
            return result.AsReadOnly();
        }

        private static List<FaceSeed> ReadFaces(Solid3d solid, int startIndex, ICollection<string> diagnostics)
        {
            var result = new List<FaceSeed>();
            try
            {
                using (var brep = new Brep(solid))
                {
                    var index = startIndex;
                    foreach (BrepFace face in brep.Faces)
                    {
                        index++;
                        var plane = face.Surface as PlanarEntity;
                        result.Add(new FaceSeed
                        {
                            Id = "FACE-" + index.ToString("00", CultureInfo.InvariantCulture),
                            Type = FaceType(plane),
                            GrossArea = SafeArea(face),
                            Plane = plane == null ? null : new Plane(plane.PointOnPlane, plane.Normal)
                        });
                    }
                }
            }
            catch (Exception ex) when (Recoverable(ex)) { diagnostics.Add("BREP face read: " + ex.Message); }
            return result;
        }

        private static double AccumulateFaceCoverage(
            Solid3d intersection,
            IReadOnlyList<FaceSeed> seeds,
            IDictionary<string, Dictionary<int, double>> accumulator,
            string elementId,
            QuantityGeometryTolerances tolerances,
            ICollection<string> diagnostics)
        {
            var total = 0d;
            try
            {
                using (var brep = new Brep(intersection))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        var plane = face.Surface as PlanarEntity;
                        if (plane == null) continue;
                        var seedIndex = FindMatchingFace(seeds, plane, tolerances.Distance);
                        if (seedIndex < 0) continue;
                        var area = SafeArea(face);
                        if (area <= tolerances.Area) continue;
                        if (!accumulator.TryGetValue(elementId, out var byFace)) accumulator[elementId] = byFace = new Dictionary<int, double>();
                        Add(byFace, seedIndex, area);
                        total += area;
                    }
                }
            }
            catch (Exception ex) when (Recoverable(ex)) { diagnostics.Add("BREP coverage/" + elementId + ": " + ex.Message); }
            return total;
        }

        private static int FindMatchingFace(IReadOnlyList<FaceSeed> seeds, PlanarEntity plane, double tolerance)
        {
            for (var i = 0; i < seeds.Count; i++)
            {
                var target = seeds[i].Plane;
                if (target == null) continue;
                if (SamePlane(target, plane, tolerance)) return i;
            }
            return -1;
        }

        private static bool SamePlane(PlanarEntity left, PlanarEntity right, double tolerance)
        {
            var ln = left.Normal.GetNormal();
            var rn = right.Normal.GetNormal();
            if (Math.Abs(ln.DotProduct(rn)) < 1d - 1e-7) return false;
            return Math.Abs((right.PointOnPlane - left.PointOnPlane).DotProduct(ln)) <= tolerance;
        }

        private static string FaceType(PlanarEntity? plane)
        {
            if (plane == null) return "Other";
            var z = plane.Normal.GetNormal().Z;
            if (z <= -0.70710678118d) return "Bottom";
            if (z >= 0.70710678118d) return "Top";
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

        private static bool TryOffset(Solid3d solid, double distance)
        {
            try { solid.OffsetBody(distance); return true; }
            catch (Exception ex) when (Recoverable(ex)) { return false; }
        }

        private static bool BoundingBoxesMayOverlap(Solid3d left, Solid3d right, double tolerance)
        {
            try
            {
                var a = left.GeometricExtents;
                var b = right.GeometricExtents;
                return a.MinPoint.X <= b.MaxPoint.X + tolerance && a.MaxPoint.X + tolerance >= b.MinPoint.X &&
                       a.MinPoint.Y <= b.MaxPoint.Y + tolerance && a.MaxPoint.Y + tolerance >= b.MinPoint.Y &&
                       a.MinPoint.Z <= b.MaxPoint.Z + tolerance && a.MaxPoint.Z + tolerance >= b.MinPoint.Z;
            }
            catch { return true; }
        }

        private static double SafeVolume(Solid3d solid)
        {
            try { var value = solid.Volume; return double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value; }
            catch { return 0d; }
        }

        private static double SafeArea(BrepFace face)
        {
            try { var value = face.GetArea(); return double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value; }
            catch { return 0d; }
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
