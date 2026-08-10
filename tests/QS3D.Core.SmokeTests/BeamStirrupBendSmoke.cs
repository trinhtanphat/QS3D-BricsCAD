using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupBendSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            LegacyLoopRemainsByteForGeometryCompatible();
            RoundedBendsTrackExactCenterline();
            HookTailsAreExplicitAndSymmetric();
            RejectsExcessiveBendRadius();
            RejectsHookOutsideEnvelope();
            RejectsAngleWithoutHookLength();
        }

        private static void LegacyLoopRemainsByteForGeometryCompatible()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 6d,
                WidthM = .3d,
                HeightM = .5d,
                SectionCoverM = .025d,
                EndCoverM = .025d,
                DiameterMm = 8d,
                Count = 5
            });
            Equal(5, layout.Count);
            Equal(5, layout.SectionLoop.Count);
            Near(-.121d, layout.SectionLoop[0].X);
            Near(-.221d, layout.SectionLoop[0].Y);
            Near(.121d, layout.SectionLoop[1].X);
            Near(.221d, layout.SectionLoop[2].Y);
            Near(layout.SectionLoop[0].X, layout.SectionLoop[4].X);
            Near(layout.SectionLoop[0].Y, layout.SectionLoop[4].Y);
            var perimeter = 2d * (.242d + .442d);
            Near(perimeter, layout.CenterlineLengthM);
            Near(perimeter, layout.PolylineLengthM);
            True(!layout.HasHookTails);
            Near(0d, layout.BendRadiusM);
        }

        private static void RoundedBendsTrackExactCenterline()
        {
            const double bend = .03d;
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 3d,
                WidthM = .5d,
                HeightM = .4d,
                SectionCoverM = .025d,
                EndCoverM = .03d,
                DiameterMm = 10d,
                Count = 3,
                BendRadiusM = bend,
                MaximumSagittaM = .0005d
            });
            var centerWidth = .5d - 2d * (.025d + .005d);
            var centerHeight = .4d - 2d * (.025d + .005d);
            var expected = 2d * (centerWidth + centerHeight) - 8d * bend + 2d * Math.PI * bend;
            Near(expected, layout.CenterlineLengthM, 1e-12d);
            Near(bend, layout.BendRadiusM);
            True(layout.SectionLoop.Count > 10);
            Near(layout.SectionLoop[0].X, layout.SectionLoop[layout.SectionLoop.Count - 1].X);
            Near(layout.SectionLoop[0].Y, layout.SectionLoop[layout.SectionLoop.Count - 1].Y);
            True(layout.PolylineLengthM <= layout.CenterlineLengthM + 1e-12d);
            True(layout.CenterlineLengthM - layout.PolylineLengthM < .005d);
            True(!layout.HasHookTails);
        }

        private static void HookTailsAreExplicitAndSymmetric()
        {
            const double hook = .08d;
            const double bend = .02d;
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 2.5d,
                WidthM = .5d,
                HeightM = .5d,
                SectionCoverM = .03d,
                EndCoverM = .03d,
                DiameterMm = 8d,
                Count = 2,
                BendRadiusM = bend,
                MaximumSagittaM = .001d,
                HookLengthM = hook,
                HookTailAngleDeg = 45d
            });
            True(layout.HasHookTails);
            var first = layout.SectionLoop.First();
            var last = layout.SectionLoop.Last();
            True(first.DistanceTo(last) > 1e-6d);
            Near(-first.X, last.X, 1e-12d);
            Near(first.Y, last.Y, 1e-12d);
            var centerWidth = .5d - 2d * (.03d + .004d);
            var centerHeight = centerWidth;
            var expected = 2d * (centerWidth + centerHeight) - 8d * bend + 2d * Math.PI * bend + 2d * hook;
            Near(expected, layout.CenterlineLengthM, 1e-12d);
            True(layout.PolylineLengthM <= layout.CenterlineLengthM + 1e-12d);
        }

        private static void RejectsExcessiveBendRadius() => Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
        {
            LengthM = 2d,
            WidthM = .25d,
            HeightM = .25d,
            SectionCoverM = .03d,
            EndCoverM = .02d,
            DiameterMm = 8d,
            Count = 2,
            BendRadiusM = .2d
        }));

        private static void RejectsHookOutsideEnvelope() => Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
        {
            LengthM = 2d,
            WidthM = .25d,
            HeightM = .25d,
            SectionCoverM = .02d,
            EndCoverM = .02d,
            DiameterMm = 8d,
            Count = 2,
            HookLengthM = .5d,
            HookTailAngleDeg = 45d
        }));

        private static void RejectsAngleWithoutHookLength() => Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
        {
            LengthM = 2d,
            WidthM = .3d,
            HeightM = .4d,
            SectionCoverM = .02d,
            EndCoverM = .02d,
            DiameterMm = 8d,
            Count = 2,
            HookTailAngleDeg = 45d
        }));

        private static void Near(double expected, double actual, double tolerance = 1e-9d)
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
