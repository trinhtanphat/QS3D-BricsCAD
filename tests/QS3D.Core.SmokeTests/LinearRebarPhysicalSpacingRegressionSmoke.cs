using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class LinearRebarPhysicalSpacingRegressionSmoke
    {
        public static void Run()
        {
            CountDrivenOverlapIsRejected();
            SpacingDrivenOverlapIsRejected();
            NormalSpacingStillSucceeds();
            TangentSpacingBoundaryStillSucceeds();
            SingletonStillSucceeds();
        }

        private static void CountDrivenOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.20d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                Count = 7
            }));
        }

        private static void SpacingDrivenOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.30d,
                CoverM = 0.04d,
                DiameterMm = 16d,
                SpacingMm = 5d
            }));
        }

        private static void NormalSpacingStillSucceeds()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d,
                CoverM = 0.05d,
                DiameterMm = 10d,
                SpacingMm = 200d
            });

            if (layout.Count != 6 || layout.ActualSpacingM < 0.01d - 1e-12d)
                throw new InvalidOperationException("Normal linear rebar spacing no longer produces a non-overlapping layout.");
        }

        private static void TangentSpacingBoundaryStillSucceeds()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.20d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                Count = 6
            });

            if (layout.Count != 6 || Math.Abs(layout.ActualSpacingM - 0.02d) > 1e-12d)
                throw new InvalidOperationException("Exact one-diameter linear rebar spacing should remain supported.");
        }

        private static void SingletonStillSucceeds()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.50d,
                CoverM = 0.04d,
                DiameterMm = 16d,
                Count = 1
            });

            if (layout.Count != 1 || Math.Abs(layout.ActualSpacingM) > 1e-12d || Math.Abs(layout.OffsetsM[0]) > 1e-12d)
                throw new InvalidOperationException("Single-bar linear layouts must remain centered with zero spacing.");
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

    internal static class LinearRebarPhysicalSpacingSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LinearRebarPhysicalSpacingRegressionSmoke.Run();
        }
    }
}
