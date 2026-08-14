using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleFamilyCategorySmoke
    {
        public static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingFamilyCategoryPreservesProjection();
            MissingFamilyFailsClosed();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = NewProject("curtain-mismatch");
            var wallFamily = new ProjectFamily("WALL", "Architectural wall family", ElementCategory.ArchitecturalWall);
            project.Families.Add(wallFamily);
            project.Elements.Add(new ProjectElement("CW1", ElementCategory.GlassWall, wallFamily.Id, "F1", "Z1"));

            Throws<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
        }

        private static void MatchingFamilyCategoryPreservesProjection()
        {
            var project = NewProject("curtain-matching");
            var curtainFamily = new ProjectFamily("CW-FAMILY", "Curtain family", ElementCategory.GlassWall);
            project.Families.Add(curtainFamily);

            var curtain = new ProjectElement("CW1", ElementCategory.GlassWall, curtainFamily.Id, "F1", "Z1");
            curtain.Quantities["LengthM"] = 4d;
            curtain.Quantities["CurtainNetGlassAreaM2"] = 10d;
            curtain.SourceHandles.Add("C1");
            project.Elements.Add(curtain);

            var row = CurtainWallScheduleBuilder.Build(project).Single();
            Equal("Curtain family", row.FamilyName);
            Equal(1, row.WallCount);
            Near(4d, row.TotalWallLengthM);
            Near(10d, row.NetGlassAreaM2);
            Equal("CW1", row.ElementIds.Single());
            Equal("C1", row.SourceHandles.Single());
        }

        private static void MissingFamilyFailsClosed()
        {
            var project = NewProject("curtain-missing-family");
            project.Elements.Add(new ProjectElement("CW1", ElementCategory.GlassWall, "MISSING", "F1", "Z1"));

            Throws<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
        }

        private static ProjectState NewProject(string id)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
