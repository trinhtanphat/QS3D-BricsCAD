using System;
using System.IO;

namespace QS3D.Core.Export
{
    public static class Qs3dReviewTraceValidator
    {
        public static void ValidateIdentity(
            Qs3dReviewTrace trace,
            string currentDrawingFingerprint,
            string currentModelRevision)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            var fingerprint = Required(currentDrawingFingerprint, nameof(currentDrawingFingerprint));
            var revision = Required(currentModelRevision, nameof(currentModelRevision));

            ValidateTraceKey(trace);

            if (!string.Equals(trace.DrawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "QS3D Review workbook belongs to a different drawing fingerprint. Model Locate was refused.");
            if (!string.Equals(trace.ModelRevision, revision, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "QS3D Review workbook model revision is stale. Export a current workbook before Model Locate.");
        }

        public static void ValidateTraceKey(Qs3dReviewTrace trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            string expected;
            if (trace.Kind == Qs3dReviewTraceKind.Quantity)
            {
                if (trace.ElementIds.Count != 1 || trace.Handles.Count == 0)
                    throw new InvalidDataException("QS3D Review QTO trace identity is incomplete.");
                expected = Qs3dReviewXlsx.TraceKey(
                    "QTO", trace.DrawingFingerprint, trace.ElementIds[0], string.Join(";", trace.Handles));
            }
            else if (trace.Kind == Qs3dReviewTraceKind.Clash)
            {
                if (trace.Handles.Count != 2)
                    throw new InvalidDataException("QS3D Review clash trace identity is incomplete.");
                expected = Qs3dReviewXlsx.TraceKey(
                    "CLASH", trace.DrawingFingerprint, trace.ItemId, trace.Handles[0], trace.Handles[1]);
            }
            else if (trace.Kind == Qs3dReviewTraceKind.Duplicate)
            {
                if (trace.ElementIds.Count != 2)
                    throw new InvalidDataException("QS3D Review duplicate trace identity is incomplete.");
                expected = Qs3dReviewXlsx.TraceKey(
                    "DUPLICATE", trace.DrawingFingerprint, trace.ItemId, trace.ElementIds[0], trace.ElementIds[1]);
            }
            else
            {
                throw new InvalidDataException("QS3D Review trace kind is unsupported.");
            }

            if (!string.Equals(trace.TraceKey, expected, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "QS3D Review TRACE_KEY does not match the selected row identity. Model Locate was refused.");
        }

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Value must be canonical without surrounding whitespace.", parameterName);
            return value;
        }
    }
}
