using System;
using System.Globalization;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapeTurnAngleUnderflowSmoke
    {
        internal static void Run()
        {
            ZeroTurnRemainsStable();
            OrdinaryPositiveTurnRemainsStable();
            OrdinaryNegativeTurnRemainsStable();
            TinyPositiveTurnFailsClosed();
            TinyNegativeTurnFailsClosed();
        }

        private static void ZeroTurnRemainsStable()
        {
            var path = RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", "0");
            Assert(path.Points.Count == 3, "Zero-turn custom rebar shape point count changed unexpectedly.");
            Near(2d, path.Points[2].X, "Zero-turn custom rebar shape X changed unexpectedly.");
            Near(0d, path.Points[2].Y, "Zero-turn custom rebar shape Y changed unexpectedly.");
        }

        private static void OrdinaryPositiveTurnRemainsStable()
        {
            var path = RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", "90");
            Assert(path.Points.Count == 3, "Positive-turn custom rebar shape point count changed unexpectedly.");
            Near(1d, path.Points[2].X, "Positive-turn custom rebar shape X changed unexpectedly.");
            Near(1d, path.Points[2].Y, "Positive-turn custom rebar shape Y changed unexpectedly.");
        }

        private static void OrdinaryNegativeTurnRemainsStable()
        {
            var path = RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", "-90");
            Assert(path.Points.Count == 3, "Negative-turn custom rebar shape point count changed unexpectedly.");
            Near(1d, path.Points[2].X, "Negative-turn custom rebar shape X changed unexpectedly.");
            Near(-1d, path.Points[2].Y, "Negative-turn custom rebar shape Y changed unexpectedly.");
        }

        private static void TinyPositiveTurnFailsClosed()
        {
            var turn = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            var error = Capture<OverflowException>(() => RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", turn));
            Assert(
                error.Message == "Rebar shape nonzero turn angle underflowed to zero radians.",
                "Positive custom rebar turn-angle underflow must fail closed.");
        }

        private static void TinyNegativeTurnFailsClosed()
        {
            var turn = (-double.Epsilon).ToString("R", CultureInfo.InvariantCulture);
            var error = Capture<OverflowException>(() => RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", turn));
            Assert(
                error.Message == "Rebar shape nonzero turn angle underflowed to zero radians.",
                "Negative custom rebar turn-angle underflow must fail closed.");
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

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12d) throw new InvalidOperationException(message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
