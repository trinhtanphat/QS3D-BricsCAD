using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineMetricSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LengthReadsEachSourcePointExactlyOnce();
            SignedAreaReadsEachSourcePointExactlyOnce();
            RejectsGrowthDuringSnapshot();
            RejectsShrinkDuringSnapshot();
            RejectsOversizedSnapshotBeforeIndexing();
            PreservesStableLengthAndAreaSemantics();
        }

        private static void LengthReadsEachSourcePointExactlyOnce()
        {
            var points = new ReadOnceList(new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 4d)
            });

            Exact(7d, PolylineMetrics.Length(points, false), "read-once length");
            points.AssertEveryPointReadExactlyOnce("length snapshot");
        }

        private static void SignedAreaReadsEachSourcePointExactlyOnce()
        {
            var points = new ReadOnceList(new[]
            {
                new Point2(0d, 0d),
                new Point2(4d, 0d),
                new Point2(4d, 3d),
                new Point2(0d, 3d)
            });

            Exact(12d, PolylineMetrics.SignedArea(points), "read-once signed area");
            points.AssertEveryPointReadExactlyOnce("area snapshot");
        }

        private static void RejectsGrowthDuringSnapshot()
        {
            var points = new MutatingList(
                new[] { new Point2(0d, 0d), new Point2(2d, 0d), new Point2(2d, 2d) },
                1,
                list => list.Add(new Point2(0d, 2d)));

            ThrowsInvalidOperation(() => PolylineMetrics.Length(points, false), "growing point collection");
        }

        private static void RejectsShrinkDuringSnapshot()
        {
            var points = new MutatingList(
                new[] { new Point2(0d, 0d), new Point2(2d, 0d), new Point2(2d, 2d) },
                0,
                list => list.RemoveAt(list.Count - 1));

            ThrowsInvalidOperation(() => PolylineMetrics.SignedArea(points), "shrinking point collection");
        }

        private static void RejectsOversizedSnapshotBeforeIndexing()
        {
            var points = new OversizedList();
            ThrowsInvalidOperation(() => PolylineMetrics.Length(points, false), "oversized point collection");
            if (points.IndexRead)
                throw new InvalidOperationException("Polyline metric snapshot must reject an oversized collection before reading any point index.");
        }

        private static void PreservesStableLengthAndAreaSemantics()
        {
            var compensated = new[]
            {
                new Point2(0d, 0d),
                new Point2(1e16, 0d),
                new Point2(1e16, 1d),
                new Point2(1e16, 2d)
            };
            Exact(10000000000000002d, PolylineMetrics.Length(compensated, false), "compensated length after snapshot");

            var rectangle = new[]
            {
                new Point2(0d, 0d),
                new Point2(4d, 0d),
                new Point2(4d, 3d),
                new Point2(0d, 3d)
            };
            Exact(12d, PolylineMetrics.Area(rectangle), "stable area after snapshot");
        }

        private static void ThrowsInvalidOperation(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Polyline metric snapshot must fail closed for " + scenario + ".");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected polyline metric for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class ReadOnceList : IReadOnlyList<Point2>
        {
            private readonly Point2[] _points;
            private readonly int[] _reads;

            public ReadOnceList(Point2[] points)
            {
                _points = points;
                _reads = new int[points.Length];
            }

            public int Count => _points.Length;

            public Point2 this[int index]
            {
                get
                {
                    _reads[index]++;
                    if (_reads[index] != 1)
                        throw new InvalidOperationException("Source point index " + index + " was read more than once.");
                    return _points[index];
                }
            }

            public void AssertEveryPointReadExactlyOnce(string scenario)
            {
                for (var i = 0; i < _reads.Length; i++)
                {
                    if (_reads[i] != 1)
                        throw new InvalidOperationException("Expected exactly one read for source point " + i + " during " + scenario + ".");
                }
            }

            public IEnumerator<Point2> GetEnumerator()
            {
                for (var i = 0; i < _points.Length; i++) yield return _points[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MutatingList : IReadOnlyList<Point2>
        {
            private readonly List<Point2> _points;
            private readonly int _triggerIndex;
            private readonly Action<List<Point2>> _mutation;
            private bool _mutated;

            public MutatingList(IEnumerable<Point2> points, int triggerIndex, Action<List<Point2>> mutation)
            {
                _points = new List<Point2>(points);
                _triggerIndex = triggerIndex;
                _mutation = mutation;
            }

            public int Count => _points.Count;

            public Point2 this[int index]
            {
                get
                {
                    var value = _points[index];
                    if (!_mutated && index == _triggerIndex)
                    {
                        _mutated = true;
                        _mutation(_points);
                    }
                    return value;
                }
            }

            public IEnumerator<Point2> GetEnumerator() => _points.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class OversizedList : IReadOnlyList<Point2>
        {
            public bool IndexRead { get; private set; }
            public int Count => 1000001;

            public Point2 this[int index]
            {
                get
                {
                    IndexRead = true;
                    throw new InvalidOperationException("Oversized collection index must not be read.");
                }
            }

            public IEnumerator<Point2> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
