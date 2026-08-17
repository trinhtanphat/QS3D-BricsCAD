using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgedPolygonFootprintSmoke
    {
        public static void Run()
        {
            StraightPolygonPreservesCanonicalVertices();
            BulgedBoundaryFeedsPolygonalMeshPlanner();
            SelfIntersectionFailsClosed();
            ExcessiveTessellationFailsClosed();
            OversizedSourceCountFailsBeforeIndexAccess();
            MutableSourceCountFailsBeforeTraversalExpansion();
        }

        private static void StraightPolygonPreservesCanonicalVertices()
        {
            var footprint = BulgedPolygonFootprintTessellator.TessellateClosed(new[]
            {
                new BulgedPolygonVertex2(new Point2(0d, 0d)),
                new BulgedPolygonVertex2(new Point2(4d, 0d)),
                new BulgedPolygonVertex2(new Point2(4d, 3d)),
                new BulgedPolygonVertex2(new Point2(0d, 3d))
            });

            Require(footprint.Count == 4, "Straight closed polygon should preserve its four source vertices without a duplicate closing point.");
            Near(0d, footprint[0].DistanceTo(new Point2(0d, 0d)));
            Near(0d, footprint[3].DistanceTo(new Point2(0d, 3d)));
        }

        private static void BulgedBoundaryFeedsPolygonalMeshPlanner()
        {
            var footprint = BulgedPolygonFootprintTessellator.TessellateClosed(new[]
            {
                new BulgedPolygonVertex2(new Point2(0d, 0d), Math.Tan(Math.PI / 8d)),
                new BulgedPolygonVertex2(new Point2(4d, 0d)),
                new BulgedPolygonVertex2(new Point2(4d, 4d)),
                new BulgedPolygonVertex2(new Point2(0d, 4d))
            }, 0.01d);

            Require(footprint.Count > 4, "A non-zero bulge should produce intermediate bounded footprint vertices.");
            Require(footprint[0].DistanceTo(footprint[footprint.Count - 1]) > 1e-10d, "Bulged footprint must not duplicate its closing point.");
            Require(footprint.All(p => Finite(p.X) && Finite(p.Y)), "Bulged footprint contains a non-finite point.");

            var layout = PolygonalSlabMeshPlanner.Plan(new PolygonalSlabMeshInput
            {
                FootprintM = footprint,
                ThicknessM = 0.2d,
                CoverM = 0.03d,
                XDiameterMm = 10d,
                YDiameterMm = 10d,
                XSpacingMm = 500d,
                YSpacingMm = 500d,
                IncludeBottom = true,
                IncludeTop = false
            });

            Require(layout.Count > 0, "Tessellated bulged footprint did not produce polygonal mesh bars.");
            Require(layout.Bars.All(bar => Finite(bar.LengthM) && bar.LengthM > 0d), "Bulged polygon mesh produced an invalid bar length.");
        }

        private static void SelfIntersectionFailsClosed()
        {
            Throws<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[]
            {
                new BulgedPolygonVertex2(new Point2(0d, 0d)),
                new BulgedPolygonVertex2(new Point2(2d, 2d)),
                new BulgedPolygonVertex2(new Point2(0d, 2d)),
                new BulgedPolygonVertex2(new Point2(2d, 0d))
            }));
        }

        private static void ExcessiveTessellationFailsClosed()
        {
            Throws<InvalidOperationException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(new[]
            {
                new BulgedPolygonVertex2(new Point2(0d, 0d), 1d),
                new BulgedPolygonVertex2(new Point2(2d, 0d)),
                new BulgedPolygonVertex2(new Point2(1d, 2d))
            }, 1e-15d));
        }

        private static void OversizedSourceCountFailsBeforeIndexAccess()
        {
            var source = new OversizedCountVertices();
            Throws<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(source));
            Require(source.IndexAccessCount == 0, "Known oversized source Count must fail before any vertex index is accessed.");
        }

        private static void MutableSourceCountFailsBeforeTraversalExpansion()
        {
            var source = new ExpandingCountVertices(new[]
            {
                new BulgedPolygonVertex2(new Point2(0d, 0d)),
                new BulgedPolygonVertex2(new Point2(2d, 0d)),
                new BulgedPolygonVertex2(new Point2(0d, 2d))
            });

            Throws<ArgumentException>(() => BulgedPolygonFootprintTessellator.TessellateClosed(source));
            Require(source.HighestRequestedIndex == 2, "Mutable source Count must not expand traversal beyond the validated vertex snapshot.");
        }

        private sealed class OversizedCountVertices : IReadOnlyList<BulgedPolygonVertex2>
        {
            public int Count => 4097;
            public int IndexAccessCount { get; private set; }

            public BulgedPolygonVertex2 this[int index]
            {
                get
                {
                    IndexAccessCount++;
                    throw new InvalidOperationException("Oversized source must not be indexed.");
                }
            }

            public IEnumerator<BulgedPolygonVertex2> GetEnumerator()
            {
                throw new InvalidOperationException("Oversized source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ExpandingCountVertices : IReadOnlyList<BulgedPolygonVertex2>
        {
            private readonly BulgedPolygonVertex2[] _vertices;
            private bool _expanded;

            public ExpandingCountVertices(BulgedPolygonVertex2[] vertices)
            {
                _vertices = vertices;
            }

            public int HighestRequestedIndex { get; private set; } = -1;
            public int Count => _expanded ? 4097 : _vertices.Length;

            public BulgedPolygonVertex2 this[int index]
            {
                get
                {
                    HighestRequestedIndex = Math.Max(HighestRequestedIndex, index);
                    if (index == 0) _expanded = true;
                    return _vertices[index];
                }
            }

            public IEnumerator<BulgedPolygonVertex2> GetEnumerator() => ((IEnumerable<BulgedPolygonVertex2>)_vertices).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
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
