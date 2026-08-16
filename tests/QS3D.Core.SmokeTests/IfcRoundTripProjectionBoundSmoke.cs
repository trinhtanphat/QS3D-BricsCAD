using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionBoundSmoke
    {
        private const int MaximumProjections = IfcRoundTripProjectionSet.MaxProjectionCount;

        internal static void Run()
        {
            NullSourceIsRejected();
            NullProjectionReportsOffendingIndex();
            DuplicateIfcGlobalIdReportsOffendingIndex();
            DuplicateQs3dElementIdReportsOffendingIndex();
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedProjection();
            ExactBoundaryPreservesCanonicalOrdering();
        }

        private static void NullSourceIsRejected()
        {
            Capture<ArgumentNullException>(() => IfcRoundTripProjectionSet.Create(null!));
        }

        private static void NullProjectionReportsOffendingIndex()
        {
            var source = new[]
            {
                Projection(0),
                null!,
                Projection(2)
            };

            var error = Capture<ArgumentException>(() => IfcRoundTripProjectionSet.Create(source));
            Contains("index 1", error.Message, "Null projection failure must identify the offending input index.");
        }

        private static void DuplicateIfcGlobalIdReportsOffendingIndex()
        {
            var source = new[]
            {
                Projection(0),
                Projection(1),
                Projection(2, ifcGlobalId: "IFC-00000")
            };

            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));
            Contains("index 2", error.Message, "Duplicate IFC identity failure must identify the offending input index.");
        }

        private static void DuplicateQs3dElementIdReportsOffendingIndex()
        {
            var source = new[]
            {
                Projection(0),
                Projection(1),
                Projection(2, qs3dElementId: "qs3d-00000")
            };

            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));
            Contains("index 2", error.Message, "Duplicate QS3D identity failure must identify the offending input index.");
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumProjections + 1);
            var error = Capture<ArgumentException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted IFC projection input must fail before enumeration.");
            Contains("10000", error.Message, "Counted oversize failure must report the projection bound.");
            Contains("index 10000", error.Message, "Counted oversize failure must report the first disallowed index.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedProjection()
        {
            var source = new StreamingProjections(MaximumProjections + 2);
            var error = Capture<ArgumentException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(
                MaximumProjections + 1,
                source.YieldedCount,
                "Streaming IFC projection ingestion must stop immediately after observing projection 10,001.");
            Contains("10000", error.Message, "Streaming oversize failure must report the projection bound.");
            Contains("index 10000", error.Message, "Streaming oversize failure must report the first disallowed index.");
        }

        private static void ExactBoundaryPreservesCanonicalOrdering()
        {
            var source = new IfcRoundTripProjection[MaximumProjections];
            for (var index = 0; index < source.Length; index++)
                source[index] = Projection(MaximumProjections - 1 - index);

            var set = IfcRoundTripProjectionSet.Create(source);

            Equal(MaximumProjections, set.Items.Count, "IFC projection set must accept exactly 10,000 valid projections.");
            Equal("IFC-00000", set.Items[0].IfcGlobalId, "Canonical IFC ordering changed at the first projection.");
            Equal("IFC-09999", set.Items[set.Items.Count - 1].IfcGlobalId, "Canonical IFC ordering changed at the final projection.");
        }

        private static IfcRoundTripProjection Projection(
            int index,
            string? ifcGlobalId = null,
            string? qs3dElementId = null)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new IfcRoundTripProjection(
                qs3dElementId ?? "QS3D-" + suffix,
                ifcGlobalId ?? "IFC-" + suffix,
                "Wall",
                Array.Empty<IfcRoundTripNumericProperty>(),
                1d,
                "m3",
                new[] { "smoke" });
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

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
            }
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<IfcRoundTripProjection>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingProjections : IEnumerable<IfcRoundTripProjection>
        {
            private readonly int _count;

            internal StreamingProjections(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return Projection(index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class IfcRoundTripProjectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripProjectionBoundSmoke.Run();
        }
    }
}
