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
            TotalWeightUnderflowFailsClosed();
        }

        private static void OrdinaryQuantityRemainsStable()
        {
            var quantity = ColumnTieQuantityCalculator.Calculate(Layout(4d), 16d);
            Near(4d, quantity.CuttingLengthPerTieM);
            Near(4d, quantity.TotalLengthM);
            Near(256d / 162d, quantity.KgPerMeter);
            Near(4d * 256d / 162d, quantity.TotalWeightKg);
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
