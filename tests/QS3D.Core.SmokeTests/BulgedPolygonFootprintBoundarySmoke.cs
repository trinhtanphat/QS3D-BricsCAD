using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgedPolygonFootprintBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StraightAndBulgedFootprintsAreDeterministic();
            ReturnedSnapshotIsIsolatedFromCallerMutation();
            InputContractsFailClosed();
            CountContractsFailClosedBeforePartialSuccess();
            InvalidTopologyRemainsRejected();
        }

        private static void StraightAndBulgedFootprintsAreDeterministic()
        {
            var square = new List<BulgedPolygonVertex2>
            {
                V(0d, 0d), V(10d, 0d), V(10d, 10d), V(0d, 10d)
            };
            var straight = BulgedPolygonFootprintTessellator.TessellateClosed(square);
            if (straight.Count != 4)
                throw new InvalidOperationException("Straight square must normalize to exactly four vertices.");
            AssertFiniteAndDistinct(straight, "straight square");

            var curved = new List<BulgedPolygonVertex2>
            {
                V(0d, 0d, 0.25d), V(10d, 0d), V(10d, 10d), V(0d, 10d)
            };
            var first = BulgedPolygonFootprintTessellator.TessellateClosed(curved, 0.01d);
            var second = BulgedPolygonFootprintTessellator.TessellateClosed(curved, 0.01d);
            if (first.Count <= 4)
                throw new InvalidOperationException("A curved footprint edge must contribute tessellated interior vertices.");
            if (first.Count != second.Count)
                throw new InvalidOperationException("Repeated bulged-footprint tessellation must retain deterministic vertex count.");
            for (var i = 0; i < first.Count; i++)
            {
                AssertSame(first[i], second[i], "repeat vertex " + i);
                AssertFinite(first[i], "curved vertex " + i);
            }
            AssertFiniteAndDistinct(first, "curved footprint");
        }

        private static void ReturnedSnapshotIsIsolatedFromCallerMutation()
        {
            var vertices = new List<BulgedPolygonVertex2>
            {
                V(0d, 0d), V(8d, 0d), V(8d, 6d), V(0d, 6d)
            };
            var result = BulgedPolygonFootprintTessellator.TessellateClosed(vertices);
            var before = Copy(result);

            vertices[0] = V(100d, 100d, 1d);
            vertices[1] = V(200d, 100d);
            if (result.Count != before.Count)
                throw new InvalidOperationException("Returned footprint must not track later caller-list mutations.");
            for (var i = 0; i < result.Count; i++)
                AssertSame(result[i], before[i], "snapshot vertex " + i);
        }

        private static void InputContractsFailClosed()
        {
            Expect<ArgumentNullException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(null), "null vertex collection");
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[] { V(0d, 0d), V(1d, 0d) }), "fewer than three vertices");
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new BulgedPolygonVertex2[] { V(0d, 0d), null, V(0d, 1d) }), "null vertex entry");
            Expect<ArgumentOutOfRangeException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[] { V(0d, 0d, double.NaN), V(1d, 0d), V(0d, 1d) }), "NaN bulge");
            Expect<ArgumentOutOfRangeException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[] { V(0d, 0d, double.PositiveInfinity), V(1d, 0d), V(0d, 1d) }), "infinite bulge");
            Expect<ArgumentOutOfRangeException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(Triangle(), 0d), "zero sagitta");
            Expect<ArgumentOutOfRangeException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(Triangle(), -0.01d), "negative sagitta");
            Expect<ArgumentOutOfRangeException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(Triangle(), double.NaN), "NaN sagitta");
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[] { V(0d, 0d), V(0d, 0d), V(1d, 1d) }), "degenerate edge");
        }

        private static void CountContractsFailClosedBeforePartialSuccess()
        {
            var oversized = new CountOnlyList(4097);
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(oversized), "oversized advertised source count");
            if (oversized.IndexReads != 0)
                throw new InvalidOperationException("Oversized known source count must be rejected before index traversal.");

            var negative = new CountOnlyList(-1);
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(negative), "negative advertised source count");
            if (negative.IndexReads != 0)
                throw new InvalidOperationException("Negative source count must fail before index traversal.");

            var drifting = new DriftingCountList();
            Expect<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(drifting), "source count drift");
            if (drifting.IndexReads != 3)
                throw new InvalidOperationException("Count-drift control must traverse exactly the originally advertised three entries before the stability check.");
        }

        private static void InvalidTopologyRemainsRejected()
        {
            var bowTie = new[]
            {
                V(0d, 0d), V(10d, 10d), V(0d, 10d), V(10d, 0d)
            };
            ExpectAny(() => BulgedPolygonFootprintTessellator.TessellateClosed(bowTie), "self-intersecting footprint");

            var duplicate = new[]
            {
                V(0d, 0d), V(10d, 0d), V(10d, 10d), V(10d, 0d), V(0d, 10d)
            };
            ExpectAny(() => BulgedPolygonFootprintTessellator.TessellateClosed(duplicate), "duplicate/retraced footprint edge");
        }

        private static BulgedPolygonVertex2[] Triangle() => new[] { V(0d, 0d), V(4d, 0d), V(0d, 3d) };
        private static BulgedPolygonVertex2 V(double x, double y, double bulge = 0d) => new BulgedPolygonVertex2(new Point2(x, y), bulge);

        private static Point2[] Copy(IReadOnlyList<Point2> points)
        {
            var copy = new Point2[points.Count];
            for (var i = 0; i < points.Count; i++) copy[i] = points[i];
            return copy;
        }

        private static void AssertFiniteAndDistinct(IReadOnlyList<Point2> points, string label)
        {
            for (var i = 0; i < points.Count; i++)
            {
                AssertFinite(points[i], label + " vertex " + i);
                var next = points[(i + 1) % points.Count];
                if (points[i].X == next.X && points[i].Y == next.Y)
                    throw new InvalidOperationException(label + " contains collapsed adjacent vertices.");
            }
        }

        private static void AssertFinite(Point2 point, string label)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new InvalidOperationException(label + " must be finite.");
        }

        private static void AssertSame(Point2 actual, Point2 expected, string label)
        {
            if (actual.X != expected.X || actual.Y != expected.Y)
                throw new InvalidOperationException(label + " mismatch.");
        }

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void ExpectAny(Action action, string label)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException(label + " must fail closed through canonical polygon validation.");
        }

        private sealed class CountOnlyList : IReadOnlyList<BulgedPolygonVertex2>
        {
            private readonly int _count;
            internal CountOnlyList(int count) { _count = count; }
            public int Count => _count;
            internal int IndexReads { get; private set; }
            public BulgedPolygonVertex2 this[int index]
            {
                get { IndexReads++; throw new InvalidOperationException("Indexer must not be reached for rejected advertised counts."); }
            }
            public IEnumerator<BulgedPolygonVertex2> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingCountList : IReadOnlyList<BulgedPolygonVertex2>
        {
            private int _countReads;
            private readonly BulgedPolygonVertex2[] _items = Triangle();
            public int Count => ++_countReads == 1 ? 3 : 4;
            internal int IndexReads { get; private set; }
            public BulgedPolygonVertex2 this[int index]
            {
                get { IndexReads++; return _items[index]; }
            }
            public IEnumerator<BulgedPolygonVertex2> GetEnumerator() => ((IEnumerable<BulgedPolygonVertex2>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
