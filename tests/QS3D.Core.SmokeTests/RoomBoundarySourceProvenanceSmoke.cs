using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundarySourceProvenanceSmoke
    {
        internal static void Run()
        {
            MalformedUtf16FailsAtSegmentAdmission();
            ControlCharactersFailAtSegmentAdmission();
            CanonicalOptionalProvenanceRemainsSupported();
            DirectDiscoveryRetainsCanonicalProvenance();
        }

        private static void MalformedUtf16FailsAtSegmentAdmission()
        {
            var high = Capture<ArgumentException>(() => Segment("SRC-\ud800"));
            Contains("well-formed UTF-16", high.Message,
                "Unpaired high-surrogate provenance must fail at BoundarySegment admission.");

            var low = Capture<ArgumentException>(() => Segment("SRC-\udc00"));
            Contains("well-formed UTF-16", low.Message,
                "Unpaired low-surrogate provenance must fail at BoundarySegment admission.");
        }

        private static void ControlCharactersFailAtSegmentAdmission()
        {
            var error = Capture<ArgumentException>(() => Segment("SRC-\u0001-ID"));
            Contains("control characters", error.Message,
                "Control-bearing room-boundary provenance must fail before topology discovery.");
        }

        private static void CanonicalOptionalProvenanceRemainsSupported()
        {
            Equal(string.Empty, Segment(null).SourceId,
                "Null optional source provenance must remain canonical empty text.");
            Equal(string.Empty, Segment("   ").SourceId,
                "Whitespace-only optional source provenance must remain canonical empty text.");
            Equal("SRC-01", Segment("  SRC-01  ").SourceId,
                "Existing surrounding-whitespace normalization must remain stable.");
            Equal("SRC-😀", Segment("SRC-😀").SourceId,
                "Well-formed surrogate pairs must remain accepted.");
        }

        private static void DirectDiscoveryRetainsCanonicalProvenance()
        {
            var segments = new[]
            {
                new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), "  EDGE-A  "),
                new BoundarySegment(new Point2(1d, 0d), new Point2(1d, 1d), "EDGE-B"),
                new BoundarySegment(new Point2(1d, 1d), new Point2(0d, 1d), "EDGE-C"),
                new BoundarySegment(new Point2(0d, 1d), new Point2(0d, 0d), "EDGE-D")
            };

            var boundaries = new RoomBoundaryEngine().Discover(segments, tolerance: 0.001d, minimumArea: 0d);
            Equal(1, boundaries.Count,
                "Stable direct-engine square input must still produce one boundary.");
            Equal(4, boundaries[0].SourceIds.Count,
                "Direct-engine boundary must retain all canonical source provenance ids.");
            ContainsValue(boundaries[0].SourceIds, "EDGE-A",
                "Trim-normalized provenance must reach direct discovery canonically.");
        }

        private static BoundarySegment Segment(string? sourceId)
        {
            return new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), sourceId!);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void ContainsValue(IReadOnlyList<string> values, string expected, string message)
        {
            for (var index = 0; index < values.Count; index++)
                if (string.Equals(values[index], expected, StringComparison.Ordinal)) return;
            throw new InvalidOperationException(message + " Expected value=" + expected + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class RoomBoundarySourceProvenanceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RoomBoundarySourceProvenanceSmoke.Run();
        }
    }
}
