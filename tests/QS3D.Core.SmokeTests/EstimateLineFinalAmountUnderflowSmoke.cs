using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineFinalAmountUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PositiveProductUnderflowFailsClosed();
            OrdinaryAmountRemainsStable();
            ZeroQuantityRemainsZero();
            ZeroUnitRateRemainsZero();
        }

        private static void PositiveProductUnderflowFailsClosed()
        {
            var error = Capture<OverflowException>(() => CreateLine(1e-28d, 0.1m));
            Assert(
                error.Message == "Estimate line final amount underflowed decimal arithmetic.",
                "EstimateLine must report final-amount decimal underflow explicitly.");
        }

        private static void OrdinaryAmountRemainsStable()
        {
            var line = CreateLine(2.5d, 10m);
            Assert(line.EstimatingQuantity == 2.5m, "Ordinary estimating quantity changed unexpectedly.");
            Assert(line.FinalAmount == 25m, "Ordinary final amount changed unexpectedly.");
        }

        private static void ZeroQuantityRemainsZero()
        {
            var line = CreateLine(0d, 10m);
            Assert(line.FinalAmount == 0m, "A legitimate zero estimating quantity must keep a zero final amount.");
        }

        private static void ZeroUnitRateRemainsZero()
        {
            var line = CreateLine(2.5d, 0m);
            Assert(line.FinalAmount == 0m, "A legitimate zero unit rate must keep a zero final amount.");
        }

        private static EstimateLine CreateLine(double quantity, decimal unitRate)
        {
            const string semanticIdentity = "semantic-1";
            const string sourceIdentity = "source-1";
            const string quantityKey = "length";
            var asOfUtc = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            var costCode = new CostCode("COST-1");
            var trace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                quantity,
                Array.Empty<MeasurementTraceAdjustment>(),
                quantity,
                "m",
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var rate = new RateItem(
                "rate-1",
                costCode,
                "m",
                "VND",
                unitRate,
                asOfUtc.AddDays(-1),
                "v1");
            var rateBook = new RateBook("book-1", new[] { rate });

            return EstimateLine.Create(
                "line-1",
                snapshot,
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                rateBook,
                costCode,
                "VND",
                asOfUtc);
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
