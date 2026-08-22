using System;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierProfileSmoke
    {
        public static void Run()
        {
            RectangularProfileMatchesWallVolume();
            ChamferedProfileReducesAreaAndVolume();
            StraightRectangularPathMatchesLegacyPlanner();
            StraightChamferedPathMatchesLegacyPlanner();
            BentPathUsesSharedFootprintAndTerminalChamfers();
            CurrentPathSnapshotDrivesWallPierQuantity();
            StalePathSnapshotFallsBackToSemanticProfile();
            LargeFiniteStraightFootprintAvoidsSquaredLengthOverflow();
            LargeGeoreferencedChamferedPathRemainsResolvable();
            RejectsOversizedTerminalChamfer();
            RejectsSelfIntersectingPath();
            RejectsImpossibleAndNonFiniteProfiles();
        }

        private static void RectangularProfileMatchesWallVolume()
        {
            var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d
            });
            Near(0.12d, profile.CrossSectionAreaM2);
            Near(1.6d, profile.CrossSectionPerimeterM);
            Near(0.36d, profile.VolumeM3);
            Near(4.8d, profile.LateralAreaM2);
            Near(0d, profile.ChamferM);
        }

        private static void ChamferedProfileReducesAreaAndVolume()
        {
            var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.02d
            });
            Near(0.1192d, profile.CrossSectionAreaM2);
            Near(0.3576d, profile.VolumeM3);
            if (!(profile.CrossSectionPerimeterM < 1.6d)) throw new Exception("Chamfered perimeter should be shorter than the rectangular perimeter.");
            if (!(profile.LateralAreaM2 < 4.8d)) throw new Exception("Chamfered lateral area should be lower than the rectangular lateral area.");
        }

        private static void StraightRectangularPathMatchesLegacyPlanner()
        {
            var legacy = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d
            });
            var path = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(0d, 0d), new Point2(0.6d, 0d) },
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Rectangular
            });
            Near(legacy.CrossSectionAreaM2, path.FootprintAreaM2);
            Near(legacy.CrossSectionPerimeterM, path.FootprintPerimeterM);
            Near(legacy.VolumeM3, path.VolumeM3);
            Near(legacy.LateralAreaM2, path.LateralAreaM2);
            Near(0.6d, path.CenterlineLengthM);
            if (path.Polygon.Count != 4) throw new Exception("Straight rectangular WallPier path must retain four footprint corners.");
        }

        private static void StraightChamferedPathMatchesLegacyPlanner()
        {
            var legacy = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.02d
            });
            var path = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(2d, 3d), new Point2(2.6d, 3d) },
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Chamfered,
                ChamferM = 0.02d
            });
            Near(legacy.CrossSectionAreaM2, path.FootprintAreaM2);
            Near(legacy.CrossSectionPerimeterM, path.FootprintPerimeterM);
            Near(legacy.VolumeM3, path.VolumeM3);
            Near(legacy.LateralAreaM2, path.LateralAreaM2);
            if (path.Polygon.Count != 8) throw new Exception("Straight chamfered WallPier path must expose eight profile corners.");
        }

        private static void BentPathUsesSharedFootprintAndTerminalChamfers()
        {
            var centerline = new[]
            {
                new Point2(0d, 0d),
                new Point2(0.8d, 0d),
                new Point2(0.8d, 0.6d)
            };
            var shared = new WallFootprintEngine().Build(centerline, 0.2d, 4d, 1e-9d);
            var rectangular = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = centerline,
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Rectangular
            });
            Near(shared.CenterlineLength, rectangular.CenterlineLengthM);
            Near(shared.Area, rectangular.FootprintAreaM2);
            Near(shared.Perimeter, rectangular.FootprintPerimeterM);
            if (shared.UsedBevelJoin != rectangular.UsedBevelJoin) throw new Exception("WallPier path must preserve WallFootprintEngine join mode.");

            const double chamfer = 0.02d;
            var chamfered = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = centerline,
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Chamfered,
                ChamferM = chamfer
            });
            Near(rectangular.FootprintAreaM2 - 2d * chamfer * chamfer, chamfered.FootprintAreaM2);
            Near(rectangular.FootprintPerimeterM - 8d * chamfer + 4d * Math.Sqrt(2d) * chamfer, chamfered.FootprintPerimeterM);
            if (chamfered.Polygon.Count != rectangular.Polygon.Count + 4) throw new Exception("Chamfered WallPier path must replace exactly four terminal corners.");
        }

        private static void CurrentPathSnapshotDrivesWallPierQuantity()
        {
            var project = new ProjectState("wall-pier-path", "WallPier Path");
            var family = new ProjectFamily("pier-family", "Trụ chamfer", ElementCategory.WallPier);
            family.Properties["WallPierProfileMode"] = "Chamfered";
            family.Properties["WallPierChamferM"] = "0.02";
            project.Families.Add(family);
            var element = PathSnapshotElement(family.Id);
            project.Elements.Add(element);

            new WallRegenerator().Regenerate(project, element);
            Near(0.2792d, element.Quantities["WallPierProfileCrossSectionAreaM2"]);
            Near(3.25d, element.Quantities["WallPierProfilePerimeterM"]);
            Near(0.8376d, element.Quantities["WallPierProfileGrossVolumeM3"]);
            Near(9.75d, element.Quantities["WallPierProfileLateralAreaM2"]);
            Near(0.8376d, element.Quantities["GrossVolumeM3"]);
        }

        private static void StalePathSnapshotFallsBackToSemanticProfile()
        {
            var project = new ProjectState("wall-pier-stale", "WallPier Stale");
            var family = new ProjectFamily("pier-family", "Trụ chamfer", ElementCategory.WallPier);
            family.Properties["WallPierProfileMode"] = "Chamfered";
            family.Properties["WallPierChamferM"] = "0.02";
            project.Families.Add(family);
            var element = PathSnapshotElement(family.Id);
            project.Elements.Add(element);
            element.MarkDirty(ElementDirtyFlags.Properties);
            if (!element.IsGeneratedSolidStale()) throw new Exception("Generated WallPier host should be stale after semantic mutation.");

            new WallRegenerator().Regenerate(project, element);
            var fallback = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 1.4d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.02d
            });
            Near(fallback.CrossSectionPerimeterM, element.Quantities["WallPierProfilePerimeterM"]);
            if (Math.Abs(element.Quantities["WallPierProfilePerimeterM"] - 3.25d) < 1e-6d)
                throw new Exception("Stale WallPier path snapshot must not drive quantity regeneration.");
        }

        private static void LargeFiniteStraightFootprintAvoidsSquaredLengthOverflow()
        {
            var footprint = new WallFootprintEngine().Build(
                new[] { new Point2(0d, 0d), new Point2(1e200d, 0d) },
                0.2d,
                4d,
                1e-9d);
            RelativeNear(1e200d, footprint.CenterlineLength);
            RelativeNear(2e199d, footprint.Area);
            RelativeNear(2e200d, footprint.Perimeter);
            if (footprint.Polygon.Count != 4) throw new Exception("Large finite straight footprint must remain a four-corner rectangle.");
        }

        private static void LargeGeoreferencedChamferedPathRemainsResolvable()
        {
            const double origin = 1e12d;
            var profile = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(origin, origin), new Point2(origin + 0.6d, origin) },
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Chamfered,
                ChamferM = 0.02d
            });
            if (profile.Polygon.Count != 8) throw new Exception("Large georeferenced chamfered WallPier must retain four distinct terminal corners before chamfer expansion.");
            if (double.IsNaN(profile.FootprintAreaM2) || double.IsInfinity(profile.FootprintAreaM2) || !(profile.FootprintAreaM2 > 0d))
                throw new Exception("Large georeferenced WallPier area must remain finite and positive.");
            if (Math.Abs(profile.CenterlineLengthM - 0.6d) > 5e-4d)
                throw new Exception("Large georeferenced WallPier centerline length lost excessive precision.");
        }

        private static ProjectElement PathSnapshotElement(string familyId)
        {
            var element = new ProjectElement("PIER-1", ElementCategory.WallPier, familyId, string.Empty, string.Empty);
            element.Properties["LengthM"] = "1.4";
            element.Properties["HeightM"] = "3";
            element.Properties["ThicknessM"] = "0.2";
            element.Properties["GeneratedSolidHandle"] = "AB";
            element.Properties["WallPierPathProfileKind"] = "OpenPolyline";
            element.Properties["WallPierPathProfileMode"] = "Chamfered";
            element.Properties["WallPierPathProfileChamferM"] = "0.02";
            element.Properties["WallPierPathProfileCenterlineLengthM"] = "1.4";
            element.Properties["WallPierPathProfileThicknessM"] = "0.2";
            element.Properties["WallPierPathProfileHeightM"] = "3";
            element.Properties["WallPierPathProfileAreaM2"] = "0.2792";
            element.Properties["WallPierPathProfilePerimeterM"] = "3.25";
            element.Properties["WallPierPathProfileGrossVolumeM3"] = "0.8376";
            element.Properties["WallPierPathProfileLateralAreaM2"] = "9.75";
            return element;
        }

        private static void RejectsOversizedTerminalChamfer()
        {
            Throws<InvalidOperationException>(() => WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(0d, 0d), new Point2(0.6d, 0d) },
                ThicknessM = 0.2d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Chamfered,
                ChamferM = 0.1d
            }));
        }

        private static void RejectsSelfIntersectingPath()
        {
            Throws<InvalidOperationException>(() => WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1d, 1d),
                    new Point2(0d, 1d),
                    new Point2(1d, 0d)
                },
                ThicknessM = 0.1d,
                HeightM = 3d,
                Mode = WallPierProfileMode.Rectangular
            }));
        }

        private static void RejectsImpossibleAndNonFiniteProfiles()
        {
            Throws<InvalidOperationException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 0.2d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.1d
            }));
            Throws<OverflowException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                WidthM = double.NaN,
                DepthM = 0.2d,
                HeightM = 3d
            }));
            Throws<ArgumentOutOfRangeException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                WidthM = 0.6d,
                DepthM = 0d,
                HeightM = 3d
            }));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void RelativeNear(double expected, double actual, double tolerance = 1e-12d)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual)) throw new Exception("Expected finite value near " + expected + ", got " + actual + ".");
            var scale = Math.Max(1d, Math.Abs(expected));
            if (Math.Abs(expected - actual) > scale * tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
