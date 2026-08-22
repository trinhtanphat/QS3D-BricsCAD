using System;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityOpeningCompensationSmoke
    {
        internal static void Run()
        {
            CollectivelySignificantOpeningAreasArePreserved();
            InputOrderDoesNotDropSmallOpeningAreas();
            OrdinaryWallQuantitiesRemainStable();
            OpeningAreaOverflowStillFailsClosed();
        }

        private static void CollectivelySignificantOpeningAreasArePreserved()
        {
            var quantities = WallQuantityCalculator.Calculate(
                2e16d,
                1d,
                1d,
                new[]
                {
                    new OpeningCut { WidthM = 1e16d, HeightM = 1d },
                    new OpeningCut { WidthM = 1d, HeightM = 1d },
                    new OpeningCut { WidthM = 1d, HeightM = 1d }
                });

            Assert(quantities.OpeningAreaM2 == 10000000000000002d, "Wall opening aggregation must preserve collectively significant small opening areas after a huge opening.");
            Assert(quantities.NetAreaM2 == 9999999999999998d, "Wall net area must inherit the compensated opening-area total.");
            Assert(quantities.DeductionVolumeM3 == 10000000000000002d, "Wall deduction volume must inherit the compensated opening-area total.");
            Assert(quantities.NetVolumeM3 == 9999999999999998d, "Wall net volume must inherit the compensated opening-area total.");
        }

        private static void InputOrderDoesNotDropSmallOpeningAreas()
        {
            var quantities = WallQuantityCalculator.Calculate(
                2e16d,
                1d,
                1d,
                new[]
                {
                    new OpeningCut { WidthM = 1d, HeightM = 1d },
                    new OpeningCut { WidthM = 1e16d, HeightM = 1d },
                    new OpeningCut { WidthM = 1d, HeightM = 1d }
                });

            Assert(quantities.OpeningAreaM2 == 10000000000000002d, "Wall opening aggregation must preserve collectively significant small opening areas across input orderings.");
        }

        private static void OrdinaryWallQuantitiesRemainStable()
        {
            var quantities = WallQuantityCalculator.Calculate(
                5d,
                3d,
                0.2d,
                new[]
                {
                    new OpeningCut { WidthM = 1d, HeightM = 2d },
                    new OpeningCut { WidthM = 0.5d, HeightM = 2d }
                });

            Assert(quantities.GrossAreaM2 == 15d, "Ordinary gross wall area changed unexpectedly.");
            Assert(quantities.OpeningAreaM2 == 3d, "Ordinary opening area changed unexpectedly.");
            Assert(quantities.NetAreaM2 == 12d, "Ordinary net wall area changed unexpectedly.");
            Assert(Math.Abs(quantities.GrossVolumeM3 - 3d) <= 1e-12d, "Ordinary gross wall volume changed unexpectedly.");
            Assert(Math.Abs(quantities.DeductionVolumeM3 - 0.6d) <= 1e-12d, "Ordinary deduction volume changed unexpectedly.");
            Assert(Math.Abs(quantities.NetVolumeM3 - 2.4d) <= 1e-12d, "Ordinary net wall volume changed unexpectedly.");
        }

        private static void OpeningAreaOverflowStillFailsClosed()
        {
            var error = Capture<OverflowException>(() => WallQuantityCalculator.Calculate(
                double.MaxValue,
                1d,
                0d,
                new[]
                {
                    new OpeningCut { WidthM = double.MaxValue, HeightM = 1d },
                    new OpeningCut { WidthM = double.MaxValue, HeightM = 1d }
                }));

            Assert(error.Message == "Total opening area is not finite.", "Wall opening accumulation overflow contract changed unexpectedly.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
