using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallOpeningHostCanonicalitySmoke
    {
        public static void Run()
        {
            CanonicalHostDeductsOpeningDeterministically();
            PaddedHostFailsClosedBeforePublishingWallQuantities();
            NullProjectElementFailsClosedBeforePublishingWallQuantities();
        }

        private static void CanonicalHostDeductsOpeningDeterministically()
        {
            var project = new ProjectState("wall-host-canonical", "Canonical wall opening host");
            var wall = CreateWall("WALL-1");
            var opening = CreateOpening("OPENING-1", "WALL-1");
            project.Elements.Add(wall);
            project.Elements.Add(opening);

            new WallRegenerator().Regenerate(project, wall);

            Near(30d, wall.Quantities["GrossWallAreaM2"], "Canonical host changed gross wall area.");
            Near(2d, wall.Quantities["OpeningAreaM2"], "Canonical host did not deduct linked opening area.");
            Near(28d, wall.Quantities["NetWallAreaM2"], "Canonical host produced the wrong net wall area.");
            Near(6d, wall.Quantities["GrossVolumeM3"], "Canonical host changed gross wall volume.");
            Near(5.6d, wall.Quantities["NetVolumeM3"], "Canonical host produced the wrong net wall volume.");
        }

        private static void PaddedHostFailsClosedBeforePublishingWallQuantities()
        {
            var project = new ProjectState("wall-host-padded", "Padded wall opening host");
            var wall = CreateWall("WALL-1");
            wall.SetQuantity("NetWallAreaM2", 777d);
            wall.SetQuantity("NetVolumeM3", 888d);
            var opening = CreateOpening("OPENING-1", " WALL-1 ");
            project.Elements.Add(wall);
            project.Elements.Add(opening);

            Throws<InvalidOperationException>(() => new WallRegenerator().Regenerate(project, wall));

            Near(777d, wall.Quantities["NetWallAreaM2"], "Failed regeneration overwrote the prior net wall area.");
            Near(888d, wall.Quantities["NetVolumeM3"], "Failed regeneration overwrote the prior net wall volume.");
            Require(!wall.Quantities.ContainsKey("GrossWallAreaM2"), "Failed regeneration published a partial gross wall area.");
            Require(!wall.Quantities.ContainsKey("OpeningAreaM2"), "Failed regeneration published a partial opening area.");
        }

        private static void NullProjectElementFailsClosedBeforePublishingWallQuantities()
        {
            var project = new ProjectState("wall-host-null", "Null wall opening child");
            var wall = CreateWall("WALL-1");
            wall.SetQuantity("NetWallAreaM2", 321d);
            project.Elements.Add(wall);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => new WallRegenerator().Regenerate(project, wall));

            Near(321d, wall.Quantities["NetWallAreaM2"], "Null-child rejection overwrote the prior net wall area.");
            Require(!wall.Quantities.ContainsKey("GrossWallAreaM2"), "Null-child rejection published a partial gross wall area.");
        }

        private static ProjectElement CreateWall(string id)
        {
            var wall = new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.SetProperty("LengthM", "10");
            wall.SetProperty("HeightM", "3");
            wall.SetProperty("ThicknessM", "0.2");
            return wall;
        }

        private static ProjectElement CreateOpening(string id, string hostWallId)
        {
            var opening = new ProjectElement(id, ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            opening.SetProperty("HostWallId", hostWallId);
            opening.SetProperty("WidthM", "1");
            opening.SetProperty("HeightM", "2");
            return opening;
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
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
