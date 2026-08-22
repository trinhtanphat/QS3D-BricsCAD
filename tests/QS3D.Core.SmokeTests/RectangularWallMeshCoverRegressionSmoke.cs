using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularWallMeshCoverRegressionSmoke
    {
        public static void Run()
        {
            NearOnlyThinWallIsRejected();
            FarOnlyThinWallIsRejected();
            ValidSingleFaceStillPlans();
            ValidTwoFaceStillPlans();
        }

        private static void NearOnlyThinWallIsRejected()
        {
            var error = Throws<InvalidOperationException>(() =>
                RectangularWallMeshPlanner.Plan(CreateInput(0.10d, includeNear: true, includeFar: false)));
            if (error.Message.IndexOf("cover envelope", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Near-only thin wall must fail on the full cover envelope guard.");
        }

        private static void FarOnlyThinWallIsRejected()
        {
            var error = Throws<InvalidOperationException>(() =>
                RectangularWallMeshPlanner.Plan(CreateInput(0.10d, includeNear: false, includeFar: true)));
            if (error.Message.IndexOf("cover envelope", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Far-only thin wall must fail on the full cover envelope guard.");
        }

        private static void ValidSingleFaceStillPlans()
        {
            var layout = RectangularWallMeshPlanner.Plan(CreateInput(0.20d, includeNear: true, includeFar: false));
            if (layout.Count <= 0) throw new InvalidOperationException("Valid near-only wall mesh produced no bars.");
            if (layout.Bars.Any(bar => bar.Face != WallMeshFace.Near))
                throw new InvalidOperationException("Near-only wall mesh emitted a non-near bar.");
        }

        private static void ValidTwoFaceStillPlans()
        {
            var layout = RectangularWallMeshPlanner.Plan(CreateInput(0.20d, includeNear: true, includeFar: true));
            if (!layout.Bars.Any(bar => bar.Face == WallMeshFace.Near) ||
                !layout.Bars.Any(bar => bar.Face == WallMeshFace.Far))
                throw new InvalidOperationException("Valid two-face wall mesh must retain both faces.");
        }

        private static RectangularWallMeshInput CreateInput(double thicknessM, bool includeNear, bool includeFar)
        {
            return new RectangularWallMeshInput
            {
                LengthM = 2d,
                HeightM = 2d,
                ThicknessM = thicknessM,
                CoverM = 0.04d,
                HorizontalDiameterMm = 20d,
                VerticalDiameterMm = 20d,
                HorizontalCount = 2,
                VerticalCount = 2,
                IncludeNear = includeNear,
                IncludeFar = includeFar,
                HorizontalClosestToFace = true
            };
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class RectangularWallMeshCoverSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RectangularWallMeshCoverRegressionSmoke.Run();
        }
    }
}
