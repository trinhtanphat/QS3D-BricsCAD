using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupLayoutSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            CountLayoutBuildsClosedSectionLoop();
            SpacingLayoutIsBounded();
            ImpossibleSectionCoverIsRejected();
            AmbiguousDistributionInputIsRejected();
        }

        private static void CountLayoutBuildsClosedSectionLoop()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 6d,
                WidthM = 0.3d,
                HeightM = 0.5d,
                SectionCoverM = 0.025d,
                EndCoverM = 0.025d,
                DiameterMm = 8d,
                Count = 5
            });
            Equal(5, layout.Count);
            Equal(5, layout.SectionLoop.Count);
            Near(layout.SectionLoop[0].X, layout.SectionLoop[4].X);
            Near(layout.SectionLoop[0].Y, layout.SectionLoop[4].Y);
            Near(-0.121d, layout.SectionLoop[0].X);
            Near(-0.221d, layout.SectionLoop[0].Y);
            Near(0.121d, layout.SectionLoop[1].X);
            Near(0.221d, layout.SectionLoop[2].Y);
            True(layout.StationOffsetsM.Zip(layout.StationOffsetsM.Skip(1), (a, b) => b > a).All(x => x));
        }

        private static void SpacingLayoutIsBounded()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 3d,
                WidthM = 0.25d,
                HeightM = 0.45d,
                SectionCoverM = 0.02d,
                EndCoverM = 0.03d,
                DiameterMm = 10d,
                SpacingMm = 150d
            });
            True(layout.Count > 1);
            True(layout.Count < 100);
            True(layout.ActualSpacingM > 0d && layout.ActualSpacingM <= 0.150000001d);
            True(layout.StationOffsetsM.All(x => Math.Abs(x) < 1.5d));
        }

        private static void ImpossibleSectionCoverIsRejected()
        {
            Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 2d,
                WidthM = 0.2d,
                HeightM = 0.3d,
                SectionCoverM = 0.1d,
                EndCoverM = 0.02d,
                DiameterMm = 12d,
                Count = 2
            }));
        }

        private static void AmbiguousDistributionInputIsRejected()
        {
            Throws<InvalidOperationException>(() => BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 2d,
                WidthM = 0.3d,
                HeightM = 0.5d,
                SectionCoverM = 0.02d,
                EndCoverM = 0.02d,
                DiameterMm = 8d,
                Count = 4,
                SpacingMm = 150d
            }));
        }

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
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
