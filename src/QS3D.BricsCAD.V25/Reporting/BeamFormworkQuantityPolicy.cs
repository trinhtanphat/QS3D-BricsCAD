using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using BrepFace = Teigha.BoundaryRepresentation.Face;

namespace QS3D.BricsCAD.V25.Reporting
{
    /// <summary>
    /// Canonical Beam formwork policy for Quantity Insight.
    ///
    /// The generic geometry explainer intentionally discovers every BREP face. This
    /// adapter performs the Beam-specific rule projection afterwards so the same
    /// exact face ledger is used by Detail and by the aggregate preview:
    /// Top/End are never quantity candidates, Side follows ExtractSide, Bottom
    /// follows ExtractBottom, and directed contact deductions follow persisted rules.
    ///
    /// Native plane evidence is copied while the source Solid3d is open ForRead.
    /// Horizontal classification uses the face Z position against the live solid
    /// bounds; it never trusts the sign of the ACIS/BREP face normal.
    /// </summary>
    internal static class BeamFormworkQuantityPolicy
    {
        private const double HorizontalNormalThreshold = 0.70710678118d;

        private sealed class LiveFaceEvidence
        {
            public string FaceId = string.Empty;
            public int SolidNumber;
            public double Nx;
            public double Ny;
            public double Nz;
            public double Z;
            public double MinZ;
            public double MaxZ;
        }

        public static QuantityGeometryExplanation Apply(
            Document document,
            ProjectState project,
            string elementId,
            QuantityGeometryExplanation geometry,
            QuantityCalculationRuleSet rules)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var element = project.FindElement((elementId ?? string.Empty).Trim())
                ?? throw new InvalidOperationException("Beam formwork element no longer exists: " + elementId + ".");
            if (element.Category != ElementCategory.Beam) return geometry;

            var diagnostics = new List<string>(geometry.Diagnostics ?? Array.Empty<string>());
            if (!rules.TryGetCategoryRule(ElementCategory.Beam, out var categoryRule))
            {
                diagnostics.Add("Beam formwork rule missing: all Beam formwork faces were excluded fail-closed.");
                geometry.FormworkFaces = Array.Empty<QuantityFormworkFaceExplanation>();
                geometry.Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToList().AsReadOnly();
                geometry.Validate(new QuantityGeometryTolerances());
                return geometry;
            }

            var kinds = ReadLiveFaceKinds(document, geometry, diagnostics);
            var gate = new QuantityCalculationDeductionGate(rules);
            var kept = new List<QuantityFormworkFaceExplanation>();
            foreach (var face in geometry.FormworkFaces ?? Array.Empty<QuantityFormworkFaceExplanation>())
            {
                if (!kinds.TryGetValue(face.FaceId ?? string.Empty, out var kind))
                {
                    diagnostics.Add((face.FaceId ?? "<face>") + ": Beam native face classification unavailable; excluded fail-closed.");
                    continue;
                }

                var isSide = string.Equals(kind, "Side", StringComparison.Ordinal);
                var isBottom = string.Equals(kind, "Bottom", StringComparison.Ordinal);
                if (!isSide && !isBottom) continue;

                var enabled = isSide ? categoryRule.ExtractSide : categoryRule.ExtractBottom;
                if (!enabled) continue;
                var grossMm2 = ToMm2(face.GrossArea, face.FaceId + "/gross");
                if (!gate.AllowsFormworkArea(grossMm2)) continue;

                var originalPositive = (face.Deductions ?? Array.Empty<QuantityGeometryDeduction>())
                    .Where(x => x != null && x.Area > 0d)
                    .ToList();
                var allowedRows = new List<QuantityGeometryDeduction>();
                foreach (var deduction in originalPositive)
                {
                    var cause = project.FindElement((deduction.ElementId ?? string.Empty).Trim());
                    if (cause == null)
                    {
                        diagnostics.Add(face.FaceId + ": deduction cause " + deduction.ElementId + " is no longer semantic; skipped fail-closed.");
                        continue;
                    }

                    var areaMm2 = ToMm2(deduction.Area, face.FaceId + "/deduction");
                    bool found;
                    bool allowed;
                    if (isSide)
                    {
                        found = deduction.Relation == QuantityGeometryRelation.FaceOverlap
                            ? gate.TryAllowSideFormworkBySideFormworkDeduction(ElementCategory.Beam, cause.Category, areaMm2, out allowed)
                            : gate.TryAllowSideFormworkByConcreteDeduction(ElementCategory.Beam, cause.Category, areaMm2, out allowed);
                    }
                    else
                    {
                        found = deduction.Relation == QuantityGeometryRelation.FaceOverlap
                            ? gate.TryAllowBottomFormworkByBottomFormworkDeduction(ElementCategory.Beam, cause.Category, areaMm2, out allowed)
                            : gate.TryAllowBottomFormworkByConcreteDeduction(ElementCategory.Beam, cause.Category, areaMm2, out allowed);
                    }

                    if (!found)
                    {
                        diagnostics.Add(face.FaceId + ": directed deduction rule Beam->" + cause.Category + " missing; deduction skipped fail-closed.");
                        continue;
                    }
                    if (allowed) allowedRows.Add(deduction);
                }

                double deductionArea;
                if (allowedRows.Count == 0)
                {
                    deductionArea = 0d;
                }
                else if (allowedRows.Count == originalPositive.Count)
                {
                    deductionArea = Math.Min(face.GrossArea, Math.Max(0d, face.DeductionArea));
                }
                else
                {
                    deductionArea = Math.Min(face.GrossArea, allowedRows.Sum(x => x.Area));
                    diagnostics.Add(face.FaceId + ": Beam deductions were rule-filtered; net uses enabled face-clipped regions only.");
                }

                kept.Add(new QuantityFormworkFaceExplanation
                {
                    FaceId = face.FaceId,
                    SemanticKey = face.SemanticKey ?? string.Empty,
                    FaceType = kind,
                    GrossArea = face.GrossArea,
                    DeductionArea = deductionArea,
                    NetArea = Math.Max(0d, face.GrossArea - deductionArea),
                    MeasurementKind = face.MeasurementKind,
                    MeasurementLength = face.MeasurementLength,
                    MeasurementHeight = face.MeasurementHeight,
                    Deductions = allowedRows.AsReadOnly()
                });
            }

