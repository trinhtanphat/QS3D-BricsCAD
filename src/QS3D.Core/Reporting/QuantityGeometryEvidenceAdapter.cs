using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Reporting
{
    /// <summary>
    /// Canonical evidence bundle projected from an already-computed exact
    /// geometry explanation. This adapter never evaluates CAD/BREP geometry;
    /// it only copies the reviewed values and selectors into QuantityExplanation.
    /// </summary>
    public sealed class QuantityGeometryEvidenceBundle
    {
        internal QuantityGeometryEvidenceBundle(
            QuantityExplanation concrete,
            QuantityExplanation formwork)
        {
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Formwork = formwork ?? throw new ArgumentNullException(nameof(formwork));
            Explanations = new[] { Concrete, Formwork };
        }

        public QuantityExplanation Concrete { get; }
        public QuantityExplanation Formwork { get; }
        public IReadOnlyList<QuantityExplanation> Explanations { get; }
    }

    public static class QuantityGeometryEvidenceAdapter
    {
        private const int DecimalPlaces = 9;

        public static QuantityGeometryEvidenceBundle Create(QuantityGeometryExplanation geometry)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            geometry.Validate(new QuantityGeometryTolerances());

            var elementId = RequireKey(geometry.ElementId, nameof(geometry.ElementId));
            var category = "ExactBREP";
            var fingerprint = RequireKey(geometry.GeometryFingerprint, nameof(geometry.GeometryFingerprint));

            var concrete = BuildConcrete(geometry, elementId, category, fingerprint);
            var formwork = BuildFormwork(geometry, elementId, category, fingerprint);
            return new QuantityGeometryEvidenceBundle(concrete, formwork);
        }

        private static QuantityExplanation BuildConcrete(
            QuantityGeometryExplanation geometry,
            string elementId,
            string category,
            string fingerprint)
        {
            var gross = Quantize(geometry.GrossVolume, nameof(geometry.GrossVolume));
            var net = Quantize(geometry.NetVolume, nameof(geometry.NetVolume));
            if (net > gross)
                throw new InvalidOperationException("Exact BREP net concrete volume cannot exceed gross volume.");

            var contributions = new List<QuantityContribution>
            {
                QuantityContribution.Create(
                    "concrete.gross.brep",
                    "Bê tông nguyên bản",
                    QuantityEvidenceOperation.Add,
                    "BREP exact gross volume",
                    gross,
                    QuantityEvidenceSelector.ForEntity(elementId),
                    new[] { new QuantityEvidenceOperand("geometry-fingerprint", 0m, fingerprint) })
            };

            foreach (var deduction in OrderedVolumeDeductions(geometry.VolumeDeductions))
            {
                var causeId = RequireKey(deduction.ElementId, "VolumeDeductions.ElementId");
                var regionKey = RequireKey(deduction.RegionKey, "VolumeDeductions.RegionKey");
                var value = Quantize(deduction.Volume, regionKey + "/Volume");
                if (value <= 0m) continue;

                contributions.Add(QuantityContribution.Create(
                    "concrete.deduction.cause." + regionKey,
                    "Khấu trừ giao " + DisplayCause(deduction),
                    QuantityEvidenceOperation.Deduct,
                    deduction.Relation.ToString(),
                    -value,
                    QuantityEvidenceSelector.ForIntersection(elementId, causeId, regionKey)));
            }

            var adjustments = BuildAggregateAdjustment(
                elementId,
                fingerprint,
                "concrete.union.deduction",
                "BREP residual boolean union deduction",
                gross,
                net,
                "volume");

            return QuantityExplanation.Create(
                elementId,
                category,
                "ConcreteVolume",
                "m3",
                gross,
                net,
                contributions,
                adjustments);
        }

        private static QuantityExplanation BuildFormwork(
            QuantityGeometryExplanation geometry,
            string elementId,
            string category,
            string fingerprint)
        {
            var gross = Quantize(geometry.GrossFormworkArea, nameof(geometry.GrossFormworkArea));
            var net = Quantize(geometry.NetFormworkArea, nameof(geometry.NetFormworkArea));
            if (net > gross)
                throw new InvalidOperationException("Exact BREP net formwork area cannot exceed gross area.");

            var contributions = new List<QuantityContribution>();
            foreach (var face in OrderedFaces(geometry.FormworkFaces))
            {
                var faceId = RequireKey(face.FaceId, "FormworkFaces.FaceId");
                var faceGross = Quantize(face.GrossArea, faceId + "/GrossArea");
                if (faceGross > 0m)
                {
                    contributions.Add(QuantityContribution.Create(
                        "formwork.face." + faceId,
                        faceId + " • " + NormalizeLabel(face.FaceType, "Other"),
                        QuantityEvidenceOperation.Add,
                        "BREP exact face gross area",
                        faceGross,
                        QuantityEvidenceSelector.ForFaceKey(elementId, faceId)));
                }

                foreach (var deduction in OrderedFaceDeductions(face.Deductions))
                {
                    var causeId = RequireKey(deduction.ElementId, faceId + "/Deduction.ElementId");
                    var regionKey = RequireKey(deduction.RegionKey, faceId + "/Deduction.RegionKey");
                    var value = Quantize(deduction.Area, regionKey + "/Area");
                    if (value <= 0m) continue;

                    contributions.Add(QuantityContribution.Create(
                        "formwork.deduction.cause." + regionKey,
                        "Khấu trừ " + faceId + " bởi " + DisplayCause(deduction),
                        QuantityEvidenceOperation.Deduct,
                        deduction.Relation.ToString(),
                        -value,
                        QuantityEvidenceSelector.ForIntersection(elementId, causeId, regionKey)));
                }
            }

            var adjustments = BuildAggregateAdjustment(
                elementId,
                fingerprint,
                "formwork.union.deduction",
                "BREP residual/contact union deduction",
                gross,
                net,
                "formwork");

            return QuantityExplanation.Create(
                elementId,
                category,
                "FormworkArea",
                "m2",
                gross,
                net,
                contributions,
                adjustments);
        }

        private static IReadOnlyList<QuantityAdjustment> BuildAggregateAdjustment(
            string elementId,
            string fingerprint,
            string semanticKey,
            string reason,
            decimal gross,
            decimal net,
            string suffix)
        {
            var delta = net - gross;
            if (delta == 0m) return Array.Empty<QuantityAdjustment>();

            // Individual BREP intersections can overlap. The geometry engine
            // subtracts their union exactly once, so arithmetic uses one explicit
            // union node while per-cause rows above retain the real source/target
            // intersection selectors for review/locate/export provenance.
            var unionNode = "@brep-union:" + fingerprint;
            var selector = QuantityEvidenceSelector.ForIntersection(
                elementId,
                unionNode,
                "union:" + suffix + ":" + fingerprint);
            return new[]
            {
                QuantityAdjustment.Create(
                    semanticKey,
                    "brep-union-residual-v1",
                    reason,
                    QuantityEvidenceOperation.Deduct,
                    elementId,
                    unionNode,
                    delta,
                    selector)
            };
        }

        private static IEnumerable<QuantityGeometryDeduction> OrderedVolumeDeductions(
            IReadOnlyList<QuantityGeometryDeduction>? deductions)
        {
            return (deductions ?? Array.Empty<QuantityGeometryDeduction>())
                .Where(x => x != null)
                .OrderBy(x => x.RegionKey ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => x.ElementId ?? string.Empty, StringComparer.Ordinal);
        }

        private static IEnumerable<QuantityFormworkFaceExplanation> OrderedFaces(
            IReadOnlyList<QuantityFormworkFaceExplanation>? faces)
        {
            return (faces ?? Array.Empty<QuantityFormworkFaceExplanation>())
                .Where(x => x != null)
                .OrderBy(x => x.FaceId ?? string.Empty, StringComparer.Ordinal);
        }

        private static IEnumerable<QuantityGeometryDeduction> OrderedFaceDeductions(
            IReadOnlyList<QuantityGeometryDeduction>? deductions)
        {
            return (deductions ?? Array.Empty<QuantityGeometryDeduction>())
                .Where(x => x != null)
                .OrderBy(x => x.RegionKey ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => x.ElementId ?? string.Empty, StringComparer.Ordinal);
        }

        private static decimal Quantize(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " must be a finite non-negative value.");
            try
            {
                return decimal.Round(Convert.ToDecimal(value), DecimalPlaces, MidpointRounding.AwayFromZero);
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException(label + " cannot be represented by the canonical decimal evidence contract.", ex);
            }
        }

        private static string RequireKey(string? value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidOperationException(label + " is required for quantity evidence.");
            return normalized;
        }

        private static string DisplayCause(QuantityGeometryDeduction deduction)
        {
            return NormalizeLabel(deduction.ElementName, deduction.ElementId);
        }

        private static string NormalizeLabel(string? value, string fallback)
        {
            var normalized = (value ?? string.Empty).Trim();
            return normalized.Length == 0 ? fallback : normalized;
        }
    }
}
