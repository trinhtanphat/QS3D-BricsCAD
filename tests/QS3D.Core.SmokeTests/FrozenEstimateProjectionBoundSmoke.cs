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
            ExactLimitIsAccepted();
            FirstOverLimitLineFailsWithoutOverrun();
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

            Assert(
                string.Equals(
                    error.Message,
                    "Frozen estimate projection supports at most 10000 estimate lines.",
                    StringComparison.Ordinal),
                "Frozen estimate projection must preserve the bounded-line failure contract.");
            Assert(
                source.ObservedCount == MaxLines + 1,
                "Frozen estimate projection must stop after observing the 10,001st source line.");
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
