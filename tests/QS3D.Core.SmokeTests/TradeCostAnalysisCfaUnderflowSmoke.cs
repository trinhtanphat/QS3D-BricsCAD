using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TradeCostAnalysisCfaUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PositiveCostDensityUnderflowFailsClosed();
            ZeroCostDensityRemainsZero();
            ZeroCfaRemainsNull();
            OrdinaryCostDensityRemainsStable();
        }

        private static void PositiveCostDensityUnderflowFailsClosed()
        {
            var service = new TradeCostAnalysisService();
            Capture<OverflowException>(() => service.Analyze(
                new[] { new TradeCostItem("ITEM-UNDERFLOW", "Concrete", 1m) },
                decimal.MaxValue));
        }

        private static void ZeroCostDensityRemainsZero()
        {
            var row = SingleRow(
                new TradeCostItem("ITEM-ZERO", "Concrete", 0m),
                decimal.MaxValue);
            Assert(row.CostPerCfaM2 == 0m, "Zero trade cost must remain zero cost-per-CFA.");
        }

        private static void ZeroCfaRemainsNull()
        {
            var row = SingleRow(
                new TradeCostItem("ITEM-NO-CFA", "Concrete", 10m),
                0m);
            Assert(!row.CostPerCfaM2.HasValue, "Zero CFA must continue to report no cost-per-CFA value.");
        }

        private static void OrdinaryCostDensityRemainsStable()
        {
            var row = SingleRow(
                new TradeCostItem("ITEM-ORDINARY", "Concrete", 100m),
                25m);
            Assert(row.CostPerCfaM2 == 4m, "Ordinary trade cost-per-CFA arithmetic changed unexpectedly.");
        }

        private static TradeCostAnalysisRow SingleRow(TradeCostItem item, decimal cfaM2)
        {
            var rows = new TradeCostAnalysisService().Analyze(new[] { item }, cfaM2);
            Assert(rows.Count == 1, "Trade analysis regression expected exactly one row.");
            return rows[0];
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
