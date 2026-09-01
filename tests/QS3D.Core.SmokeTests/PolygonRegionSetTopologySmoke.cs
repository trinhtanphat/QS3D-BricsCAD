using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionSetTopologySmoke
    {
        internal static void Run()
        {
            SeparateIslandsRemainIndependentlyTagged();
            HoleClippingStaysWithinOwningIsland();
            InputOrderDoesNotChangeCanonicalRegionOrder();
            DuplicateIdsFailClosed();
            MalformedUnicodeIdsFailClosed();
            XmlInvalidIdsFailClosed();
            SupplementaryUnicodeIdsRemainCanonical();
            TouchingIslandsFailClosed();
            OverlappingIslandsFailClosed();
            NestedOuterIslandsFailClosed();
        }

        private static void SeparateIslandsRemainIndependentlyTagged()
        {
            var topology = PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("B", Square(20, 0, 30, 10)),
                new PolygonRegionSeed2("A", Square(0, 0, 10, 10))
            });

            Equal(2, topology.Islands.Count);
            Equal("A", topology.Islands[0].RegionId);
            Equal("B", topology.Islands[1].RegionId);

            var segments = PolygonRegionSetTopology.Clip(topology, PolygonScanAxis.Horizontal, 5);
            Equal(2, segments.Count);
            Equal("A", segments[0].RegionId);
            Near(0, segments[0].Start.X);
            Near(10, segments[0].End.X);
            Equal("B", segments[1].RegionId);
            Near(20, segments[1].Start.X);
            Near(30, segments[1].End.X);
        }

        private static void HoleClippingStaysWithinOwningIsland()
        {
            var topology = PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("A", Square(0, 0, 10, 10), new[] { Square(4, 4, 6, 6) }),
                new PolygonRegionSeed2("B", Square(20, 0, 30, 10))
            });

            var segments = PolygonRegionSetTopology.Clip(topology, PolygonScanAxis.Horizontal, 5);
            Equal(3, segments.Count);
            Equal(2, segments.Count(x => x.RegionId == "A"));
            Equal(1, segments.Count(x => x.RegionId == "B"));
            Near(0, segments[0].Start.X);
            Near(4, segments[0].End.X);
            Near(6, segments[1].Start.X);
            Near(10, segments[1].End.X);
        }

        private static void InputOrderDoesNotChangeCanonicalRegionOrder()
        {
            var left = PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("region-2", Square(20, 0, 30, 10)),
                new PolygonRegionSeed2("region-1", Square(0, 0, 10, 10))
            });
            var right = PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("region-1", Square(0, 0, 10, 10)),
                new PolygonRegionSeed2("region-2", Square(20, 0, 30, 10))
            });

            Equal(string.Join("|", left.Islands.Select(x => x.RegionId)), string.Join("|", right.Islands.Select(x => x.RegionId)));
        }

        private static void DuplicateIdsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("A", Square(0, 0, 10, 10)),
                new PolygonRegionSeed2("a", Square(20, 0, 30, 10))
            }));
        }

        private static void MalformedUnicodeIdsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("region-\uD800", Square(0, 0, 10, 10))
            }));
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("region-\uDC00", Square(0, 0, 10, 10))
            }));
        }

        private static void XmlInvalidIdsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("region-\uFFFE", Square(0, 0, 10, 10))
            }));
        }

        private static void SupplementaryUnicodeIdsRemainCanonical()
        {
            const string expected = "region-\U0001F6E0";
            var topology = PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("  " + expected + "  ", Square(0, 0, 10, 10))
            });

            Equal(expected, topology.Islands.Single().RegionId);
            var segment = PolygonRegionSetTopology.Clip(topology, PolygonScanAxis.Horizontal, 5).Single();
            Equal(expected, segment.RegionId);
        }

        private static void TouchingIslandsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("A", Square(0, 0, 10, 10)),
                new PolygonRegionSeed2("B", Square(10, 2, 15, 8))
            }));
        }

        private static void OverlappingIslandsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("A", Square(0, 0, 10, 10)),
                new PolygonRegionSeed2("B", Square(8, 2, 15, 8))
            }));
        }

        private static void NestedOuterIslandsFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionSetTopology.NormalizeAndValidate(new[]
            {
                new PolygonRegionSeed2("A", Square(0, 0, 20, 20)),
                new PolygonRegionSeed2("B", Square(5, 5, 10, 10))
            }));
        }

        private static Point2[] Square(double minX, double minY, double maxX, double maxY) => new[]
        {
            new Point2(minX, minY),
            new Point2(maxX, minY),
            new Point2(maxX, maxY),
            new Point2(minX, maxY)
        };

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class PolygonRegionSetTopologySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => PolygonRegionSetTopologySmoke.Run();
    }
}
