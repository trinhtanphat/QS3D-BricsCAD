using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateRevisionCostImpactUnderflowSmoke
    {
        private const string SemanticIdentity = "cost-impact";
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
            QuantityDrivenUnderflowFailsClosed();
            RateDrivenUnderflowFailsClosed();
            OrdinaryDecompositionRemainsStable();
        }

        private static void QuantityDrivenUnderflowFailsClosed()
        {
            var code = new CostCode("COST-001");
            var snapshot = Snapshot(1d);
            var rateBook = RateBookWith("quantity", code, 0.1m);
            var previous = Line("revision-line", snapshot, rateBook, code);
            var current = Line(
                "revision-line",
                snapshot,
                rateBook,
                code,
                0.0000000000000000000000000001m,
                "tiny commercial adjustment");

            Capture<OverflowException>(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static void RateDrivenUnderflowFailsClosed()
        {
            var code = new CostCode("COST-001");
            var snapshot = Snapshot(0.1d);
            var previous = Line("revision-line", snapshot, RateBookWith("rate-before", code, 1m), code);
            var current = Line(
                "revision-line",
                snapshot,
                RateBookWith("rate-after", code, 1.0000000000000000000000000001m),
                code);

            Capture<OverflowException>(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static void OrdinaryDecompositionRemainsStable()
        {
            var code = new CostCode("COST-001");
            var previous = Line("revision-line", Snapshot(2d), RateBookWith("ordinary-before", code, 3m), code);
            var current = Line("revision-line", Snapshot(5d), RateBookWith("ordinary-after", code, 4m), code);

            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Assert(impact.MeasuredQuantityDelta == 3m, "Ordinary measured quantity delta changed unexpectedly.");
            Assert(impact.EstimatingQuantityDelta == 3m, "Ordinary estimating quantity delta changed unexpectedly.");
            Assert(impact.UnitRateDelta == 1m, "Ordinary unit-rate delta changed unexpectedly.");
            Assert(impact.QuantityDrivenCostDelta == 9m, "Ordinary quantity-driven cost delta changed unexpectedly.");
            Assert(impact.RateDrivenCostDelta == 5m, "Ordinary rate-driven cost delta changed unexpectedly.");
            Assert(impact.RateEffectAtCurrentQuantity == 5m, "Ordinary current-quantity rate effect changed unexpectedly.");
            Assert(impact.RateEffectRoundingResidual == 0m, "Ordinary rate-effect residual changed unexpectedly.");
            Assert(impact.CostDelta == 14m, "Ordinary total cost delta changed unexpectedly.");
        }

        private static MeasurementSnapshot Snapshot(double quantity)
        {
            var trace = new MeasurementTrace(
                SemanticIdentity,
                SourceIdentity,
                QuantityKey,
                Array.Empty<MeasurementTraceFact>(),
                quantity,
                Array.Empty<MeasurementTraceAdjustment>(),
                quantity,
                Unit,
                "none");
            return new MeasurementSnapshot(new[] { trace });
        }

        private static RateBook RateBookWith(string identity, CostCode code, decimal unitRate)
        {
            var item = new RateItem(
                "rate-" + identity,
                code,
                Unit,
                Currency,
                unitRate,
                EffectiveUtc,
                "v1");
            return new RateBook("book-" + identity, new[] { item });
        }

        private static EstimateLine Line(
            string estimateLineId,
            MeasurementSnapshot snapshot,
            RateBook rateBook,
            CostCode code,
            decimal commercialAdjustmentQuantity = 0m,
            string? commercialAdjustmentReason = null) =>
            EstimateLine.Create(
                estimateLineId,
                snapshot,
                SemanticIdentity,
                SourceIdentity,
                QuantityKey,
                rateBook,
                code,
                Currency,
                AsOfUtc,
                commercialAdjustmentQuantity,
                commercialAdjustmentReason);

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
