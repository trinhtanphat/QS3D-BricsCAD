using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionCollectionBoundSmoke
    {
        private const int MaximumProjections = 10000;

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedProjection();
            ExactBoundaryRemainsAcceptedAndCanonical();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumProjections + 1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted IFC projection input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted oversize failure must report the IFC projection bound.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedProjection()
        {
            var source = new StreamingProjections(MaximumProjections + 2);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(
                MaximumProjections + 1,
                source.YieldedCount,
                "Streaming IFC projection ingestion must stop immediately after observing projection 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize failure must report the IFC projection bound.");
        }

        private static void ExactBoundaryRemainsAcceptedAndCanonical()
        {
            var projections = new IfcRoundTripProjection[MaximumProjections];
            for (var index = 0; index < projections.Length; index++)
                projections[index] = Projection(MaximumProjections - 1 - index);

            var set = IfcRoundTripProjectionSet.Create(projections);
            Equal(MaximumProjections, set.Items.Count, "IFC projection set must accept exactly 10,000 valid projections.");
            Equal("ifc-00000", set.Items[0].IfcGlobalId, "Boundary-sized IFC projection set lost canonical first-item ordering.");
            Equal("ifc-09999", set.Items[set.Items.Count - 1].IfcGlobalId, "Boundary-sized IFC projection set lost canonical last-item ordering.");
        }

        private static IfcRoundTripProjection Projection(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new IfcRoundTripProjection(
                "ELEMENT-" + suffix,
                "ifc-" + suffix,
                "IfcBuildingElementProxy",
                new[] { new IfcRoundTripNumericProperty("Length", index + 1d, "m") },
                index + 1d,
                "m",
                new[] { "source:bound-smoke" });
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
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
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
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

    internal static class IfcRoundTripProjectionCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripProjectionCollectionBoundSmoke.Run();
        }
    }
}
