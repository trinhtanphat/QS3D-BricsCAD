using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallOpeningPanelPlannerBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OrdinaryInterruptionProducesDeterministicMetadata();
            NullAndCountContractsFailClosedBeforeIndexing();
            PanelCountDriftFailsClosedBeforeIndexing();
            OpeningCountDriftFailsClosedBeforeIndexing();
        }

        private static void OrdinaryInterruptionProducesDeterministicMetadata()
        {
            var panels = new[]
            {
                Rect(10d, 0d, 2d, 2d),
                Rect(0d, 0d, 4d, 3d)
            };
            var openings = new[] { Opening(1d, 1d, 2d, 1d) };

            var plan = CurtainWallOpeningPanelPlanner.Plan(panels, openings);
            if (plan.SourcePanelCount != 2)
                throw new InvalidOperationException("Opening panel plan must preserve source panel count.");
            if (plan.InterruptedPanelCount != 1)
                throw new InvalidOperationException("Exactly one source panel must be interrupted.");
            Near(16d, plan.OriginalPanelAreaM2, "original panel area");
            Near(14d, plan.RemainingPanelAreaM2, "remaining panel area");
            Near(2d, plan.RemovedPanelAreaM2, "removed panel area");
            if (plan.Pieces.Count != 5)
                throw new InvalidOperationException("One interrupted panel plus one untouched panel must produce five pieces.");

            for (var index = 1; index < plan.Pieces.Count; index++)
            {
                var previous = plan.Pieces[index - 1];
                var current = plan.Pieces[index];
                if (previous.SourcePanelIndex > current.SourcePanelIndex)
                    throw new InvalidOperationException("Panel pieces must remain ordered by source panel index.");
                if (previous.SourcePanelIndex == current.SourcePanelIndex && previous.Z_M > current.Z_M)
                    throw new InvalidOperationException("Panel pieces within a source must remain ordered by Z.");
                if (previous.SourcePanelIndex == current.SourcePanelIndex && previous.Z_M == current.Z_M && previous.X_M > current.X_M)
                    throw new InvalidOperationException("Panel pieces at the same Z must remain ordered by X.");
            }
        }

        private static void NullAndCountContractsFailClosedBeforeIndexing()
        {
            Expect<ArgumentNullException>(() => CurtainWallOpeningPanelPlanner.Plan(null!, Array.Empty<CurtainWallOpeningRect>()), "null panels");
            Expect<ArgumentNullException>(() => CurtainWallOpeningPanelPlanner.Plan(Array.Empty<CurtainWallRect>(), null!), "null openings");

            var negativePanels = new FixedCountProbeList<CurtainWallRect>(-1, Array.Empty<CurtainWallRect>());
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(negativePanels, Array.Empty<CurtainWallOpeningRect>()), "negative panel Count");
            if (negativePanels.IndexReads != 0)
                throw new InvalidOperationException("Negative panel Count must fail before index access.");

            var oversizedPanels = new FixedCountProbeList<CurtainWallRect>(CurtainWallOpeningPanelPlanner.MaxInputPanels + 1, Array.Empty<CurtainWallRect>());
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(oversizedPanels, Array.Empty<CurtainWallOpeningRect>()), "oversized panel Count");
            if (oversizedPanels.IndexReads != 0)
                throw new InvalidOperationException("Oversized panel Count must fail before index access.");

            var oversizedOpenings = new FixedCountProbeList<CurtainWallOpeningRect>(CurtainWallOpeningPanelPlanner.MaxOpenings + 1, Array.Empty<CurtainWallOpeningRect>());
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(Array.Empty<CurtainWallRect>(), oversizedOpenings), "oversized opening Count");
            if (oversizedOpenings.IndexReads != 0)
                throw new InvalidOperationException("Oversized opening Count must fail before index access.");
        }

        private static void PanelCountDriftFailsClosedBeforeIndexing()
        {
            var growing = new ChangingCountList<CurtainWallRect>(
                new[] { Rect(0d, 0d, 2d, 2d) },
                firstCount: 1,
                laterCount: 2);
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(growing, Array.Empty<CurtainWallOpeningRect>()), "growing panel Count");
            if (growing.IndexReads != 0)
                throw new InvalidOperationException("Panel Count drift must fail before reading an element against the stale count.");

            var shrinking = new ChangingCountList<CurtainWallRect>(
                new[] { Rect(0d, 0d, 2d, 2d) },
                firstCount: 1,
                laterCount: 0);
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(shrinking, Array.Empty<CurtainWallOpeningRect>()), "shrinking panel Count");
            if (shrinking.IndexReads != 0)
                throw new InvalidOperationException("Shrinking panel Count must fail before index access.");
        }

        private static void OpeningCountDriftFailsClosedBeforeIndexing()
        {
            var growing = new ChangingCountList<CurtainWallOpeningRect>(
                new[] { Opening(0.5d, 0.5d, 1d, 1d) },
                firstCount: 1,
                laterCount: 2);
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(new[] { Rect(0d, 0d, 2d, 2d) }, growing), "growing opening Count");
            if (growing.IndexReads != 0)
                throw new InvalidOperationException("Opening Count drift must fail before reading an element against the stale count.");

            var shrinking = new ChangingCountList<CurtainWallOpeningRect>(
                new[] { Opening(0.5d, 0.5d, 1d, 1d) },
                firstCount: 1,
                laterCount: 0);
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(new[] { Rect(0d, 0d, 2d, 2d) }, shrinking), "shrinking opening Count");
            if (shrinking.IndexReads != 0)
                throw new InvalidOperationException("Shrinking opening Count must fail before index access.");
        }

        private static CurtainWallRect Rect(double x, double z, double width, double height)
            => new CurtainWallRect(x, z, width, height);

        private static CurtainWallOpeningRect Opening(double x, double z, double width, double height)
            => new CurtainWallOpeningRect { X_M = x, Z_M = z, WidthM = width, HeightM = height };

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException(label + " mismatch: expected " + expected + ", actual " + actual + ".");
        }

        private sealed class FixedCountProbeList<T> : IReadOnlyList<T>
        {
            private readonly int _count;
            private readonly IReadOnlyList<T> _items;

            internal FixedCountProbeList(int count, IReadOnlyList<T> items)
            {
                _count = count;
                _items = items;
            }

            internal int IndexReads { get; private set; }
            public int Count => _count;
            public T this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ChangingCountList<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _firstCount;
            private readonly int _laterCount;
            private int _countReads;

            internal ChangingCountList(IReadOnlyList<T> items, int firstCount, int laterCount)
            {
                _items = items;
                _firstCount = firstCount;
                _laterCount = laterCount;
            }

            internal int IndexReads { get; private set; }
            public int Count => ++_countReads == 1 ? _firstCount : _laterCount;
            public T this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
