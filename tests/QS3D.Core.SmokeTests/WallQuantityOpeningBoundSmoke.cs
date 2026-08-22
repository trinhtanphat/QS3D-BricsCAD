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
