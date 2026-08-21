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
            PanelCountGrowthAfterBoundaryIsIgnoredBySnapshot();
            OpeningCountGrowthAfterBoundaryIsIgnoredBySnapshot();
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
            Equal(2, plan.SourcePanelCount, "source panel count");
            Equal(1, plan.InterruptedPanelCount, "interrupted panel count");
            Near(16d, plan.OriginalPanelAreaM2, "original panel area");
            Near(14d, plan.RemainingPanelAreaM2, "remaining panel area");
            Near(2d, plan.RemovedPanelAreaM2, "removed panel area");
            Equal(5, plan.Pieces.Count, "piece count");

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
            Equal(0, negativePanels.IndexReads, "negative panel Count must fail before index access");

            var oversizedPanels = new FixedCountProbeList<CurtainWallRect>(CurtainWallOpeningPanelPlanner.MaxInputPanels + 1, Array.Empty<CurtainWallRect>());
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(oversizedPanels, Array.Empty<CurtainWallOpeningRect>()), "oversized panel Count");
            Equal(0, oversizedPanels.IndexReads, "oversized panel Count must fail before index access");

            var oversizedOpenings = new FixedCountProbeList<CurtainWallOpeningRect>(CurtainWallOpeningPanelPlanner.MaxOpenings + 1, Array.Empty<CurtainWallOpeningRect>());
            Expect<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(Array.Empty<CurtainWallRect>(), oversizedOpenings), "oversized opening Count");
            Equal(0, oversizedOpenings.IndexReads, "oversized opening Count must fail before index access");
        }

        private static void PanelCountGrowthAfterBoundaryIsIgnoredBySnapshot()
        {
            var panels = new ChangingCountProbeList<CurtainWallRect>(
                new[] { Rect(0d, 0d, 2d, 2d) },
                firstCount: 1,
                laterCount: 2);

            var plan = CurtainWallOpeningPanelPlanner.Plan(panels, Array.Empty<CurtainWallOpeningRect>());
            Equal(1, panels.CountReads, "panel Count must be read once at the public boundary");
            Equal(1, panels.IndexReads, "nested planning must consume only the snapshotted panel range");
            Equal(1, plan.SourcePanelCount, "source panel count must retain the validated snapshot");
            Equal(1, plan.Pieces.Count, "later panel Count growth must not expand the accepted input set");
            Near(4d, plan.OriginalPanelAreaM2, "panel snapshot area");
        }

        private static void OpeningCountGrowthAfterBoundaryIsIgnoredBySnapshot()
        {
            var openings = new ChangingCountProbeList<CurtainWallOpeningRect>(
                new[] { Opening(0.5d, 0.5d, 1d, 1d) },
                firstCount: 1,
                laterCount: 2);

            var plan = CurtainWallOpeningPanelPlanner.Plan(new[] { Rect(0d, 0d, 2d, 2d) }, openings);
            Equal(1, openings.CountReads, "opening Count must be read once at the public boundary");
            Equal(1, openings.IndexReads, "nested planning must consume only the snapshotted opening range");
            Equal(1, plan.InterruptedPanelCount, "later opening Count growth must not expand the accepted input set");
            Near(3d, plan.RemainingPanelAreaM2, "opening snapshot clipping area");
            Near(1d, plan.RemovedPanelAreaM2, "opening snapshot removed area");
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

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
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

        private sealed class ChangingCountProbeList<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _firstCount;
            private readonly int _laterCount;

            internal ChangingCountProbeList(IReadOnlyList<T> items, int firstCount, int laterCount)
            {
                _items = items;
                _firstCount = firstCount;
                _laterCount = laterCount;
            }

            internal int CountReads { get; private set; }
            internal int IndexReads { get; private set; }
            public int Count => ++CountReads == 1 ? _firstCount : _laterCount;
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
