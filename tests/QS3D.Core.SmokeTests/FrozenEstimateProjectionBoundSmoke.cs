using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionBoundSmoke
    {
        private const int MaxLines = 10000;
        private const string SemanticIdentity = "frozen-estimate";
        private const string SourceIdentity = "element-1";
        private const string QuantityKey = "net-volume";
        private const string Unit = "m3";
        private const string Currency = "USD";
        private static readonly DateTime EffectiveUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AsOfUtc = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverLimitFailsBeforeEnumeration();
            NegativeGenericCountFailsBeforeEnumeration();
            NegativeReadOnlyCountFailsBeforeEnumeration();
            NegativeNonGenericCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            OversizeKnownCountPreservesBoundPrecedence();
            ExactLimitIsAccepted();
            FirstOverLimitLineFailsWithoutOverrun();
        }

        private static void KnownCountOverLimitFailsBeforeEnumeration()
        {
            var source = new KnownCountLineSource();
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertBoundError(error);
            Assert(!source.Enumerated, "Known-count over-limit projection must fail before source enumeration starts.");
        }

        private static void NegativeGenericCountFailsBeforeEnumeration()
        {
            var source = new GenericKnownCountLineSource(-1);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertContains("invalid negative known count", error.Message, "Negative generic Count must fail closed.");
            Assert(!source.Enumerated, "Negative generic Count must fail before source enumeration starts.");
        }

        private static void NegativeReadOnlyCountFailsBeforeEnumeration()
        {
            var source = new ReadOnlyKnownCountLineSource(-1);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertContains("invalid negative known count", error.Message, "Negative read-only Count must fail closed.");
            Assert(!source.Enumerated, "Negative read-only Count must fail before source enumeration starts.");
        }

        private static void NegativeNonGenericCountFailsBeforeEnumeration()
        {
            var source = new NonGenericKnownCountLineSource(-1);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertContains("invalid negative known count", error.Message, "Negative non-generic Count must fail closed.");
            Assert(!source.Enumerated, "Negative non-generic Count must fail before source enumeration starts.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiKnownCountLineSource(1, 2, 2);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertContains("conflicting known counts", error.Message, "Conflicting known Counts must fail closed.");
            Assert(!source.Enumerated, "Conflicting known Counts must fail before source enumeration starts.");
        }

        private static void OversizeKnownCountPreservesBoundPrecedence()
        {
            var source = new MultiKnownCountLineSource(-1, MaxLines + 1, 2);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertBoundError(error);
            Assert(!source.Enumerated, "Any oversize known Count must preserve bounded-error precedence before enumeration.");
        }

        private static void ExactLimitIsAccepted()
        {
            var source = new LineSource(MaxLines);
            var projection = FrozenEstimateProjection.Create(source);

            Assert(projection.Rows.Count == MaxLines, "Frozen estimate projection must preserve exactly 10,000 lines.");
            Assert(source.ObservedCount == MaxLines, "Exact-limit projection must consume exactly 10,000 source lines.");
        }

        private static void FirstOverLimitLineFailsWithoutOverrun()
        {
            var source = new LineSource(MaxLines + 2);
            var error = Capture<InvalidOperationException>(() => FrozenEstimateProjection.Create(source));

            AssertBoundError(error);
            Assert(
                source.ObservedCount == MaxLines + 1,
                "Frozen estimate projection must stop after observing the 10,001st source line.");
        }

        private static void AssertBoundError(InvalidOperationException error)
        {
            Assert(
                string.Equals(
                    error.Message,
                    "Frozen estimate projection supports at most 10000 estimate lines.",
                    StringComparison.Ordinal),
                "Frozen estimate projection must preserve the bounded-line failure contract.");
        }

        private static void AssertContains(string expected, string actual, string message)
        {
            Assert(
                actual != null && actual.IndexOf(expected, StringComparison.Ordinal) >= 0,
                message + " Actual=" + (actual ?? "<null>") + ".");
        }

        private sealed class KnownCountLineSource : IReadOnlyCollection<EstimateLine>
        {
            internal bool Enumerated { get; private set; }

            public int Count => MaxLines + 1;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GenericKnownCountLineSource : ICollection<EstimateLine>
        {
            private readonly int _count;

            internal GenericKnownCountLineSource(int count) { _count = count; }
            internal bool Enumerated { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Generic counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(EstimateLine item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(EstimateLine item) => false;
            public void CopyTo(EstimateLine[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(EstimateLine item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyKnownCountLineSource : IReadOnlyCollection<EstimateLine>
        {
            private readonly int _count;

            internal ReadOnlyKnownCountLineSource(int count) { _count = count; }
            internal bool Enumerated { get; private set; }
            public int Count => _count;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Read-only counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericKnownCountLineSource : IEnumerable<EstimateLine>, ICollection
        {
            private readonly int _count;

            internal NonGenericKnownCountLineSource(int count) { _count = count; }
            internal bool Enumerated { get; private set; }
            public int Count => _count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Non-generic counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class MultiKnownCountLineSource : ICollection<EstimateLine>, IReadOnlyCollection<EstimateLine>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal MultiKnownCountLineSource(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            internal bool Enumerated { get; private set; }
            int ICollection<EstimateLine>.Count => _genericCount;
            int IReadOnlyCollection<EstimateLine>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<EstimateLine>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            IEnumerator<EstimateLine> IEnumerable<EstimateLine>.GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Multi-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<EstimateLine>)this).GetEnumerator();
            void ICollection<EstimateLine>.Add(EstimateLine item) => throw new NotSupportedException();
            void ICollection<EstimateLine>.Clear() => throw new NotSupportedException();
            bool ICollection<EstimateLine>.Contains(EstimateLine item) => false;
            void ICollection<EstimateLine>.CopyTo(EstimateLine[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<EstimateLine>.Remove(EstimateLine item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class LineSource : IEnumerable<EstimateLine>
        {
            private readonly int _count;

            internal LineSource(int count)
            {
                _count = count;
            }

            internal int ObservedCount { get; private set; }

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                var code = new CostCode("COST-001");
                var snapshot = Snapshot();
                var rateBook = RateBookWith(code);

                for (var i = 0; i < _count; i++)
                {
                    ObservedCount++;
                    yield return EstimateLine.Create(
                        "frozen-line-" + i.ToString("D5"),
                        snapshot,
                        SemanticIdentity,
                        SourceIdentity,
                        QuantityKey,
                        rateBook,
                        code,
                        Currency,
                        AsOfUtc);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static MeasurementSnapshot Snapshot()
        {
            var trace = new MeasurementTrace(
                SemanticIdentity,
                SourceIdentity,
                QuantityKey,
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                Unit,
                "none");
            return new MeasurementSnapshot(new[] { trace });
        }

        private static RateBook RateBookWith(CostCode code)
        {
            var item = new RateItem(
                "rate-frozen-estimate",
                code,
                Unit,
                Currency,
                1m,
                EffectiveUtc,
                "v1");
            return new RateBook("book-frozen-estimate", new[] { item });
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
