using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupAxialOverlapRegressionSmoke
    {
        public static void Run()
        {
            SpacingDrivenOverlapIsRejected();
            CountDrivenOverlapIsRejected();
            NormalSpacingStillSucceeds();
            TangentSpacingBoundaryStillSucceeds();
        }

        private static void SpacingDrivenOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(BaseInput(0.30d, 4d, null)));
        }

        private static void CountDrivenOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(BaseInput(0.20d, null, 20)));
        }

        private static void NormalSpacingStillSucceeds()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(BaseInput(0.30d, 100d, null));
            if (layout.Count < 2 || layout.ActualSpacingM < 0.008d - 1e-12d)
                throw new InvalidOperationException("Normal beam stirrup spacing no longer produces a non-overlapping station layout.");
        }

        private static void TangentSpacingBoundaryStillSucceeds()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(BaseInput(0.168d, 8d, null));
            if (layout.Count != 11 || Math.Abs(layout.ActualSpacingM - 0.008d) > 1e-12d)
                throw new InvalidOperationException("Exact one-diameter beam stirrup station spacing should remain supported.");
        }

        private static BeamStirrupLayoutInput BaseInput(double lengthM, double? spacingMm, int? count)
        {
            return new BeamStirrupLayoutInput
            {
                LengthM = lengthM,
                WidthM = 0.30d,
                HeightM = 0.50d,
                SectionCoverM = 0.025d,
                EndCoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = spacingMm,
                Count = count,
                BendRadiusM = 0d,
                MaximumSagittaM = 0.001d,
                HookLengthM = 0d,
                HookTailAngleDeg = 0d
            };
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class BeamStirrupAxialOverlapSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BeamStirrupAxialOverlapRegressionSmoke.Run();
        }
    }
}
