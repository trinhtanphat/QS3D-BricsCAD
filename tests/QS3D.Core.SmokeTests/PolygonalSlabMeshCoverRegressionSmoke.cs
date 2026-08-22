using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonalSlabMeshCoverRegressionSmoke
    {
        public static void Run()
        {
            BottomOnlyThinSlabIsRejected();
            TopOnlyThinSlabIsRejected();
            ValidSingleFaceStillPlans();
            ValidDualFaceStillPlans();
        }

        private static void BottomOnlyThinSlabIsRejected()
        {
            var error = Throws<InvalidOperationException>(() =>
                PolygonalSlabMeshPlanner.Plan(CreateInput(0.10d, includeBottom: true, includeTop: false)));
            if (error.Message.IndexOf("cover envelope", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Bottom-only thin slab must fail on the full cover envelope guard.");
        }

        private static void TopOnlyThinSlabIsRejected()
        {
            var error = Throws<InvalidOperationException>(() =>
                PolygonalSlabMeshPlanner.Plan(CreateInput(0.10d, includeBottom: false, includeTop: true)));
            if (error.Message.IndexOf("cover envelope", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Top-only thin slab must fail on the full cover envelope guard.");
        }

        private static void ValidSingleFaceStillPlans()
        {
            var layout = PolygonalSlabMeshPlanner.Plan(CreateInput(0.20d, includeBottom: true, includeTop: false));
            if (layout.Count <= 0) throw new InvalidOperationException("Valid bottom-only polygonal slab mesh produced no bars.");
            if (layout.Bars.Any(bar => bar.Face != SlabMeshFace.Bottom))
                throw new InvalidOperationException("Bottom-only polygonal slab mesh emitted a non-bottom bar.");
        }

        private static void ValidDualFaceStillPlans()
        {
            var layout = PolygonalSlabMeshPlanner.Plan(CreateInput(0.20d, includeBottom: true, includeTop: true));
            if (!layout.Bars.Any(bar => bar.Face == SlabMeshFace.Bottom) ||
                !layout.Bars.Any(bar => bar.Face == SlabMeshFace.Top))
                throw new InvalidOperationException("Valid dual-face polygonal slab mesh must retain both faces.");
        }

        private static PolygonalSlabMeshInput CreateInput(double thicknessM, bool includeBottom, bool includeTop)
        {
            return new PolygonalSlabMeshInput
            {
                FootprintM = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(2d, 0d),
                    new Point2(2d, 2d),
                    new Point2(0d, 2d)
                },
                ThicknessM = thicknessM,
                CoverM = 0.04d,
                XDiameterMm = 20d,
                YDiameterMm = 20d,
                XCount = 2,
                YCount = 2,
                IncludeBottom = includeBottom,
                IncludeTop = includeTop,
                XClosestToFace = true
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

    internal static class PolygonalSlabMeshCoverSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PolygonalSlabMeshCoverRegressionSmoke.Run();
        }
    }
}
