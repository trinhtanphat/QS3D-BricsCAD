using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            DimensionCountDriftFailsBeforePublication();
            ProvenanceCountDriftFailsBeforePublication();
            ProjectionSetCountDriftFailsBeforePublication();
            QuantityEvidenceCountDriftFailsBeforePublication();
            ExchangeResultCountDriftFailsBeforePublication();
            RejectsNegativePostTraversalCount();
            RejectsConflictingPostTraversalCounts();
            StableCountedInputsRemainAccepted();
            PureStreamingInputsRemainAccepted();
        }

        private static void DimensionCountDriftFailsBeforePublication()
        {
            var dimensions = new PostTraversalCountCollection<IfcRoundTripNumericProperty>(
                1,
                2,
                new IfcRoundTripNumericProperty("Length", 2d, "m"));

            var error = Capture<InvalidOperationException>(() =>
                NewProjection("Q-DIM-STABLE", "IFC-DIM-STABLE", dimensions, new[] { "SRC" }));

            Contains("dimension source Count changed during traversal", error.Message,
                "Dimension Count drift must fail before canonical projection publication.");
            Equal(6, dimensions.CountReads,
                "Dimension Count evidence must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void ProvenanceCountDriftFailsBeforePublication()
        {
            var provenance = new PostTraversalCountCollection<string>(1, 2, "SRC");

            var error = Capture<InvalidOperationException>(() =>
                NewProjection(
                    "Q-PROV-STABLE",
                    "IFC-PROV-STABLE",
                    new[] { new IfcRoundTripNumericProperty("Length", 2d, "m") },
                    provenance));

            Contains("provenance source Count changed during traversal", error.Message,
                "Provenance Count drift must fail before canonical projection publication.");
            Equal(6, provenance.CountReads,
                "Provenance Count evidence must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void ProjectionSetCountDriftFailsBeforePublication()
        {
            var projections = new PostTraversalCountCollection<IfcRoundTripProjection>(
                1,
                2,
                NewProjection("Q-SET-STABLE", "IFC-SET-STABLE"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("projection source Count changed during traversal", error.Message,
                "Projection-set Count drift must fail before canonical sorting/publication.");
            Equal(6, projections.CountReads,
                "Projection-set Count evidence must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void QuantityEvidenceCountDriftFailsBeforePublication()
        {
            var evidence = new PostTraversalCountCollection<IfcRoundTripQuantityEvidence>(
                1,
                2,
                NewEvidence("NetVolume", 1d, "IFC-QTO-STABLE", "SRC-QTO"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripQuantityEvidenceSet.Create(evidence));

            Contains("quantity evidence source Count changed during traversal", error.Message,
                "Quantity-evidence Count drift must fail before sorting/grouping publication.");
            Equal(6, evidence.CountReads,
                "Quantity-evidence Count must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void ExchangeResultCountDriftFailsBeforePublication()
        {
            var results = new PostTraversalCountCollection<IfcRoundTripExchangeResult>(
                1,
                2,
                NewUnsupportedResult("IFC-RESULT-STABLE"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripExchangeResultSet.Create(results));

            Contains("IFC exchange result source Count changed during traversal", error.Message,
                "Exchange-result Count drift must fail before result sorting/publication.");
            Equal(6, results.CountReads,
                "Exchange-result Count evidence must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void RejectsNegativePostTraversalCount()
        {
            var projections = new PostTraversalCountCollection<IfcRoundTripProjection>(
                1,
                -1,
                NewProjection("Q-NEGATIVE", "IFC-NEGATIVE"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("invalid negative known Count value after traversal", error.Message,
                "Newly negative post-traversal Count evidence must fail closed.");
        }

        private static void RejectsConflictingPostTraversalCounts()
        {
            var results = new PostTraversalConflictingCountCollection<IfcRoundTripExchangeResult>(
                1,
                NewUnsupportedResult("IFC-CONFLICT"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripExchangeResultSet.Create(results));

            Contains("conflicting known Count values after traversal", error.Message,
                "Newly conflicting post-traversal Count evidence must fail closed.");
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var dimensions = new PostTraversalCountCollection<IfcRoundTripNumericProperty>(
                2,
                2,
                new IfcRoundTripNumericProperty("Width", 3d, "m"),
                new IfcRoundTripNumericProperty("Length", 4d, "m"));
            var provenance = new PostTraversalCountCollection<string>(2, 2, "SRC-B", "SRC-A");
            var projection = NewProjection(
                "Q-HONEST-STABILITY",
                "IFC-HONEST-STABILITY",
                dimensions,
                provenance);

            Equal("Length", projection.Dimensions[0].Name,
                "Stable counted dimensions must retain canonical ordering.");
            Equal("SRC-A", projection.Provenance[0],
                "Stable counted provenance must retain canonical ordering.");
            Equal(9, dimensions.CountReads,
                "Stable two-item dimension Count must be bound at admission, rebound around every MoveNext and Current, and checked after traversal.");
            Equal(9, provenance.CountReads,
                "Stable two-item provenance Count must be bound at admission, rebound around every MoveNext and Current, and checked after traversal.");

            var projectionSource = new PostTraversalCountCollection<IfcRoundTripProjection>(
                1,
                1,
                projection);
            Equal(1, IfcRoundTripProjectionSet.Create(projectionSource).Items.Count,
                "Stable counted projection sets must remain accepted.");
            Equal(6, projectionSource.CountReads,
                "Stable one-item projection-set Count must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");

            var evidenceSource = new PostTraversalCountCollection<IfcRoundTripQuantityEvidence>(
                1,
                1,
                NewEvidence("NetVolume", 2d, "IFC-HONEST-QTO", "SRC-QTO"));
            Equal(1, IfcRoundTripQuantityEvidenceSet.Create(evidenceSource).CandidateCount,
                "Stable counted quantity evidence must remain accepted.");
            Equal(6, evidenceSource.CountReads,
                "Stable quantity-evidence Count must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");

            var resultSource = new PostTraversalCountCollection<IfcRoundTripExchangeResult>(
                1,
                1,
                NewUnsupportedResult("IFC-HONEST-RESULT"));
            Equal(1, IfcRoundTripExchangeResultSet.Create(resultSource).Items.Count,
                "Stable counted exchange results must remain accepted.");
            Equal(6, resultSource.CountReads,
                "Stable exchange-result Count must be bound at admission, rebound around MoveNext and Current, and checked after traversal.");
        }

        private static void PureStreamingInputsRemainAccepted()
        {
            var projection = NewProjection(
                "Q-STREAM",
                "IFC-STREAM",
                Stream(new IfcRoundTripNumericProperty("Length", 1d, "m")),
                Stream("SRC-STREAM"));

            Equal(1, IfcRoundTripProjectionSet.Create(Stream(projection)).Items.Count,
                "Pure streaming projection inputs must remain accepted.");
            Equal(1, IfcRoundTripQuantityEvidenceSet.Create(
                Stream(NewEvidence("NetVolume", 1d, "IFC-STREAM-QTO", "SRC-STREAM"))).CandidateCount,
                "Pure streaming quantity evidence must remain accepted.");
            Equal(1, IfcRoundTripExchangeResultSet.Create(
                Stream(NewUnsupportedResult("IFC-STREAM-RESULT"))).Items.Count,
                "Pure streaming exchange results must remain accepted.");
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

        private static IfcRoundTripQuantityEvidence NewEvidence(
            string key,
            double value,
            string externalSource,
            string provenance)
        {
            return new IfcRoundTripQuantityEvidence(
                key,
                value,
                "m3",
                externalSource,
                provenance);
        }

        private static IfcRoundTripExchangeResult NewUnsupportedResult(string externalObjectId)
        {
            return new IfcRoundTripExchangeResult(
                externalObjectId,
                IfcRoundTripResultState.Unsupported,
                null,
                stateDetail: "unsupported");
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            foreach (var item in items)
                yield return item;
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

        private sealed class PostTraversalCountCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private bool _completed;

            internal PostTraversalCountCollection(
                int initialCount,
                int postTraversalCount,
                params T[] items)
            {
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _completed ? _postTraversalCount : _initialCount;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new TraversalEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TraversalEnumerator : IEnumerator<T>
            {
                private readonly PostTraversalCountCollection<T> _owner;
                private int _index = -1;

                internal TraversalEnumerator(PostTraversalCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._items.Length)
                        return true;

                    _owner._completed = true;
                    return false;
                }

                public void Reset()
                {
                    _index = -1;
                    _owner._completed = false;
                }

                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class PostTraversalConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private bool _completed;

            internal PostTraversalConflictingCountCollection(int initialCount, params T[] items)
            {
                _initialCount = initialCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<T>.Count => _initialCount;
            int IReadOnlyCollection<T>.Count => _completed ? _initialCount + 1 : _initialCount;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new TraversalEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TraversalEnumerator : IEnumerator<T>
            {
                private readonly PostTraversalConflictingCountCollection<T> _owner;
                private int _index = -1;

                internal TraversalEnumerator(PostTraversalConflictingCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._items.Length)
                        return true;

                    _owner._completed = true;
                    return false;
                }

                public void Reset()
                {
                    _index = -1;
                    _owner._completed = false;
                }

                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class IfcRoundTripKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripKnownCountStabilitySmoke.Run();
        }
    }
}