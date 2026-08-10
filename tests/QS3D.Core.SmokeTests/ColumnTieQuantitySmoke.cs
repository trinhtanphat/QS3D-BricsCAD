using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieQuantitySmoke
    {
        public static void Run()
        {
            DeterministicTieWeight();
            HookAllowanceAddsPerTie();
            RejectsInvalidInputs();
        }

        private static void DeterministicTieWeight()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                HeightM = 3d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 150d
            });
            var quantity = ColumnTieQuantityCalculator.Calculate(layout, 8d);
            if (quantity.Count != layout.ElevationsM.Count) throw new Exception("Tie quantity count mismatch.");
            Near(1.448d, quantity.CuttingLengthPerTieM, 1e-12d);
            Near(64d / 162d, quantity.KgPerMeter, 1e-12d);
            Near(quantity.CuttingLengthPerTieM * quantity.Count, quantity.TotalLengthM, 1e-12d);
            Near(quantity.TotalLengthM * quantity.KgPerMeter, quantity.TotalWeightKg, 1e-12d);
        }

        private static void HookAllowanceAddsPerTie()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d,
                DepthM = 0.3d,
                HeightM = 1d,
                CoverM = 0.03d,
                DiameterMm = 8d,
                SpacingMm = 200d
            });
            var withoutHook = ColumnTieQuantityCalculator.Calculate(layout, 8d, 0d);
            var withHook = ColumnTieQuantityCalculator.Calculate(layout, 8d, 0.12d);
            Near(0.12d, withHook.CuttingLengthPerTieM - withoutHook.CuttingLengthPerTieM, 1e-12d);
            Near(0.12d * layout.ElevationsM.Count, withHook.TotalLengthM - withoutHook.TotalLengthM, 1e-12d);
        }

        private static void RejectsInvalidInputs()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d, DepthM = 0.3d, HeightM = 1d,
                CoverM = 0.03d, DiameterMm = 8d, SpacingMm = 200d
            });
            Throws<ArgumentOutOfRangeException>(() => ColumnTieQuantityCalculator.Calculate(layout, 0d));
            Throws<ArgumentOutOfRangeException>(() => ColumnTieQuantityCalculator.Calculate(layout, 8d, -0.01d));
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