            diagnostics.Add("Beam formwork policy applied: Top/End excluded; Side=" + categoryRule.ExtractSide + "; Bottom=" + categoryRule.ExtractBottom + ".");
            geometry.FormworkFaces = kept.AsReadOnly();
            geometry.Diagnostics = diagnostics.Distinct(StringComparer.Ordinal).ToList().AsReadOnly();
            geometry.Validate(new QuantityGeometryTolerances());
            return geometry;
        }

        public static void ApplyAnalyticFallback(ProjectElement element, QuantityCalculationRuleSet rules)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (element.Category != ElementCategory.Beam) return;

            if (!rules.TryGetCategoryRule(ElementCategory.Beam, out var rule))
            {
                element.SetQuantity("FormworkM2", 0d);
                return;
            }

            var length = Q(element, "LengthM");
            var height = Q(element, "HeightM");
            var crossSection = Q(element, "CrossSectionAreaM2");
            var width = height > 0d ? crossSection / height : 0d;
            var side = rule.ExtractSide ? 2d * height * length : 0d;
            var bottom = rule.ExtractBottom ? width * length : 0d;
            element.SetQuantity("SideAreaM2", side);
            element.SetQuantity("BottomAreaM2", bottom);
            element.SetQuantity("TopAreaM2", 0d);
            element.SetQuantity("FormworkM2", side + bottom);
        }

        public static void ApplyExactQuantity(ProjectElement element, QuantityGeometryExplanation geometry)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (element.Category != ElementCategory.Beam) return;

            var side = geometry.FormworkFaces
                .Where(x => string.Equals(x.FaceType, "Side", StringComparison.Ordinal))
                .Sum(x => x.NetArea);
            var bottom = geometry.FormworkFaces
                .Where(x => string.Equals(x.FaceType, "Bottom", StringComparison.Ordinal))
                .Sum(x => x.NetArea);
            element.SetQuantity("SideAreaM2", side);
            element.SetQuantity("BottomAreaM2", bottom);
            element.SetQuantity("TopAreaM2", 0d);
            element.SetQuantity("GrossFormworkM2", geometry.GrossFormworkArea);
            element.SetQuantity("ConcreteContactDeductionM2", geometry.DeductionFormworkArea);
            element.SetQuantity("FormworkM2", geometry.NetFormworkArea);
        }

        private static IReadOnlyDictionary<string, string> ReadLiveFaceKinds(
            Document document,
            QuantityGeometryExplanation geometry,
            ICollection<string> diagnostics)
        {
            var evidence = new List<LiveFaceEvidence>();
            var ids = CadHandleService.Resolve(document, geometry.SourceHandles ?? Array.Empty<string>());
            try
            {
                using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var solidNumber = 0;
                    foreach (var id in ids)
                    {
                        var solid = tr.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                        if (solid == null || solid.IsErased) continue;
                        solidNumber++;
                        var ext = solid.GeometricExtents;
                        var rootPath = new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null);
                        using (var brep = new Brep(rootPath))
                        {
                            var faceNumber = 0;
                            foreach (BrepFace face in brep.Faces)
                            {
                                faceNumber++;
                                var plane = ReadFacePlane(face);
                                if (plane == null) continue;
                                var normal = plane.Normal.GetNormal();
                                evidence.Add(new LiveFaceEvidence
                                {
                                    FaceId = "SOLID-" + solidNumber.ToString("00", CultureInfo.InvariantCulture) + "/FACE-" + faceNumber.ToString("00", CultureInfo.InvariantCulture),
                                    SolidNumber = solidNumber,
                                    Nx = normal.X,
                                    Ny = normal.Y,
                                    Nz = normal.Z,
                                    Z = plane.PointOnPlane.Z,
                                    MinZ = ext.MinPoint.Z,
                                    MaxZ = ext.MaxPoint.Z
                                });
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add("Beam live BREP face evidence unavailable: " + ex.Message);
            }

            var grossByFace = (geometry.FormworkFaces ?? Array.Empty<QuantityFormworkFaceExplanation>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.FaceId))
                .GroupBy(x => x.FaceId, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First().GrossArea, StringComparer.Ordinal);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var solidGroup in evidence.GroupBy(x => x.SolidNumber))
            {
                var vertical = solidGroup.Where(x => Math.Abs(x.Nz) < HorizontalNormalThreshold).ToList();
                LiveFaceEvidence? majorSide = null;
                var majorArea = double.NegativeInfinity;
                foreach (var face in vertical)
                {
                    if (!grossByFace.TryGetValue(face.FaceId, out var area)) continue;
                    if (area <= majorArea) continue;
                    majorArea = area;
                    majorSide = face;
                }

                var axisX = 0d;
                var axisY = 0d;
                if (majorSide != null)
                {
                    axisX = -majorSide.Ny;
                    axisY = majorSide.Nx;
                    var axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);
                    if (axisLength > 0d)
                    {
                        axisX /= axisLength;
                        axisY /= axisLength;
                    }
                }

                foreach (var face in solidGroup)
                {
                    if (Math.Abs(face.Nz) >= HorizontalNormalThreshold)
                    {
                        var span = Math.Abs(face.MaxZ - face.MinZ);
                        var tolerance = Math.Max(span * 1e-8d, 1e-9d);
                        if (Math.Abs(face.Z - face.MinZ) <= tolerance) result[face.FaceId] = "Bottom";
                        else if (Math.Abs(face.Z - face.MaxZ) <= tolerance) result[face.FaceId] = "Top";
                        else result[face.FaceId] = "Other";
                        continue;
                    }

                    if (majorSide == null)
                    {
                        result[face.FaceId] = "Other";
                        continue;
                    }
                    var horizontalNormalLength = Math.Sqrt(face.Nx * face.Nx + face.Ny * face.Ny);
                    if (horizontalNormalLength <= 0d)
                    {
                        result[face.FaceId] = "Other";
                        continue;
                    }
                    var dot = Math.Abs((face.Nx / horizontalNormalLength) * axisX + (face.Ny / horizontalNormalLength) * axisY);
                    result[face.FaceId] = dot >= HorizontalNormalThreshold ? "End" : "Side";
                }
            }
            return result;
        }

        private static PlanarEntity? ReadFacePlane(BrepFace face)
        {
            var surface = face.Surface;
            if (surface is PlanarEntity planar)
                return new Plane(planar.PointOnPlane, planar.Normal);
            if (surface is ExternalBoundedSurface external && external.IsPlane && external.BaseSurface is PlanarEntity basePlane)
                return new Plane(basePlane.PointOnPlane, basePlane.Normal);
            return null;
        }

        private static double ToMm2(double areaM2, string label)
        {
            if (double.IsNaN(areaM2) || double.IsInfinity(areaM2) || areaM2 < 0d)
                throw new InvalidOperationException(label + " must be a finite non-negative area.");
            var value = areaM2 * 1000000d;
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " cannot be represented in mm2.");
            return value;
        }

        private static double Q(ProjectElement element, string key)
        {
            return element.Quantities.TryGetValue(key, out var value) && value > 0d ? value : 0d;
        }
    }
}
