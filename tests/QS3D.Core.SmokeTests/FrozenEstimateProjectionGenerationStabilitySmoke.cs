using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionGenerationStabilitySmoke
    {
        private static readonly DateTime RateUtc = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountReplacementIsRejected();
            SameCountReorderIsRejected();
            StableCountedGenerationRemainsAccepted();
            StreamingInputRemainsSinglePassCompatible();
            Console.WriteLine("PASS frozen estimate projection generation stability");
        }

        private static void SameCountReplacementIsRejected()
        {
            var a = Line("LINE-A", "SEM-A", 1d, 10m);
            var b = Line("LINE-B", "SEM-B", 2d, 20m);
            var c = Line("LINE-C", "SEM-C", 3d, 30m);
            var source = new SameCountDriftCollection<EstimateLine>(
                new[] { a, b },
                new[] { a, c });

            ExpectContentDrift(source, "same-count frozen estimate replacement");
        }

        private static void SameCountReorderIsRejected()
        {
            var a = Line("LINE-D", "SEM-D", 4d, 40m);
            var b = Line("LINE-E", "SEM-E", 5d, 50m);
            var source = new SameCountDriftCollection<EstimateLine>(
                new[] { a, b },
                new[] { b, a });

            ExpectContentDrift(source, "same-count frozen estimate reorder");
        }

        private static void StableCountedGenerationRemainsAccepted()
        {
            var a = Line("LINE-F", "SEM-F", 6d, 60m);
            var b = Line("LINE-G", "SEM-G", 7d, 70m);
            var source = new SameCountDriftCollection<EstimateLine>(
                new[] { a, b },
                new[] { a, b });

            var projection = FrozenEstimateProjection.Create(source);
            Require(projection.Rows.Count == 2, "stable counted frozen estimate generation changed cardinality");
            Require(projection.Rows[0].EstimateLineId == "LINE-F" && projection.Rows[1].EstimateLineId == "LINE-G",
                "stable counted frozen estimate generation changed ordering");
        }

        private static void StreamingInputRemainsSinglePassCompatible()
        {
            var source = new SinglePassEnumerable<EstimateLine>(Line("LINE-H", "SEM-H", 8d, 80m));
            var projection = FrozenEstimateProjection.Create(source);
            Require(source.GetEnumeratorCalls == 1, "streaming frozen estimate input was replayed unexpectedly");
            Require(projection.Rows.Count == 1 && projection.Rows[0].EstimateLineId == "LINE-H",
                "streaming frozen estimate projection changed");
        }

        private static void ExpectContentDrift(IEnumerable<EstimateLine> source, string label)
        {
            try
            {
                FrozenEstimateProjection.Create(source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("content changed during enumeration", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(label + " failed for the wrong reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static EstimateLine Line(string lineId, string semanticIdentity, double quantity, decimal unitRate)
        {
            var sourceIdentity = "SRC-" + lineId;
            var quantityKey = "QTY";
            var unit = "m2";
            var trace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                quantity,
                Array.Empty<MeasurementTraceAdjustment>(),
                quantity,
                unit,
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var costCode = new CostCode("COST-" + lineId);
            var rateItem = new RateItem(
                "RATE-" + lineId,
                costCode,
                unit,
                "VND",
                unitRate,
                RateUtc,
                "R1");
            var rateBook = new RateBook("BOOK-" + lineId, new[] { rateItem });
            return EstimateLine.Create(
                lineId,
                snapshot,
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                rateBook,
                costCode,
                "VND",
                RateUtc);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class SameCountDriftCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _first;
            private readonly T[] _second;
            private int _enumerations;

            internal SameCountDriftCollection(T[] first, T[] second)
            {
                if (first == null) throw new ArgumentNullException(nameof(first));
                if (second == null) throw new ArgumentNullException(nameof(second));
                if (first.Length != second.Length)
                    throw new ArgumentException("Drift generations must preserve Count.");
                _first = first;
                _second = second;
            }

            public int Count => _first.Length;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                var generation = _enumerations++ == 0 ? _first : _second;
                return ((IEnumerable<T>)generation).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => ((ICollection<T>)_first).Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _first.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T _value;

            internal SinglePassEnumerable(T value)
            {
                _value = value;
            }

            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls != 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                yield return _value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
