using System;
using System.Linq;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCuttingOptimizerSmoke
    {
        public static void Run()
        {
            BestFitDecreasingUsesDeterministicTwoBarPlan();
            ExactStockEndAvoidsFinalCut();
            TailRequiresFinalCutAndPreservesOffCut();
            RequirementOrderDoesNotChangePlan();
            OversizedExpandedDemandFailsBeforePlanning();
            PieceThatCannotFitFailsClosed();
            SubToleranceStockOverrunFailsClosed();
        }

        private static void BestFitDecreasingUsesDeterministicTwoBarPlan()
        {
            var demand = Demand(
                10d,
                0d,
                new RebarCutRequirement("A", 6d, 2),
                new RebarCutRequirement("B", 4d, 2));

            var result = RebarCuttingOptimizer.Plan(demand);
            Equal("BestFitDecreasingV1", RebarCuttingOptimizationResult.AlgorithmId);
            Equal(2, result.StockBars.Count);
            Equal(2, result.ProcurementQuantities.StockBarCount);
            Near(20d, result.ProcurementQuantities.ProcurementLengthM);
            Near(0d, result.ProcurementQuantities.KerfLengthM);
            Near(0d, result.ProcurementQuantities.OffCutLengthM);
            Equal("A#1|B#1", BarSignature(result.StockBars[0]));
            Equal("A#2|B#2", BarSignature(result.StockBars[1]));
            AssertConservation(result);
        }

        private static void ExactStockEndAvoidsFinalCut()
        {
            var demand = Demand(
                12d,
                0.01d,
                new RebarCutRequirement("LONG", 6d, 1),
                new RebarCutRequirement("SHORT", 5.99d, 1));

            var result = RebarCuttingOptimizer.Plan(demand);
            Equal(1, result.StockBars.Count);
            Equal(1, result.StockBars[0].CutOperationCount);
            Near(0.01d, result.StockBars[0].KerfLengthM);
            Near(0d, result.StockBars[0].OffCutLengthM);
            AssertConservation(result);
        }

        private static void TailRequiresFinalCutAndPreservesOffCut()
        {
            var demand = Demand(
                12d,
                0.01d,
                new RebarCutRequirement("LONG", 6d, 1),
                new RebarCutRequirement("SHORT", 5.5d, 1));

            var result = RebarCuttingOptimizer.Plan(demand);
            Equal(1, result.StockBars.Count);
            Equal(2, result.StockBars[0].CutOperationCount);
            Near(0.02d, result.StockBars[0].KerfLengthM);
            Near(0.48d, result.StockBars[0].OffCutLengthM);
            AssertConservation(result);
        }

        private static void RequirementOrderDoesNotChangePlan()
        {
            var first = RebarCuttingOptimizer.Plan(Demand(
                10d,
                0d,
                new RebarCutRequirement("B", 4d, 2),
                new RebarCutRequirement("A", 6d, 2)));
            var second = RebarCuttingOptimizer.Plan(Demand(
                10d,
                0d,
                new RebarCutRequirement("A", 6d, 2),
                new RebarCutRequirement("B", 4d, 2)));

            Equal(PlanSignature(first), PlanSignature(second));
        }

        private static void OversizedExpandedDemandFailsBeforePlanning()
        {
            var demand = Demand(12d, 0d, new RebarCutRequirement("A", 1d, 10001));
            Throws<ArgumentOutOfRangeException>(() => RebarCuttingOptimizer.Plan(demand));
        }

        private static void PieceThatCannotFitFailsClosed()
        {
            var demand = new RebarStockDemand(
                "G",
                "CB400-V",
                16d,
                10d,
                new[] { new RebarCutRequirement("A", 10d, 1) },
                new RebarCutAllowancePolicy(0d, 0.01d));
            Throws<InvalidOperationException>(() => RebarCuttingOptimizer.Plan(demand));
        }

        private static void SubToleranceStockOverrunFailsClosed()
        {
            var demand = Demand(10d, 0d, new RebarCutRequirement("A", 10d + 5e-13d, 1));
            Throws<InvalidOperationException>(() => RebarCuttingOptimizer.Plan(demand));
        }

        private static RebarStockDemand Demand(double stockLengthM, double kerfM, params RebarCutRequirement[] requirements)
        {
            return new RebarStockDemand(
                "G",
                "CB400-V",
                16d,
                stockLengthM,
                requirements,
                new RebarCutAllowancePolicy(kerfM, 0d));
        }

        private static void AssertConservation(RebarCuttingOptimizationResult result)
        {
            var accounted = result.Demand.DemandLengthBeforeKerfM
                + result.ProcurementQuantities.KerfLengthM
                + result.ProcurementQuantities.OffCutLengthM;
            Near(result.ProcurementQuantities.ProcurementLengthM, accounted);
        }

        private static string PlanSignature(RebarCuttingOptimizationResult result)
        {
            return string.Join(";", result.StockBars.Select(BarSignature));
        }

        private static string BarSignature(RebarStockCutPlan bar)
        {
            return string.Join("|", bar.Cuts.Select(cut => cut.CutId + "#" + cut.InstanceIndex));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-10d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
