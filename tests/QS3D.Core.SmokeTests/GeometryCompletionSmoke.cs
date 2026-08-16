using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class GeometryCompletionSmoke
    {
        public static void Run()
        {
            StableDistanceAndPolylineMetrics();
            RoomBoundaryLargeCoordinates();
            StraightWallFootprint();
            PolylineWallCorner();
            FarOriginWallFootprint();
            WallFootprintRejectsSelfIntersection();
            OpeningCutPlan();
            OpeningCutRejectsInvalidPlacement();
            RectangularRebarLayout();
            RectangularRebarRejectsImpossibleCover();
            GeneratedRebarHealth();
        }

        private static void StableDistanceAndPolylineMetrics()
        {
            var distance = new Point2(0d, 0d).DistanceTo(new Point2(3e200d, 4e200d));
            NearRelative(5e200d, distance);
            Throws<OverflowException>(() => new Point2(double.MaxValue, 0d).DistanceTo(new Point2(-double.MaxValue, 0d)));

            Equal(5d, PolylineMetrics.Length(new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 4d)
            }, false));
            Equal(12d, PolylineMetrics.Length(new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 4d)
            }, true));
            Equal(10000000000000002d, PolylineMetrics.Length(new[]
            {
                new Point2(0d, 0d),
                new Point2(1e16d, 0d),
                new Point2(1e16d, 1d),
                new Point2(1e16d, 2d)
            }, false));

            const double origin = 1e12d;
            var polygon = new[]
            {
                new Point2(origin, origin),
                new Point2(origin + 10d, origin),
                new Point2(origin + 10d, origin + 20d),
                new Point2(origin, origin + 20d)
            };
            Near(200d, PolylineMetrics.Area(polygon));
            Near(60d, PolylineMetrics.Length(polygon, true));

            Throws<OverflowException>(() => PolylineMetrics.Area(new[]
            {
                new Point2(0d, 0d),
                new Point2(1e308d, 0d),
                new Point2(0d, 1e308d)
            }));
        }

        private static void RoomBoundaryLargeCoordinates()
        {
            const double origin = 1e12d;
            var segments = new[]
            {
                new BoundarySegment(new Point2(origin, origin), new Point2(origin + 10d, origin), "A"),
                new BoundarySegment(new Point2(origin + 10d, origin), new Point2(origin + 10d, origin + 20d), "B"),
                new BoundarySegment(new Point2(origin + 10d, origin + 20d), new Point2(origin, origin + 20d), "C"),
                new BoundarySegment(new Point2(origin, origin + 20d), new Point2(origin, origin), "D")
            };
            var rooms = new RoomBoundaryEngine().Discover(segments, 0.005d, 1d);
            Equal(1, rooms.Count);
            Near(200d, rooms[0].Area);
            Near(60d, rooms[0].Perimeter);
        }

        private static void StraightWallFootprint()
        {
            var result = new WallFootprintEngine().Build(new[] { new Point2(0, 0), new Point2(5, 0) }, 0.2d);
            Equal(4, result.Polygon.Count);
            Near(5d, result.CenterlineLength);
            Near(1d, result.Area);
            Near(10.4d, result.Perimeter);
            True(!result.UsedBevelJoin);
        }

        private static void PolylineWallCorner()
        {
            var result = new WallFootprintEngine().Build(new[]
            {
                new Point2(0, 0), new Point2(5, 0), new Point2(5, 3)
            }, 0.2d);
            Near(8d, result.CenterlineLength);
            Near(1.6d, result.Area);
            Near(16.4d, result.Perimeter);
            True(result.Polygon.All(p => Finite(p.X) && Finite(p.Y)));
        }

        private static void FarOriginWallFootprint()
        {
            const double origin = 1_000_000_000d;
            var result = new WallFootprintEngine().Build(new[]
            {
                new Point2(origin, origin), new Point2(origin + 5d, origin), new Point2(origin + 5d, origin + 3d)
            }, 0.2d);
            Near(8d, result.CenterlineLength, 1e-8d);
            Near(1.6d, result.Area, 1e-7d);
            Near(16.4d, result.Perimeter, 1e-7d);
        }

        private static void WallFootprintRejectsSelfIntersection()
        {
            Throws<InvalidOperationException>(() => new WallFootprintEngine().Build(new[]
            {
                new Point2(0, 0), new Point2(2, 2), new Point2(0, 2), new Point2(2, 0)
            }, 0.2d));
            Throws<ArgumentOutOfRangeException>(() => new WallFootprintEngine().Build(new[]
            {
                new Point2(0, 0), new Point2(1, 0)
            }, double.NaN));
        }

        private static void OpeningCutPlan()
        {
            var plan = OpeningCutPlanner.Plan(new OpeningCutInput
            {
                HostLengthM = 5d,
                HostThicknessM = 0.2d,
                HostHeightM = 3d,
                OpeningWidthM = 0.9d,
                OpeningHeightM = 2.2d,
                SillHeightM = 0d,
                CenterAlongHostM = 2.5d,
                ClearanceM = 0.01d
            });
            Near(2.05d, plan.StartAlongHostM);
            Near(2.95d, plan.EndAlongHostM);
            Near(0.92d, plan.CutterWidthM);
            Near(0.22d, plan.CutterDepthM);
            Near(2.22d, plan.CutterHeightM);
            Near(-0.01d, plan.BaseElevationM);
            Near(2.21d, plan.TopElevationM);
            Near(1.1d, plan.CenterElevationM);
            True(Finite(plan.CenterElevationM));
        }

        private static void OpeningCutRejectsInvalidPlacement()
        {
            Throws<InvalidOperationException>(() => OpeningCutPlanner.Plan(new OpeningCutInput
            {
                HostLengthM = 5d, HostThicknessM = 0.2d, HostHeightM = 3d,
                OpeningWidthM = 1d, OpeningHeightM = 2d, CenterAlongHostM = 0.2d
            }));
            Throws<InvalidOperationException>(() => OpeningCutPlanner.Plan(new OpeningCutInput
            {
                HostLengthM = 5d, HostThicknessM = 0.2d, HostHeightM = 3d,
                OpeningWidthM = 1d, OpeningHeightM = 2.2d, SillHeightM = 1d, CenterAlongHostM = 2.5d
            }));
        }

        private static void RectangularRebarLayout()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.4d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 4,
                BarsAlongDepth = 4
            });
            Equal(12, layout.BarCenters.Count);
            Near(0.15d, layout.ClearHalfWidthM);
            Near(0.15d, layout.ClearHalfDepthM);
            True(layout.BarCenters.Any(p => Math.Abs(p.X + 0.15d) < 1e-12d && Math.Abs(p.Y + 0.15d) < 1e-12d));
            True(layout.BarCenters.All(p => Finite(p.X) && Finite(p.Y)));

            var fourCorners = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.3d, DepthM = 0.5d, CoverM = 0.03d, DiameterMm = 16d,
                BarsAlongWidth = 2, BarsAlongDepth = 2
            });
            Equal(4, fourCorners.BarCenters.Count);
        }

        private static void RectangularRebarRejectsImpossibleCover()
        {
            Throws<InvalidOperationException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.2d, DepthM = 0.2d, CoverM = 0.1d, DiameterMm = 20d,
                BarsAlongWidth = 2, BarsAlongDepth = 2
            }));
        }

        private static void GeneratedRebarHealth()
        {
            var project = new ProjectState("P", "P");
            var first = new ProjectElement("C1", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            first.Properties["GeneratedRebarHandles"] = "AA;BB";
            first.Properties["GeneratedRebarCount"] = "2";
            first.Properties["GeneratedRebarDiameterMm"] = "20";
            first.Properties["GeneratedShapeRebarHandles"] = "CC;DD";
            first.Properties["GeneratedShapeRebarCount"] = "2";
            project.Elements.Add(first);

            var columnHealth = new GeneratedRebarHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            True(columnHealth.Any(x => x.Code == "REBAR_GENERATED_SOLID_MISSING"));
            True(!columnHealth.Any(x => x.Code == "SHAPE_REBAR_GENERATED_SOLID_MISSING"));

            var shapeHealth = new GeneratedRebarHealthService().InspectShape(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CC" });
            True(shapeHealth.Any(x => x.Code == "SHAPE_REBAR_GENERATED_SOLID_MISSING"));

            var second = new ProjectElement("C2", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            second.Properties["GeneratedRebarHandles"] = "AA";
            second.Properties["GeneratedRebarCount"] = "1";
            second.Properties["GeneratedRebarDiameterMm"] = "16";
            second.Properties["GeneratedShapeRebarHandles"] = "CC";
            second.Properties["GeneratedShapeRebarCount"] = "1";
            project.Elements.Add(second);
            var conflict = new GeneratedRebarHealthService().InspectAll(project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA", "BB" },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CC", "DD" });
            True(conflict.Any(x => x.Code == "REBAR_GENERATED_OWNERSHIP_CONFLICT"));
            True(conflict.Any(x => x.Code == "SHAPE_REBAR_GENERATED_OWNERSHIP_CONFLICT"));
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Near(double expected, double actual, double tolerance = 1e-9d) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void NearRelative(double expected, double actual, double relativeTolerance = 1e-12d)
        {
            var scale = Math.Max(Math.Abs(expected), Math.Abs(actual));
            if (scale == 0d) return;
            if (Math.Abs(expected - actual) / scale > relativeTolerance) throw new Exception("Expected " + expected + ", got " + actual);
        }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
