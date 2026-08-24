using System;
using System.Collections.Generic;
using System.Xml;

namespace QS3D.Core.Export
{
    public sealed class Qs3dReviewModelInfo
    {
        public Qs3dReviewModelInfo(string projectId, string drawingName, string drawingFingerprint, string modelRevision, DateTimeOffset exportedAtUtc, double? reinforcementTon = null)
        {
            ProjectId = Required(projectId, nameof(projectId));
            DrawingName = Optional(drawingName, nameof(drawingName));
            DrawingFingerprint = Required(drawingFingerprint, nameof(drawingFingerprint));
            ModelRevision = Required(modelRevision, nameof(modelRevision));
            if (exportedAtUtc == default(DateTimeOffset)) throw new ArgumentOutOfRangeException(nameof(exportedAtUtc));
            if (reinforcementTon.HasValue && (!Finite(reinforcementTon.Value) || reinforcementTon.Value < 0d)) throw new ArgumentOutOfRangeException(nameof(reinforcementTon));
            ExportedAtUtc = exportedAtUtc.ToUniversalTime();
            ReinforcementTon = reinforcementTon;
        }

        public string ProjectId { get; }
        public string DrawingName { get; }
        public string DrawingFingerprint { get; }
        public string ModelRevision { get; }
        public DateTimeOffset ExportedAtUtc { get; }
        public double? ReinforcementTon { get; }

        internal static void VerifyXml(string value, string parameterName)
        {
            try { XmlConvert.VerifyXmlChars(value ?? string.Empty); }
            catch (XmlException error) { throw new ArgumentException("Value contains characters that cannot be stored in XLSX XML.", parameterName, error); }
        }

        private static string Required(string value, string parameterName)
        {
            var normalized = Optional(value, parameterName);
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return normalized;
        }

        private static string Optional(string value, string parameterName)
        {
            var normalized = (value ?? string.Empty).Trim();
            VerifyXml(normalized, parameterName);
            return normalized;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class Qs3dReviewIssueGeometry
    {
        public Qs3dReviewIssueGeometry(
            string findingId,
            double? overlapX = null, double? overlapY = null, double? overlapZ = null,
            double? distanceMm = null, double? rotationDeltaDegrees = null, double? confidencePercent = null,
            DateTimeOffset? createdAtUtc = null, DateTimeOffset? lastCheckedAtUtc = null)
        {
            FindingId = Required(findingId, nameof(findingId));
            OverlapX = NonNegative(overlapX, nameof(overlapX));
            OverlapY = NonNegative(overlapY, nameof(overlapY));
            OverlapZ = NonNegative(overlapZ, nameof(overlapZ));
            DistanceMm = NonNegative(distanceMm, nameof(distanceMm));
            if (rotationDeltaDegrees.HasValue && (!Finite(rotationDeltaDegrees.Value) || rotationDeltaDegrees.Value < 0d || rotationDeltaDegrees.Value > 360d))
                throw new ArgumentOutOfRangeException(nameof(rotationDeltaDegrees));
            if (confidencePercent.HasValue && (!Finite(confidencePercent.Value) || confidencePercent.Value < 0d || confidencePercent.Value > 100d))
                throw new ArgumentOutOfRangeException(nameof(confidencePercent));
            RotationDeltaDegrees = rotationDeltaDegrees;
            ConfidencePercent = confidencePercent;
            CreatedAtUtc = createdAtUtc?.ToUniversalTime();
            LastCheckedAtUtc = lastCheckedAtUtc?.ToUniversalTime();
            if (CreatedAtUtc.HasValue && LastCheckedAtUtc.HasValue && LastCheckedAtUtc.Value < CreatedAtUtc.Value)
                throw new ArgumentException("LastCheckedAtUtc cannot be earlier than CreatedAtUtc.", nameof(lastCheckedAtUtc));
        }

        public string FindingId { get; }
        public double? OverlapX { get; }
        public double? OverlapY { get; }
        public double? OverlapZ { get; }
        public double? DistanceMm { get; }
        public double? RotationDeltaDegrees { get; }
        public double? ConfidencePercent { get; }
        public DateTimeOffset? CreatedAtUtc { get; }
        public DateTimeOffset? LastCheckedAtUtc { get; }

        private static double? NonNegative(double? value, string parameterName)
        {
            if (value.HasValue && (!Finite(value.Value) || value.Value < 0d)) throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static string Required(string value, string parameterName)
        {
            var normalized = (value ?? string.Empty).Trim();
            Qs3dReviewModelInfo.VerifyXml(normalized, parameterName);
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return normalized;
        }
    }

    public enum Qs3dReviewTraceKind { Quantity = 0, Clash = 1, Duplicate = 2 }

    public sealed class Qs3dReviewTrace
    {
        internal Qs3dReviewTrace(Qs3dReviewTraceKind kind, string sheetName, int rowNumber, string itemId, string drawingFingerprint, string modelRevision, string traceKey, IReadOnlyList<string> elementIds, IReadOnlyList<string> handles)
        {
            Kind = kind; SheetName = sheetName; RowNumber = rowNumber; ItemId = itemId; DrawingFingerprint = drawingFingerprint;
            ModelRevision = modelRevision; TraceKey = traceKey; ElementIds = elementIds; Handles = handles;
        }
        public Qs3dReviewTraceKind Kind { get; }
        public string SheetName { get; }
        public int RowNumber { get; }
        public string ItemId { get; }
        public string DrawingFingerprint { get; }
        public string ModelRevision { get; }
        public string TraceKey { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<string> Handles { get; }
    }
}
