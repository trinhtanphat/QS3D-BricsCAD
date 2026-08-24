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

            if (!string.Equals(trace.DrawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "QS3D Review workbook belongs to a different drawing fingerprint. Model Locate was refused.");
            if (!string.Equals(trace.ModelRevision, revision, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "QS3D Review workbook model revision is stale. Export a current workbook before Model Locate.");
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
