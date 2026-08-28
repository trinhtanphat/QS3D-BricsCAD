using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripKnownCountEarlyOverrunSmoke
    {
        internal static void Run()
        {
            DimensionOverrunWinsBeforeNullProcessing();
            ProvenanceOverrunWinsBeforeTokenProcessing();
            ProjectionOverrunWinsBeforeIdentityProcessing();
            QuantityEvidenceOverrunWinsBeforeNullProcessing();
            ExchangeResultOverrunWinsBeforeDuplicateProcessing();
            UnderYieldStillFailsAfterTraversal();
            HonestCountedInputsRemainCanonical();
        }

        private static void DimensionOverrunWinsBeforeNullProcessing()
        {
            var dimensions = new CountingMisreportedCollection<IfcRoundTripNumericProperty>(
                1,
                new IfcRoundTripNumericProperty("Length", 2d, "m"),
                null!);

            var error = Capture<InvalidOperationException>(() =>
                NewProjection("Q-DIM", "IFC-DIM", dimensions, new[] { "SRC" }));

            Contains("dimension source Count was exceeded", error.Message,
                "Dimension known-count integrity must win before null semantic validation.");
            Equal(2, dimensions.MoveNextCalls,
                "Dimension overrun must stop on the first unexpected yielded item.");
        }

        private static void ProvenanceOverrunWinsBeforeTokenProcessing()
        {
            var provenance = new CountingMisreportedCollection<string>(1, "SRC", null!);

            var error = Capture<InvalidOperationException>(() =>
                NewProjection(
                    "Q-PROV",
                    "IFC-PROV",
                    new[] { new IfcRoundTripNumericProperty("Length", 2d, "m") },
                    provenance));

            Contains("provenance source Count was exceeded", error.Message,
                "Provenance known-count integrity must win before token validation.");
            Equal(2, provenance.MoveNextCalls,
                "Provenance overrun must stop on the first unexpected yielded item.");
        }

        private static void ProjectionOverrunWinsBeforeIdentityProcessing()
        {
            var projections = new CountingMisreportedCollection<IfcRoundTripProjection>(
                1,
                NewProjection("Q-1", "IFC-1"),
                null!);

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("projection source Count was exceeded", error.Message,
                "Projection-set known-count integrity must win before null/identity processing.");
            Equal(2, projections.MoveNextCalls,
                "Projection-set overrun must stop on the first unexpected yielded item.");
        }

        private static void QuantityEvidenceOverrunWinsBeforeNullProcessing()
        {
            var evidence = new CountingMisreportedCollection<IfcRoundTripQuantityEvidence>(
                1,
                new IfcRoundTripQuantityEvidence("NetVolume", 1d, "m3", "IFC-1", "SRC-1"),
                null!);

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripQuantityEvidenceSet.Create(evidence));

            Contains("quantity evidence source Count was exceeded", error.Message,
                "Quantity-evidence known-count integrity must win before null/grouping processing.");
            Equal(2, evidence.MoveNextCalls,
                "Quantity-evidence overrun must stop on the first unexpected yielded item.");
        }

        private static void ExchangeResultOverrunWinsBeforeDuplicateProcessing()
        {
            var first = new IfcRoundTripExchangeResult(
                "IFC-RESULT",
                IfcRoundTripResultState.Unsupported,
                null,
                stateDetail: "unsupported");
            var duplicate = new IfcRoundTripExchangeResult(
                "IFC-RESULT",
                IfcRoundTripResultState.Unsupported,
                null,
                stateDetail: "duplicate");
            var results = new CountingMisreportedCollection<IfcRoundTripExchangeResult>(1, first, duplicate);

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripExchangeResultSet.Create(results));

            Contains("IFC exchange result source Count was exceeded", error.Message,
                "Exchange-result known-count integrity must win before duplicate-identity mutation.");
            Equal(2, results.MoveNextCalls,
                "Exchange-result overrun must stop on the first unexpected yielded item.");
        }

        private static void UnderYieldStillFailsAfterTraversal()
        {
            var projections = new CountingMisreportedCollection<IfcRoundTripProjection>(
                2,
                NewProjection("Q-U", "IFC-U"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("does not match enumerated projection count", error.Message,
                "Under-yield must remain a post-traversal Count-integrity failure.");
            Equal(2, projections.MoveNextCalls,
                "Under-yield must enumerate through completion before final mismatch validation.");
        }

        private static void HonestCountedInputsRemainCanonical()
        {
            var projection = NewProjection(
                "Q-HONEST",
                "IFC-HONEST",
                new CountingMisreportedCollection<IfcRoundTripNumericProperty>(
                    2,
                    new IfcRoundTripNumericProperty("Width", 3d, "m"),
                    new IfcRoundTripNumericProperty("Length", 4d, "m")),
                new CountingMisreportedCollection<string>(2, "SRC-B", "SRC-A"));

            Equal("Length", projection.Dimensions[0].Name,
                "Honest counted dimensions must retain canonical sorting.");
            Equal("SRC-A", projection.Provenance[0],
                "Honest counted provenance must retain canonical sorting.");

            var evidence = IfcRoundTripQuantityEvidenceSet.Create(
                new CountingMisreportedCollection<IfcRoundTripQuantityEvidence>(
                    2,
                    new IfcRoundTripQuantityEvidence("NetVolume", 2d, "m3", "IFC-HONEST", "SRC-B"),
                    new IfcRoundTripQuantityEvidence("NetVolume", 1d, "m3", "IFC-HONEST", "SRC-A")));
            Equal(2, evidence.CandidateCount,
                "Honest counted quantity evidence must retain candidate grouping semantics.");

            var results = IfcRoundTripExchangeResultSet.Create(
                new CountingMisreportedCollection<IfcRoundTripExchangeResult>(
                    1,
                    new IfcRoundTripExchangeResult(
                        "IFC-RESULT-HONEST",
                        IfcRoundTripResultState.Unsupported,
                        null,
                        stateDetail: "unsupported")));
            Equal(1, results.Items.Count,
                "Honest counted exchange results must remain accepted.");
        }

        private static IfcRoundTripProjection NewProjection(
            string qs3dId,
            string ifcId,
            IEnumerable<IfcRoundTripNumericProperty>? dimensions = null,
            IEnumerable<string>? provenance = null)
        {
            return new IfcRoundTripProjection(
                qs3dId,
                ifcId,
                "Wall",
                dimensions ?? new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") },
                1d,
                "m3",
                provenance ?? new[] { "SRC" });
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountingMisreportedCollection<T> : ICollection<T>
        {
            private readonly T[] _items;

            internal CountingMisreportedCollection(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator() => new CountingEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class CountingEnumerator : IEnumerator<T>
            {
                private readonly CountingMisreportedCollection<T> _owner;
                private int _index = -1;

                internal CountingEnumerator(CountingMisreportedCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() { _index = -1; }
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class IfcRoundTripKnownCountEarlyOverrunRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripKnownCountEarlyOverrunSmoke.Run();
        }
    }
}
