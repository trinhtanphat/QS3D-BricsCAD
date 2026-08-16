using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSystemPlannerSmoke
    {
        public static void Run()
        {
            RotatedRectangularSystemProducesDeterministicIntersections();
            RadialSystemProducesRayRingIntersections();
            InvalidRectangularAxesFailClosed();
            DuplicateRadialAnglesFailClosed();
            PrecisionCollapsedStationOffsetFailsClosed();
            RepresentableLargeStationOffsetRemainsExact();
        }

        private static void RotatedRectangularSystemProducesDeterministicIntersections()
        {
            var curves = GridSystemPlanner.PlanRectangular(new RectangularGridSystemInput
            {
                OriginM = new Point2(10d, 20d),
                UAxis = new Point2(1d, 1d),
                VAxis = new Point2(-1d, 1d),
                UMinM = 0d,
                UMaxM = 4d,
                VMinM = 0d,
                VMaxM = 3d,
                UStations = new[]
                {
                    new GridLinearStation("U-1", 0d),
                    new GridLinearStation("U-2", 2d),
                    new GridLinearStation("U-3", 4d)
                },
                VStations = new[]
                {
                    new GridLinearStation("V-A", 0d),
                    new GridLinearStation("V-B", 3d)
                }
            });

            Require(curves.Count == 5, "Rectangular Grid plan should contain every explicit U/V station exactly once.");
            Require(curves.All(x => x.Kind == GridReferenceCurveKind.Line), "Rectangular Grid plan must emit LINE references only.");
            var intersections = GridIntersectionPlanner.FindIntersections(curves);
            Require(intersections.Count == 6, "Three U stations by two V stations should produce six deterministic intersections.");
            Require(intersections.All(x => Finite(x.Point.X) && Finite(x.Point.Y)), "Rectangular Grid produced a non-finite intersection.");
        }

        private static void RadialSystemProducesRayRingIntersections()
        {
            var curves = GridSystemPlanner.PlanRadial(new RadialGridSystemInput
            {
                CenterM = new Point2(5d, -2d),
                InnerRadiusM = 0d,
                OuterRadiusM = 5d,
                Rays = new[]
                {
                    new GridAngularStation("RAY-0", 0d),
                    new GridAngularStation("RAY-90", Math.PI / 2d),
                    new GridAngularStation("RAY-180", Math.PI)
                },
                Rings = new[]
                {
                    new GridRadialStation("RING-2", 2d),
                    new GridRadialStation("RING-4", 4d)
                }
            });

            Require(curves.Count == 5, "Radial Grid plan should contain every explicit ray/ring exactly once.");
            Require(curves.Count(x => x.Kind == GridReferenceCurveKind.Line) == 3, "Radial Grid should emit three ray LINE references.");
            Require(curves.Count(x => x.Kind == GridReferenceCurveKind.Arc) == 2, "Radial Grid should emit two full-circle ring ARC references.");

            var intersections = GridIntersectionPlanner.FindIntersections(curves);
            var rayRing = intersections.Count(x => IsRayRing(x.FirstElementId, x.SecondElementId));
            Require(rayRing == 6, "Three rays by two rings should produce six ray/ring intersections.");
        }

        private static void InvalidRectangularAxesFailClosed()
        {
            Throws<InvalidOperationException>(() => GridSystemPlanner.PlanRectangular(new RectangularGridSystemInput
            {
                OriginM = new Point2(0d, 0d),
                UAxis = new Point2(1d, 0d),
                VAxis = new Point2(1d, 1d),
                UMinM = 0d,
                UMaxM = 5d,
                VMinM = 0d,
                VMaxM = 5d,
                UStations = new[] { new GridLinearStation("U", 1d) },
                VStations = new[] { new GridLinearStation("V", 1d) }
            }));
        }

        private static void DuplicateRadialAnglesFailClosed()
        {
            Throws<InvalidOperationException>(() => GridSystemPlanner.PlanRadial(new RadialGridSystemInput
            {
                CenterM = new Point2(0d, 0d),
                InnerRadiusM = 0d,
                OuterRadiusM = 5d,
                Rays = new[]
                {
                    new GridAngularStation("R1", 0d),
                    new GridAngularStation("R2", Math.PI * 2d)
                },
                Rings = new[] { new GridRadialStation("C1", 2d) }
            }));
        }

        private static void PrecisionCollapsedStationOffsetFailsClosed()
        {
            Throws<OverflowException>(() => GridSystemPlanner.PlanRectangular(new RectangularGridSystemInput
            {
                OriginM = new Point2(10000000000000000d, 0d),
                UAxis = new Point2(1d, 0d),
                VAxis = new Point2(0d, 1d),
                UMinM = 0d,
                UMaxM = 2d,
                VMinM = 0d,
                VMaxM = 2d,
                UStations = new[] { new GridLinearStation("U-COLLAPSE", 1d) },
                VStations = new[] { new GridLinearStation("V-CONTROL", 0d) }
            }));
        }

        private static void RepresentableLargeStationOffsetRemainsExact()
        {
            const double originX = 10000000000000000d;
            const double expectedStationX = 10000000000000002d;
            var curves = GridSystemPlanner.PlanRectangular(new RectangularGridSystemInput
            {
                OriginM = new Point2(originX, 0d),
                UAxis = new Point2(1d, 0d),
                VAxis = new Point2(0d, 1d),
                UMinM = 0d,
                UMaxM = 2d,
                VMinM = 0d,
                VMaxM = 2d,
                UStations = new[] { new GridLinearStation("U-LARGE", 2d) },
                VStations = new[] { new GridLinearStation("V-LARGE", 0d) }
            });

            var station = curves.Single(x => x.ElementId == "U-LARGE");
            Require(station.Start.X == expectedStationX, "Representable large Grid station start coordinate changed.");
            Require(station.End.X == expectedStationX, "Representable large Grid station end coordinate changed.");
        }

        private static bool IsRayRing(string first, string second)
        {
            return (first.StartsWith("RAY-", StringComparison.OrdinalIgnoreCase) && second.StartsWith("RING-", StringComparison.OrdinalIgnoreCase)) ||
                   (second.StartsWith("RAY-", StringComparison.OrdinalIgnoreCase) && first.StartsWith("RING-", StringComparison.OrdinalIgnoreCase));
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

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
