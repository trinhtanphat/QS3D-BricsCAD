using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ShapeRebarDistributionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        public static void Run()
        {
            CenterlineDistributionIsSymmetric();
            ExtentsDistributionRespectsClearCover();
            SingleBarUsesHostCenter();
            ImpossibleEnvelopeIsRejected();
        }

        private static void CenterlineDistributionIsSymmetric()
        {
            var result = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 0.2d, Cover = 0.02d, Radius = 0.01d, Count = 3, Centered = true
            });
            Near(0.03d, result.CenterClearance);
            Near(-0.07d, result.Offsets[0]);
            Near(0d, result.Offsets[1]);
            Near(0.07d, result.Offsets[2]);
        }

        private static void ExtentsDistributionRespectsClearCover()
        {
            var result = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 0.2d, Cover = 0.02d, Radius = 0.01d, Count = 2, Centered = false
            });
            Near(0.03d, result.Offsets[0]);
            Near(0.17d, result.Offsets[1]);
        }

        private static void SingleBarUsesHostCenter()
        {
            var edge = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 0.2d, Cover = 0.02d, Radius = 0.01d, Count = 1, Centered = false
            });
            var centerline = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 0.2d, Cover = 0.02d, Radius = 0.01d, Count = 1, Centered = true
            });
            Near(0.1d, edge.Offsets[0]);
            Near(0d, centerline.Offsets[0]);
        }

        private static void ImpossibleEnvelopeIsRejected()
        {
            Throws<InvalidOperationException>(() => ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 0.05d, Cover = 0.02d, Radius = 0.01d, Count = 2, Centered = false
            }));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
