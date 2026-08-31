using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapePathCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GrowthRejectsBeforeUnexpectedIndexerRead();
            ShrinkRejectsBeforeMissingIndexerRead();
            PostTraversalCountDriftRejects();
            TooFewRejectsBeforeIndexerRead();
            StablePointsSnapshotWithoutEnumeration();
        }

        private static void GrowthRejectsBeforeUnexpectedIndexerRead()
        {
            var source = HostilePointList.WithCounts(2, 2, 3);
            ExpectInvalidOperation(() => _ = new RebarShapePath("11", source), "growth");
            Equal(1, source.IndexerReads, "growth indexer reads");
            Equal(0, source.EnumeratorRequests, "growth enumerator requests");
        }

        private static void ShrinkRejectsBeforeMissingIndexerRead()
        {
            var source = HostilePointList.WithCounts(2, 2, 1);
            ExpectInvalidOperation(() => _ = new RebarShapePath("11", source), "shrink");
            Equal(1, source.IndexerReads, "shrink indexer reads");
            Equal(0, source.EnumeratorRequests, "shrink enumerator requests");
        }

        private static void PostTraversalCountDriftRejects()
        {
            var source = HostilePointList.WithCounts(2, 2, 2, 3);
            ExpectInvalidOperation(() => _ = new RebarShapePath("11", source), "final rebound");
            Equal(2, source.IndexerReads, "final rebound indexer reads");
            Equal(0, source.EnumeratorRequests, "final rebound enumerator requests");
        }

        private static void TooFewRejectsBeforeIndexerRead()
        {
            var source = HostilePointList.WithCounts(1);
            ExpectArgument(() => _ = new RebarShapePath("11", source), "too few");
            Equal(0, source.IndexerReads, "too few indexer reads");
            Equal(0, source.EnumeratorRequests, "too few enumerator requests");
        }

        private static void StablePointsSnapshotWithoutEnumeration()
        {
            var source = HostilePointList.WithCounts(2, 2, 2, 2);
            var path = new RebarShapePath(" 11 ", source);

            Equal("11", path.ShapeCode, "stable shape code");
            Equal(2, path.Points.Count, "stable point count");
            Equal(0d, path.Points[0].X, "stable first X");
            Equal(0d, path.Points[0].Y, "stable first Y");
            Equal(1d, path.Points[1].X, "stable second X");
            Equal(2d, path.Points[1].Y, "stable second Y");
            Equal(2, source.IndexerReads, "stable indexer reads");
            Equal(0, source.EnumeratorRequests, "stable enumerator requests");
        }

        private static void ExpectInvalidOperation(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("point Count changed during snapshot", StringComparison.Ordinal))
                    throw new Exception(label + " wrong InvalidOperationException: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected InvalidOperationException.");
        }

        private static void ExpectArgument(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (!ex.Message.Contains("at least two points", StringComparison.Ordinal))
                    throw new Exception(label + " wrong ArgumentException: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected ArgumentException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class HostilePointList : IReadOnlyList<RebarShapePoint>
        {
            private readonly int[] _counts;
            private int _countReads;
            private readonly RebarShapePoint[] _points =
            {
                new RebarShapePoint(0d, 0d),
                new RebarShapePoint(1d, 2d),
                new RebarShapePoint(3d, 4d),
            };

            private HostilePointList(int[] counts) => _counts = counts;

            internal int IndexerReads { get; private set; }
            internal int EnumeratorRequests { get; private set; }

            internal static HostilePointList WithCounts(params int[] counts) => new HostilePointList(counts);

            public int Count
            {
                get
                {
                    var index = _countReads++;
                    return index < _counts.Length ? _counts[index] : _counts[_counts.Length - 1];
                }
            }

            public RebarShapePoint this[int index]
            {
                get
                {
                    IndexerReads++;
                    if (index < 0 || index >= _points.Length) throw new IndexOutOfRangeException();
                    return _points[index];
                }
            }

            public IEnumerator<RebarShapePoint> GetEnumerator()
            {
                EnumeratorRequests++;
                throw new InvalidOperationException("Rebar shape path snapshot must not request caller enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
