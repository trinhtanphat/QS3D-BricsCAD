using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarOverlapRegressionSmoke
    {
        public static void Run()
        {
            WidthDirectionOverlapIsRejected();
            DepthDirectionOverlapIsRejected();
            NormalLayoutStillSucceeds();
            TangentSpacingBoundaryStillSucceeds();
        }

        private static void WidthDirectionOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.20d,
                DepthM = 0.30d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 7,
                BarsAlongDepth = 3
            }));
        }

        private static void DepthDirectionOverlapIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.30d,
                DepthM = 0.20d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 3,
                BarsAlongDepth = 7
            }));
        }

        private static void NormalLayoutStillSucceeds()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.40d,
                DepthM = 0.30d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 4,
                BarsAlongDepth = 3
            });

            if (layout.BarCenters.Count != 10)
                throw new InvalidOperationException("Valid rectangular column rebar layout changed its perimeter bar count.");
        }

        private static void TangentSpacingBoundaryStillSucceeds()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.22d,
                DepthM = 0.18d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 7,
                BarsAlongDepth = 5
            });

            if (layout.BarCenters.Count != 20)
                throw new InvalidOperationException("One-diameter tangent rectangular rebar spacing should remain supported.");
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

    internal static class RectangularRebarOverlapSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RectangularRebarOverlapRegressionSmoke.Run();
        }
    }
}
