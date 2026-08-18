using System;
using System.Collections.Generic;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class DrawingUnitMetadataDiagnosticSafetySmoke
    {
        public static void Run()
        {
            var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = "meter",
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = "METER"
            };

            if (!DrawingUnitResolutionPolicy.TryResolve(null, canonical, out var resolution) ||
                resolution.Unit != LengthUnit.Meter)
                throw new Exception("Canonical drawing-unit override must remain case-insensitive.");
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(canonical, true, LengthUnit.Meter);

            AssertRejectedWithoutRawOverride("NotAUnit");
            AssertRejectedWithoutRawOverride("Meter\tInjected");
            AssertRejectedWithoutRawOverride("Meter\nInjected");
            AssertRejectedWithoutRawOverride("Meter\rInjected");
            AssertRejectedWithoutRawOverride("Meter\u007fInjected");

            AssertRejectedWithoutRawBound("NotAUnit");
            AssertRejectedWithoutRawBound("Meter\tInjected");
            AssertRejectedWithoutRawBound("Meter\nInjected");
            AssertRejectedWithoutRawBound("Meter\rInjected");
            AssertRejectedWithoutRawBound("Meter\u007fInjected");
        }

        private static void AssertRejectedWithoutRawOverride(string raw)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = raw
            };

            var ex = Capture<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.TryResolve(null, metadata, out _));
            AssertSafeDiagnostic(ex.Message, raw, DrawingUnitResolutionPolicy.OverrideMetadataKey);
        }

        private static void AssertRejectedWithoutRawBound(string raw)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = raw
            };

            var ex = Capture<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(metadata, true, LengthUnit.Meter));
            AssertSafeDiagnostic(ex.Message, raw, DrawingUnitResolutionPolicy.BoundMetadataKey);
        }

        private static void AssertSafeDiagnostic(string message, string raw, string expectedContext)
        {
            if (message.IndexOf(raw, StringComparison.Ordinal) >= 0)
                throw new Exception("Invalid drawing-unit metadata diagnostic echoed hostile raw input.");
            if (message.IndexOfAny(new[] { '\t', '\r', '\n', '\u007f' }) >= 0)
                throw new Exception("Invalid drawing-unit metadata diagnostic contains control characters.");
            if (message.IndexOf("drawing-unit", StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception("Invalid drawing-unit metadata diagnostic lost drawing-unit context.");
            if (expectedContext == DrawingUnitResolutionPolicy.BoundMetadataKey &&
                message.IndexOf(expectedContext, StringComparison.Ordinal) < 0)
                throw new Exception("Bound drawing-unit diagnostic must retain the metadata-key context.");
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
