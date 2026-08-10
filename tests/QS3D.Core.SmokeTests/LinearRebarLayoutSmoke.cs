using System;
using System.Linq;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class LinearRebarLayoutSmoke
    {
        public static void Run()
        {
            CountDistributionIsSymmetric();
            SpacingDistributionRoundsUpSafely();
            SingleBarUsesCenter();
            AmbiguousModeIsRejected();
            CoverEnvelopeIsValidated();
            ExcessiveBarCountIsRejected();
        }

        private static void CountDistributionIsSymmetric()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d,
                CoverM = 0.05d,
                DiameterMm = 10d,
                Count = 3
            });
            Equal(3, layout.Count);
            Near(0.89d, layout.UsableSpanM);
            Near(0.445d, layout.ActualSpacingM);
            Near(-0.445d, layout.OffsetsM[0]);
            Near(0d, layout.OffsetsM[1]);
            Near(0.445d, layout.OffsetsM[2]);
        }

        private static void SpacingDistributionRoundsUpSafely()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d,
                CoverM = 0.05d,
                DiameterMm = 10d,
                SpacingMm = 200d
            });
            Equal(6, layout.Count);
            Near(0.178d, layout.ActualSpacingM);
            True(layout.OffsetsM.Zip(layout.OffsetsM.Skip(1), (a, b) => b > a).All(x => x));
            True(layout.ActualSpacingM * 1000d <= 200d + 1e-9d);
        }

        private static void SingleBarUsesCenter()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.5d,
                CoverM = 0.04d,
                DiameterMm = 16d,
                Count = 1
            });
            Equal(1, layout.Count);
            Near(0d, layout.OffsetsM.Single());
            Near(0d, layout.ActualSpacingM);
        }

        private static void AmbiguousModeIsRejected()
        {
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d, CoverM = 0.05d, DiameterMm = 12d
            }));
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d, CoverM = 0.05d, DiameterMm = 12d, Count = 4, SpacingMm = 200d
            }));
        }

        private static void CoverEnvelopeIsValidated()
        {
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 0.1d, CoverM = 0.05d, DiameterMm = 20d, Count = 2
            }));
            Throws<ArgumentOutOfRangeException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = double.NaN, CoverM = 0.04d, DiameterMm = 12d, Count = 2
            }));
        }

        private static void ExcessiveBarCountIsRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 10d, CoverM = 0.04d, DiameterMm = 12d, Count = 10001
            }));
            Throws<InvalidOperationException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 100d, CoverM = 0.04d, DiameterMm = 12d, SpacingMm = 0.001d
            }));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
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
