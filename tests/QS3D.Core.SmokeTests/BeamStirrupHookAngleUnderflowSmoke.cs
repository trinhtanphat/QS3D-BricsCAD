using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupHookAngleUnderflowSmoke
    {
        internal static void Run()
        {
            OrdinaryHookAngleRemainsStable();
            TinyPositiveHookAngleFailsClosed();
        }

        private static void OrdinaryHookAngleRemainsStable()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(Input(90d));

            Assert(layout.Count == 3, "Ordinary beam stirrup station count changed unexpectedly.");
            Assert(layout.HasHookTails, "Ordinary positive hook length must retain hook tails.");
            Assert(layout.SectionLoop.Count == 8, "Ordinary square stirrup with hook tails must retain the expected path points.");
            Near(3.768d, layout.CenterlineLengthM, "Ordinary beam stirrup centerline length changed unexpectedly.");
            Near(3.768d, layout.PolylineLengthM, "Ordinary beam stirrup polyline length changed unexpectedly.");
        }

        private static void TinyPositiveHookAngleFailsClosed()
        {
            var error = Capture<OverflowException>(() => BeamStirrupLayoutPlanner.Plan(Input(double.Epsilon)));
            Assert(
                error.Message == "Rebar division underflow: beam stirrup hook tail angle radians",
                "Positive beam stirrup hook angle lost during degree-to-radian scaling must fail closed.");
        }

        private static BeamStirrupLayoutInput Input(double hookTailAngleDeg)
        {
            return new BeamStirrupLayoutInput
            {
                LengthM = 1d,
                WidthM = 1d,
                HeightM = 1d,
                SectionCoverM = 0.05d,
                EndCoverM = 0.05d,
                DiameterMm = 8d,
                Count = 3,
                BendRadiusM = 0d,
                MaximumSagittaM = 0.001d,
                HookLengthM = 0.1d,
                HookTailAngleDeg = hookTailAngleDeg
            };
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
