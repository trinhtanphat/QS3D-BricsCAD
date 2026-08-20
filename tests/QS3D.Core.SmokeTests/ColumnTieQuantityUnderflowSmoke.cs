using System;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieQuantityUnderflowSmoke
    {
        internal static void Run()
        {
            OrdinaryQuantityRemainsStable();
            SwallowedHookAllowanceFailsClosed();
            SwallowedPerimeterContributionFailsClosed();
            TotalWeightUnderflowFailsClosed();
        }

        private static void OrdinaryQuantityRemainsStable()
        {
            var quantity = ColumnTieQuantityCalculator.Calculate(Layout(4d), 16d, 0.25d);
            Near(4.25d, quantity.CuttingLengthPerTieM);
            Near(4.25d, quantity.TotalLengthM);
            Near(256d / 162d, quantity.KgPerMeter);
            Near(4.25d * 256d / 162d, quantity.TotalWeightKg);
        }

        private static void SwallowedHookAllowanceFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                ColumnTieQuantityCalculator.Calculate(Layout(9007199254740992d), 16d, 1d));
            Equal("tie cutting length lost a positive contribution at the current coordinate scale.", error.Message);
        }

        private static void SwallowedPerimeterContributionFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                ColumnTieQuantityCalculator.Calculate(Layout(1d), 16d, 9007199254740992d));
            Equal("tie cutting length lost a positive contribution at the current coordinate scale.", error.Message);
        }

        private static void TotalWeightUnderflowFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                ColumnTieQuantityCalculator.Calculate(Layout(0.01d), 1e-160d));
            Equal("tie total weight underflowed.", error.Message);
        }

        private static ColumnTieLayout Layout(double pathPerimeterM) =>
            new ColumnTieLayout(Array.Empty<Point2>(), new[] { 0d }, 0d, pathPerimeterM);

        private static T Capture<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Near(double expected, double actual)
        {
            var tolerance = Math.Max(1e-12d, Math.Abs(expected) * 1e-12d);
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}