using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityOpeningBoundSmoke
    {
        public static void Run()
        {
            ExactBoundRemainsAccepted();
            KnownOversizedCollectionFailsBeforeEnumeration();
            LazyOversizedEnumerableStopsAtFirstExcessItem();
            NullOpeningRejectionRemainsIntact();
            LostAreaDeductionFailsClosed();
            LostVolumeDeductionFailsClosed();
            OrdinaryAndFullDeductionsRemainStable();
        }

        private static void ExactBoundRemainsAccepted()
        {
            var yielded = 0;
            var result = WallQuantityCalculator.Calculate(
                10d,
                3d,
                0.2d,
                LazyOpenings(10000, () => yielded++));

            Require(yielded == 10000, "Wall quantity did not consume exactly the accepted 10,000 opening inputs.");
            Near(30d, result.GrossAreaM2, "Gross wall area changed at the opening input bound.");
            Near(0d, result.OpeningAreaM2, "Zero-area openings changed opening takeoff at the input bound.");
            Near(30d, result.NetAreaM2, "Zero-area openings changed net wall area at the input bound.");
        }

        private static void KnownOversizedCollectionFailsBeforeEnumeration()
        {
            var openings = new KnownCountOpenings(10001);
            Throws<InvalidOperationException>(() => WallQuantityCalculator.Calculate(10d, 3d, 0.2d, openings));
            Require(!openings.EnumeratorRequested, "Known oversized opening collection was enumerated before its count guard rejected it.");
        }

        private static void LazyOversizedEnumerableStopsAtFirstExcessItem()
        {
            var yielded = 0;
            Throws<InvalidOperationException>(() => WallQuantityCalculator.Calculate(
                10d,
                3d,
                0.2d,
                LazyOpenings(10002, () => yielded++)));
            Require(yielded == 10001, "Lazy oversized opening enumeration was not stopped at the first item beyond the 10,000-entry bound.");
        }

        private static void NullOpeningRejectionRemainsIntact()
        {
            Throws<ArgumentException>(() => WallQuantityCalculator.Calculate(
                10d,
                3d,
                0.2d,
                new OpeningCut[] { null! }));
        }

        private static void LostAreaDeductionFailsClosed()
        {
            Throws<InvalidOperationException>(() => WallQuantityCalculator.Calculate(
                1e16d,
                1d,
                0d,
                new[] { new OpeningCut { WidthM = 1d, HeightM = 1d } }));
        }

        private static void LostVolumeDeductionFailsClosed()
        {
            Throws<InvalidOperationException>(() => WallQuantityCalculator.Calculate(
                2d,
                1d,
                1e16d,
                new[] { new OpeningCut { WidthM = 2e-16d, HeightM = 1d } }));
        }

        private static void OrdinaryAndFullDeductionsRemainStable()
        {
            var ordinary = WallQuantityCalculator.Calculate(
                10d,
                3d,
                0.2d,
                new[] { new OpeningCut { WidthM = 1d, HeightM = 2d } });

            Near(30d, ordinary.GrossAreaM2, "Ordinary gross wall area changed.");
            Near(2d, ordinary.OpeningAreaM2, "Ordinary opening area changed.");
            Near(28d, ordinary.NetAreaM2, "Ordinary net wall area changed.");
            Near(6d, ordinary.GrossVolumeM3, "Ordinary gross wall volume changed.");
            Near(0.4d, ordinary.DeductionVolumeM3, "Ordinary deduction volume changed.");
            Near(5.6d, ordinary.NetVolumeM3, "Ordinary net wall volume changed.");
            Near(56d, ordinary.TwoSideFinishAreaM2, "Ordinary finish area changed.");

            var full = WallQuantityCalculator.Calculate(
                10d,
                3d,
                0.2d,
                new[] { new OpeningCut { WidthM = 100d, HeightM = 100d } });

            Near(30d, full.OpeningAreaM2, "Full opening deduction no longer clamps to gross wall area.");
            Near(0d, full.NetAreaM2, "Full opening deduction no longer floors net wall area to zero.");
            Near(6d, full.DeductionVolumeM3, "Full opening deduction volume changed.");
            Near(0d, full.NetVolumeM3, "Full opening deduction no longer floors net wall volume to zero.");
            Near(0d, full.TwoSideFinishAreaM2, "Full opening deduction no longer floors finish area to zero.");
        }

        private static IEnumerable<OpeningCut> LazyOpenings(int count, Action onYield)
        {
            for (var index = 0; index < count; index++)
            {
                onYield();
                yield return new OpeningCut { WidthM = 0d, HeightM = 0d };
            }
        }

        private sealed class KnownCountOpenings : IReadOnlyCollection<OpeningCut>
        {
            public KnownCountOpenings(int count) => Count = count;

            public int Count { get; }
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<OpeningCut> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Known oversized opening collection must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
