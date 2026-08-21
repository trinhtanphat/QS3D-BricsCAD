using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Reporting
{
    /// <summary>
    /// Host-resolved formwork face kind. Core deliberately does not infer this
    /// classification from CAD normals because that would make shared quantity
    /// rules depend on unavailable host geometry.
    /// </summary>
    public enum QuantityFormworkFaceKind
    {
        Side = 1,
        Bottom = 2
    }

    /// <summary>
    /// Identifies which directed persisted flag controls a measured contact area.
    /// </summary>
    public enum QuantityFormworkDeductionBasis
    {
        Concrete = 1,
        Formwork = 2
    }

    /// <summary>
    /// One already-measured, union-resolved contact region for a formwork face.
    /// Area is expressed in mm2 to match QuantityCalculationSettings.
    /// </summary>
    public sealed class QuantityFormworkDeductionCandidate
    {
        public QuantityFormworkDeductionCandidate(
            int targetCode,
            QuantityFormworkDeductionBasis basis,
            double areaMm2,
            string regionKey = "")
        {
            if (targetCode < 0) throw new ArgumentOutOfRangeException(nameof(targetCode));
            if (!Enum.IsDefined(typeof(QuantityFormworkDeductionBasis), basis))
                throw new ArgumentOutOfRangeException(nameof(basis));
            RequireMeasurement(areaMm2, nameof(areaMm2));

            TargetCode = targetCode;
            Basis = basis;
            AreaMm2 = areaMm2;
            RegionKey = (regionKey ?? string.Empty).Trim();
        }

        public int TargetCode { get; }
        public QuantityFormworkDeductionBasis Basis { get; }
        public double AreaMm2 { get; }
        public string RegionKey { get; }

        private static void RequireMeasurement(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Formwork deduction area must be a finite non-negative number.");
        }
    }

    /// <summary>
    /// A formwork face already discovered/measured by the host adapter.
    /// Deductions must be non-overlapping union-resolved regions for this face.
    /// </summary>
    public sealed class QuantityFormworkFaceCandidate
    {
        public QuantityFormworkFaceCandidate(
            string faceId,
            int sourceCode,
            QuantityFormworkFaceKind kind,
            double grossAreaMm2,
            IEnumerable<QuantityFormworkDeductionCandidate>? deductions = null)
        {
            FaceId = NormalizeRequired(faceId, nameof(faceId));
            if (sourceCode < 0) throw new ArgumentOutOfRangeException(nameof(sourceCode));
            if (!Enum.IsDefined(typeof(QuantityFormworkFaceKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            RequireMeasurement(grossAreaMm2, nameof(grossAreaMm2));

            SourceCode = sourceCode;
            Kind = kind;
            GrossAreaMm2 = grossAreaMm2;
            Deductions = SnapshotDeductions(deductions);
        }

        public string FaceId { get; }
        public int SourceCode { get; }
        public QuantityFormworkFaceKind Kind { get; }
        public double GrossAreaMm2 { get; }
        public IReadOnlyList<QuantityFormworkDeductionCandidate> Deductions { get; }

        private static IReadOnlyList<QuantityFormworkDeductionCandidate> SnapshotDeductions(
            IEnumerable<QuantityFormworkDeductionCandidate>? deductions)
        {
            if (deductions == null) return Array.Empty<QuantityFormworkDeductionCandidate>();
            var result = new List<QuantityFormworkDeductionCandidate>();
            var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var deduction in deductions)
            {
                if (deduction == null)
                    throw new ArgumentException("Formwork deductions cannot contain null entries.", nameof(deductions));
                if (deduction.RegionKey.Length != 0 && !regions.Add(deduction.RegionKey))
                    throw new ArgumentException("Formwork deduction region keys must be unique per face: " + deduction.RegionKey + ".", nameof(deductions));
                result.Add(deduction);
            }
            return result.AsReadOnly();
        }

        private static string NormalizeRequired(string value, string name)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Formwork face id must not be blank.", name);
            return normalized;
        }

        private static void RequireMeasurement(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Formwork face area must be a finite non-negative number.");
        }
    }

    public sealed class QuantityFormworkRuleTrace
    {
        internal QuantityFormworkRuleTrace(
            string faceId,
            string code,
            string message,
            bool applied,
            double candidateAreaMm2,
            int? targetCode = null,
            QuantityFormworkDeductionBasis? basis = null)
        {
            FaceId = faceId;
            Code = code;
            Message = message;
            Applied = applied;
            CandidateAreaMm2 = candidateAreaMm2;
            TargetCode = targetCode;
            Basis = basis;
        }

        public string FaceId { get; }
        public string Code { get; }
        public string Message { get; }
        public bool Applied { get; }
        public double CandidateAreaMm2 { get; }
        public int? TargetCode { get; }
        public QuantityFormworkDeductionBasis? Basis { get; }
    }

    public sealed class QuantityFormworkFaceResult
    {
        internal QuantityFormworkFaceResult(
            string faceId,
            int sourceCode,
            QuantityFormworkFaceKind kind,
            bool included,
            double measuredAreaMm2,
            double grossAreaMm2,
            double deductionAreaMm2,
            double faceAngleThresholdDeg,
            IReadOnlyList<QuantityFormworkRuleTrace> trace)
        {
            FaceId = faceId;
            SourceCode = sourceCode;
            Kind = kind;
            Included = included;
            MeasuredAreaMm2 = measuredAreaMm2;
            GrossAreaMm2 = grossAreaMm2;
            DeductionAreaMm2 = deductionAreaMm2;
            NetAreaMm2 = Math.Max(0d, grossAreaMm2 - deductionAreaMm2);
            FaceAngleThresholdDeg = faceAngleThresholdDeg;
            Trace = trace;
        }

        public string FaceId { get; }
        public int SourceCode { get; }
        public QuantityFormworkFaceKind Kind { get; }
        public bool Included { get; }
        public double MeasuredAreaMm2 { get; }
        public double GrossAreaMm2 { get; }
        public double DeductionAreaMm2 { get; }
        public double NetAreaMm2 { get; }
        public double GrossAreaM2 => GrossAreaMm2 / 1000000d;
        public double DeductionAreaM2 => DeductionAreaMm2 / 1000000d;
        public double NetAreaM2 => NetAreaMm2 / 1000000d;
        public double FaceAngleThresholdDeg { get; }
        public IReadOnlyList<QuantityFormworkRuleTrace> Trace { get; }
    }

    public sealed class QuantityFormworkCalculationResult
    {
        internal QuantityFormworkCalculationResult(
            IReadOnlyList<QuantityFormworkFaceResult> faces,
            QuantityGeometryExplanation explanation)
        {
            Faces = faces ?? throw new ArgumentNullException(nameof(faces));
            GeometryExplanation = explanation ?? throw new ArgumentNullException(nameof(explanation));

            var gross = 0d;
            var deduction = 0d;
            var net = 0d;
            foreach (var face in faces)
            {
                gross = AddFinite(gross, face.GrossAreaMm2, nameof(GrossAreaMm2));
                deduction = AddFinite(deduction, face.DeductionAreaMm2, nameof(DeductionAreaMm2));
                net = AddFinite(net, face.NetAreaMm2, nameof(NetAreaMm2));
            }

            GrossAreaMm2 = gross;
            DeductionAreaMm2 = deduction;
            NetAreaMm2 = net;
        }

        public IReadOnlyList<QuantityFormworkFaceResult> Faces { get; }
        public double GrossAreaMm2 { get; }
        public double DeductionAreaMm2 { get; }
        public double NetAreaMm2 { get; }
        public double GrossAreaM2 => GrossAreaMm2 / 1000000d;
        public double DeductionAreaM2 => DeductionAreaMm2 / 1000000d;
        public double NetAreaM2 => NetAreaMm2 / 1000000d;
        public double FormworkM2 => NetAreaM2;
        public QuantityGeometryExplanation GeometryExplanation { get; }

        private static double AddFinite(double left, double right, string label)
        {
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException("Formwork " + label + " total is not finite.");
            return result;
        }
    }

    /// <summary>
    /// Applies persisted QS3D/BLT-compatible quantity rules to host-measured
    /// formwork candidates. This type is intentionally geometry-agnostic.
    /// </summary>
    public sealed class QuantityFormworkCalculationEngine
    {
        private readonly QuantityCalculationRuleSet _rules;
        private readonly QuantityCalculationDeductionGate _gate;
        private readonly QuantityCalculationSettings _settings;

        public QuantityFormworkCalculationEngine(QuantityCalculationRuleSet rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _gate = new QuantityCalculationDeductionGate(rules);
            _settings = rules.Snapshot;
            _settings.NormalizeAndValidate();
        }

        public QuantityFormworkCalculationResult Calculate(
            string elementId,
            string elementName,
            IEnumerable<QuantityFormworkFaceCandidate> faceCandidates,
            IEnumerable<string>? sourceHandles = null,
            string geometryFingerprint = "")
        {
            var normalizedElementId = NormalizeRequired(elementId, nameof(elementId));
            var normalizedElementName = (elementName ?? string.Empty).Trim();
            if (faceCandidates == null) throw new ArgumentNullException(nameof(faceCandidates));

            var faces = SnapshotFaces(faceCandidates);
            var results = new List<QuantityFormworkFaceResult>(faces.Count);
            var explanations = new List<QuantityFormworkFaceExplanation>(faces.Count);
            var diagnostics = new List<string>();

            foreach (var face in faces)
            {
                var trace = new List<QuantityFormworkRuleTrace>();
                if (!_rules.TryGetCategoryRule(face.SourceCode, out var categoryRule))
                {
                    AddTrace(trace, diagnostics, face, "FW-CATEGORY-RULE-MISSING",
                        "No exact category extraction rule exists; face is excluded.", false, face.GrossAreaMm2);
                    results.Add(Excluded(face, 0d, trace));
                    explanations.Add(ExplainExcluded(face));
                    continue;
                }

                var enabled = face.Kind == QuantityFormworkFaceKind.Side
                    ? categoryRule.ExtractSide
                    : categoryRule.ExtractBottom;
                if (!enabled)
                {
                    AddTrace(trace, diagnostics, face, "FW-FACE-DISABLED",
                        "Persisted category extraction flag excludes this face kind.", false, face.GrossAreaMm2);
                    results.Add(Excluded(face, categoryRule.FaceAngleThresholdDeg, trace));
                    explanations.Add(ExplainExcluded(face));
                    continue;
                }

                if (!_gate.AllowsFormworkArea(face.GrossAreaMm2))
                {
                    AddTrace(trace, diagnostics, face, "FW-BELOW-MIN-AREA",
                        "Measured face area is below MinFormworkAreaMm2=" + _settings.MinFormworkAreaMm2 + ".",
                        false, face.GrossAreaMm2);
                    results.Add(Excluded(face, categoryRule.FaceAngleThresholdDeg, trace));
                    explanations.Add(ExplainExcluded(face));
                    continue;
                }

                AddTrace(trace, diagnostics, face, "FW-FACE-INCLUDED",
                    "Face passed category extraction and minimum-area gates.", true, face.GrossAreaMm2);

                var deductionAreaMm2 = 0d;
                var appliedDeductions = new List<QuantityGeometryDeduction>();
                foreach (var deduction in face.Deductions)
                {
                    if (deduction.AreaMm2 == 0d)
                    {
                        AddDeductionTrace(trace, diagnostics, face, deduction, "FW-DEDUCTION-ZERO",
                            "Zero-area deduction candidate has no effect.", false);
                        continue;
                    }

                    var found = TryAllow(face, deduction, out var allowed);
                    if (!found)
                    {
                        throw new InvalidOperationException(
                            "Missing directed formwork deduction rule for " + face.SourceCode + "->" + deduction.TargetCode +
                            " on face " + face.FaceId + ". Core will not mirror or synthesize the missing rule.");
                    }

                    if (!allowed)
                    {
                        var reason = deduction.AreaMm2 < _settings.MinSubtractAreaMm2
                            ? "Measured contact area is below MinSubtractAreaMm2=" + _settings.MinSubtractAreaMm2 + "."
                            : "Persisted directed deduction flag is disabled.";
                        AddDeductionTrace(trace, diagnostics, face, deduction, "FW-DEDUCTION-SKIPPED", reason, false);
                        continue;
                    }

                    deductionAreaMm2 = AddFinite(deductionAreaMm2, deduction.AreaMm2, face.FaceId + "/deduction");
                    if (deductionAreaMm2 > face.GrossAreaMm2)
                    {
                        throw new InvalidOperationException(
                            "Formwork deductions exceed gross area for face " + face.FaceId +
                            ". Host candidates must be union-resolved and non-overlapping before Core evaluation.");
                    }

                    AddDeductionTrace(trace, diagnostics, face, deduction, "FW-DEDUCTION-APPLIED",
                        "Directed deduction rule applied.", true);
                    appliedDeductions.Add(new QuantityGeometryDeduction
                    {
                        ElementId = "CATEGORY:" + deduction.TargetCode,
                        ElementName = "Category " + deduction.TargetCode,
                        Relation = deduction.Basis == QuantityFormworkDeductionBasis.Concrete
                            ? QuantityGeometryRelation.FaceContact
                            : QuantityGeometryRelation.FaceOverlap,
                        Area = deduction.AreaMm2 / 1000000d,
                        RegionKey = deduction.RegionKey,
                        FaceId = face.FaceId
                    });
                }

                var result = new QuantityFormworkFaceResult(
                    face.FaceId,
                    face.SourceCode,
                    face.Kind,
                    true,
                    face.GrossAreaMm2,
                    face.GrossAreaMm2,
                    deductionAreaMm2,
                    categoryRule.FaceAngleThresholdDeg,
                    trace.AsReadOnly());
                results.Add(result);
                explanations.Add(new QuantityFormworkFaceExplanation
                {
                    FaceId = face.FaceId,
                    FaceType = face.Kind.ToString(),
                    GrossArea = result.GrossAreaM2,
                    DeductionArea = result.DeductionAreaM2,
                    NetArea = result.NetAreaM2,
                    Deductions = appliedDeductions.AsReadOnly()
                });
            }

            var explanation = new QuantityGeometryExplanation
            {
                ElementId = normalizedElementId,
                ElementName = normalizedElementName,
                SourceHandles = SnapshotHandles(sourceHandles),
                GeometryFingerprint = (geometryFingerprint ?? string.Empty).Trim(),
                FormworkFaces = explanations.AsReadOnly(),
                Diagnostics = diagnostics.AsReadOnly()
            };
            explanation.Validate(new QuantityGeometryTolerances());

            return new QuantityFormworkCalculationResult(results.AsReadOnly(), explanation);
        }

        private bool TryAllow(
            QuantityFormworkFaceCandidate face,
            QuantityFormworkDeductionCandidate deduction,
            out bool allowed)
        {
            if (face.Kind == QuantityFormworkFaceKind.Side)
            {
                return deduction.Basis == QuantityFormworkDeductionBasis.Concrete
                    ? _gate.TryAllowSideFormworkByConcreteDeduction(face.SourceCode, deduction.TargetCode, deduction.AreaMm2, out allowed)
                    : _gate.TryAllowSideFormworkBySideFormworkDeduction(face.SourceCode, deduction.TargetCode, deduction.AreaMm2, out allowed);
            }

            return deduction.Basis == QuantityFormworkDeductionBasis.Concrete
                ? _gate.TryAllowBottomFormworkByConcreteDeduction(face.SourceCode, deduction.TargetCode, deduction.AreaMm2, out allowed)
                : _gate.TryAllowBottomFormworkByBottomFormworkDeduction(face.SourceCode, deduction.TargetCode, deduction.AreaMm2, out allowed);
        }

        private static List<QuantityFormworkFaceCandidate> SnapshotFaces(IEnumerable<QuantityFormworkFaceCandidate> candidates)
        {
            var result = new List<QuantityFormworkFaceCandidate>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var face in candidates)
            {
                if (face == null) throw new ArgumentException("Formwork face candidates cannot contain null entries.", nameof(candidates));
                if (!ids.Add(face.FaceId))
                    throw new ArgumentException("Formwork face ids must be unique: " + face.FaceId + ".", nameof(candidates));
                result.Add(face);
            }
            return result;
        }

        private static IReadOnlyList<string> SnapshotHandles(IEnumerable<string>? handles)
        {
            if (handles == null) return Array.Empty<string>();
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in handles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length != 0 && seen.Add(handle)) result.Add(handle);
            }
            return result.AsReadOnly();
        }

        private static QuantityFormworkFaceResult Excluded(
            QuantityFormworkFaceCandidate face,
            double faceAngleThresholdDeg,
            List<QuantityFormworkRuleTrace> trace)
        {
            return new QuantityFormworkFaceResult(
                face.FaceId,
                face.SourceCode,
                face.Kind,
                false,
                face.GrossAreaMm2,
                0d,
                0d,
                faceAngleThresholdDeg,
                trace.AsReadOnly());
        }

        private static QuantityFormworkFaceExplanation ExplainExcluded(QuantityFormworkFaceCandidate face)
        {
            return new QuantityFormworkFaceExplanation
            {
                FaceId = face.FaceId,
                FaceType = face.Kind.ToString(),
                GrossArea = 0d,
                DeductionArea = 0d,
                NetArea = 0d,
                Deductions = Array.Empty<QuantityGeometryDeduction>()
            };
        }

        private static void AddTrace(
            List<QuantityFormworkRuleTrace> trace,
            List<string> diagnostics,
            QuantityFormworkFaceCandidate face,
            string code,
            string message,
            bool applied,
            double areaMm2)
        {
            trace.Add(new QuantityFormworkRuleTrace(face.FaceId, code, message, applied, areaMm2));
            diagnostics.Add(face.FaceId + ": " + code + ": " + message);
        }

        private static void AddDeductionTrace(
            List<QuantityFormworkRuleTrace> trace,
            List<string> diagnostics,
            QuantityFormworkFaceCandidate face,
            QuantityFormworkDeductionCandidate deduction,
            string code,
            string message,
            bool applied)
        {
            trace.Add(new QuantityFormworkRuleTrace(
                face.FaceId,
                code,
                message,
                applied,
                deduction.AreaMm2,
                deduction.TargetCode,
                deduction.Basis));
            diagnostics.Add(face.FaceId + ": " + code + " " + face.SourceCode + "->" + deduction.TargetCode +
                " (" + deduction.Basis + "): " + message);
        }

        private static double AddFinite(double left, double right, string label)
        {
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException("Formwork area overflow while accumulating " + label + ".");
            return result;
        }

        private static string NormalizeRequired(string value, string name)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Formwork element id must not be blank.", name);
            return normalized;
        }
    }
}
