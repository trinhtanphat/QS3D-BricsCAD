using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularStirrupSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        public static void Run()
        {
            MiteredClosedTie();
            RoundedTie();
            SymmetricHookTails();
            SpacingDistribution();
            CountDistribution();
            RejectsInvalidEnvelope();
            RejectsExcessiveBend();
            RejectsHookOutsideEnvelope();
            RejectsAmbiguousDistribution();
        }

        private static void MiteredClosedTie()
        {
            var plan = RectangularStirrupPlanner.Plan(new RectangularStirrupInput
            {
                WidthM = .4d,
                DepthM = .6d,
                CoverM = .04d,
                DiameterMm = 8d,
                MaximumSagittaM = .001d
            });
            Near(.312d, plan.CenterlineWidthM, 1e-12d);
            Near(.512d, plan.CenterlineDepthM, 1e-12d);
            Near(1.648d, plan.CenterlineLengthM, 1e-12d);
            Equal(6, plan.Path.Points.Count);
            Near(plan.Path.Points.First().X, plan.Path.Points.Last().X, 1e-12d);
            Near(plan.Path.Points.First().Y, plan.Path.Points.Last().Y, 1e-12d);
            Near(plan.CenterlineLengthM, plan.PolylineLengthM, 1e-12d);
        }

        private static void RoundedTie()
        {
            const double bend = .03d;
            var plan = RectangularStirrupPlanner.Plan(new RectangularStirrupInput
            {
                WidthM = .5d,
                DepthM = .4d,
                CoverM = .025d,
                DiameterMm = 10d,
                BendRadiusM = bend,
                MaximumSagittaM = .0005d
            });
            var expected = 2d * (plan.CenterlineWidthM + plan.CenterlineDepthM) - 8d * bend + 2d * Math.PI * bend;
            Near(expected, plan.CenterlineLengthM, 1e-12d);
            True(plan.Path.Points.Count > 10);
            True(plan.PolylineLengthM <= plan.CenterlineLengthM + 1e-12d);
            True(plan.CenterlineLengthM - plan.PolylineLengthM < .005d);
        }

        private static void SymmetricHookTails()
        {
            var plan = RectangularStirrupPlanner.Plan(new RectangularStirrupInput
            {
                WidthM = .5d,
                DepthM = .5d,
                CoverM = .03d,
                DiameterMm = 8d,
                BendRadiusM = .02d,
                MaximumSagittaM = .001d,
                HookLengthM = .08d,
                HookTailAngleDeg = 45d
            });
            var first = plan.Path.Points.First();
            var last = plan.Path.Points.Last();
            Near(-first.X, last.X, 1e-12d);
            Near(first.Y, last.Y, 1e-12d);
            Near(.16d, plan.CenterlineLengthM - (2d * (plan.CenterlineWidthM + plan.CenterlineDepthM) - .16d + 2d * Math.PI * .02d), 1e-12d);
        }

        private static void SpacingDistribution()
        {
            var set = RectangularStirrupPlanner.PlanSet(new RectangularStirrupSetInput
            {
                Shape = new RectangularStirrupInput { WidthM = .3d, DepthM = .5d, CoverM = .03d, DiameterMm = 8d },
                HostSpanM = 3d,
                EndCoverM = .05d,
                SpacingMm = 150d
            });
            Equal(21, set.Distribution.Count);
            Near(set.Shape.CenterlineLengthM * set.Distribution.Count, set.TotalCenterlineLengthM, 1e-12d);
            Near(-set.Distribution.UsableSpanM / 2d, set.Distribution.OffsetsM.First(), 1e-12d);
            Near(set.Distribution.UsableSpanM / 2d, set.Distribution.OffsetsM.Last(), 1e-12d);
        }

        private static void CountDistribution()
        {
            var set = RectangularStirrupPlanner.PlanSet(new RectangularStirrupSetInput
            {
                Shape = new RectangularStirrupInput { WidthM = .3d, DepthM = .4d, CoverM = .025d, DiameterMm = 6d },
                HostSpanM = 2.5d,
                EndCoverM = .04d,
                Count = 12
            });
            Equal(12, set.Distribution.Count);
            True(set.Distribution.ActualSpacingM > 0d);
        }

        private static void RejectsInvalidEnvelope() => Throws<InvalidOperationException>(() => RectangularStirrupPlanner.Plan(new RectangularStirrupInput
        {
            WidthM = .08d,
            DepthM = .2d,
            CoverM = .04d,
            DiameterMm = 12d
        }));

        private static void RejectsExcessiveBend() => Throws<InvalidOperationException>(() => RectangularStirrupPlanner.Plan(new RectangularStirrupInput
        {
            WidthM = .3d,
            DepthM = .3d,
            CoverM = .03d,
            DiameterMm = 8d,
            BendRadiusM = .2d
        }));

        private static void RejectsHookOutsideEnvelope() => Throws<InvalidOperationException>(() => RectangularStirrupPlanner.Plan(new RectangularStirrupInput
        {
            WidthM = .25d,
            DepthM = .25d,
            CoverM = .02d,
            DiameterMm = 8d,
            HookLengthM = .5d,
            HookTailAngleDeg = 45d
        }));

        private static void RejectsAmbiguousDistribution() => Throws<InvalidOperationException>(() => RectangularStirrupPlanner.PlanSet(new RectangularStirrupSetInput
        {
            Shape = new RectangularStirrupInput { WidthM = .3d, DepthM = .4d, CoverM = .025d, DiameterMm = 8d },
            HostSpanM = 2d,
            EndCoverM = .04d,
            Count = 10,
            SpacingMm = 150d
        }));

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
