using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionNestedCollectionBoundSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownOversizedDimensionsFailBeforeEnumeration();
            KnownOversizedProvenanceFailsBeforeEnumeration();
            StreamingDimensionsStopAtFirstDisallowedItem();
            KnownCountTraversalMismatchFailsClosed();
            ExactBoundaryAndCanonicalOrderingRemainAccepted();
        }

        private static void KnownOversizedDimensionsFailBeforeEnumeration()
        {
            var dimensions = new ThrowOnEnumerationCollection<IfcRoundTripNumericProperty>(Limit + 1);
            ExpectInvalidOperation(
                () => CreateProjection(dimensions, new[] { "source" }),
                "dimension",
                "Known oversized IFC dimensions must fail before enumeration.");
            if (dimensions.EnumerationAttempts != 0)
                throw new InvalidOperationException("Known oversized IFC dimensions must not be enumerated before rejection.");
        }

        private static void KnownOversizedProvenanceFailsBeforeEnumeration()
        {
            var provenance = new ThrowOnEnumerationCollection<string>(Limit + 1);
            ExpectInvalidOperation(
                () => CreateProjection(Array.Empty<IfcRoundTripNumericProperty>(), provenance),
                "provenance",
                "Known oversized IFC provenance must fail before enumeration.");
            if (provenance.EnumerationAttempts != 0)
                throw new InvalidOperationException("Known oversized IFC provenance must not be enumerated before rejection.");
        }

        private static void StreamingDimensionsStopAtFirstDisallowedItem()
        {
            var probe = new EnumerationProbe();
            ExpectInvalidOperation(
                () => CreateProjection(StreamingDimensions(probe), new[] { "source" }),
                "dimension",
                "Streaming IFC dimensions must fail at the first item above the supported bound.");

            if (probe.Yielded != Limit + 1)
                throw new InvalidOperationException(
                    "Streaming IFC dimension rejection must stop at item " + (Limit + 1) + "; observed " + probe.Yielded + " yielded items.");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var oneDimension = new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") };
            var mismatchedDimensions = new KnownCountEnumerable<IfcRoundTripNumericProperty>(2, oneDimension);
            ExpectInvalidOperation(
                () => CreateProjection(mismatchedDimensions, new[] { "source" }),
                "Count does not match",
                "IFC dimension Count/traversal mismatch must fail closed.");

            var mismatchedProvenance = new KnownCountEnumerable<string>(2, new[] { "source" });
            ExpectInvalidOperation(
                () => CreateProjection(Array.Empty<IfcRoundTripNumericProperty>(), mismatchedProvenance),
                "Count does not match",
                "IFC provenance Count/traversal mismatch must fail closed.");
        }

        private static void ExactBoundaryAndCanonicalOrderingRemainAccepted()
        {
            var dimensions = new List<IfcRoundTripNumericProperty>(Limit);
            var provenance = new List<string>(Limit);
            for (var index = Limit - 1; index >= 0; index--)
            {
                dimensions.Add(new IfcRoundTripNumericProperty("D" + index.ToString("D5"), index, "m"));
                provenance.Add("P" + index.ToString("D5"));
            }

            var projection = CreateProjection(dimensions, provenance);
            if (projection.Dimensions.Count != Limit || projection.Provenance.Count != Limit)
                throw new InvalidOperationException("Exact-boundary IFC nested collections must remain accepted.");
            if (!string.Equals(projection.Dimensions[0].Name, "D00000", StringComparison.Ordinal) ||
                !string.Equals(projection.Dimensions[Limit - 1].Name, "D09999", StringComparison.Ordinal))
                throw new InvalidOperationException("IFC dimension canonical ordering changed at the exact boundary.");
            if (!string.Equals(projection.Provenance[0], "P00000", StringComparison.Ordinal) ||
                !string.Equals(projection.Provenance[Limit - 1], "P09999", StringComparison.Ordinal))
                throw new InvalidOperationException("IFC provenance canonical ordering changed at the exact boundary.");
        }

        private static IfcRoundTripProjection CreateProjection(
            IEnumerable<IfcRoundTripNumericProperty> dimensions,
            IEnumerable<string> provenance)
        {
            return new IfcRoundTripProjection(
                "E1",
                "IFC-1",
                "Wall",
                dimensions,
                1d,
                "m3",
                provenance);
        }

        private static IEnumerable<IfcRoundTripNumericProperty> StreamingDimensions(EnumerationProbe probe)
        {
            for (var index = 0; ; index++)
            {
                probe.Yielded++;
                if (probe.Yielded > Limit + 1)
                    throw new InvalidOperationException("IFC dimension source was enumerated beyond the first disallowed item.");
                yield return new IfcRoundTripNumericProperty("D" + index.ToString("D5"), index, "m");
            }
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        failureMessage + " Diagnostic did not contain '" + expectedMessageFragment + "': " + exception.Message,
                        exception);
                return;
            }

            throw new InvalidOperationException(failureMessage);
        }

        private sealed class EnumerationProbe
        {
            internal int Yielded { get; set; }
        }

        private sealed class ThrowOnEnumerationCollection<T> : IEnumerable<T>, IReadOnlyCollection<T>
        {
            internal ThrowOnEnumerationCollection(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int EnumerationAttempts { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new InvalidOperationException("Oversized known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountEnumerable<T> : IEnumerable<T>, IReadOnlyCollection<T>
        {
            private readonly IEnumerable<T> _items;

            internal KnownCountEnumerable(int count, IEnumerable<T> items)
            {
                Count = count;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
