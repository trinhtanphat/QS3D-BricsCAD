using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageFamilyCategorySmoke
    {
        public static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingFamilyCategoryPreservesMaterialInheritance();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = NewProject("material-mismatch");
            var wallFamily = new ProjectFamily("F-WALL", "Wall family", ElementCategory.ArchitecturalWall);
            wallFamily.Properties["Material"] = "Wrong material";
            project.Families.Add(wallFamily);

            var slab = new ProjectElement("S1", ElementCategory.Slab, wallFamily.Id, "F1", "Z1");
            slab.Quantities["NetVolumeM3"] = 2d;
            project.Elements.Add(slab);

            Throws<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project));
        }

        private static void MatchingFamilyCategoryPreservesMaterialInheritance()
        {
            var project = NewProject("material-matching");
            var slabFamily = new ProjectFamily("F-SLAB", "Slab family", ElementCategory.Slab);
            slabFamily.Properties["Material"] = "Concrete";
            project.Families.Add(slabFamily);

            var slab = new ProjectElement("S1", ElementCategory.Slab, slabFamily.Id, "F1", "Z1");
            slab.Quantities["NetVolumeM3"] = 2d;
            slab.SourceHandles.Add("AA1");
            project.Elements.Add(slab);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            Equal("Concrete", row.MaterialName);
            Equal("Slab family", row.FamilyName);
            Equal("Slab", row.Category);
            Equal(1, row.ElementCount);
            Near(2d, row.VolumeM3);
            Equal("S1", row.ElementIds.Single());
            Equal("AA1", row.SourceHandles.Single());
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
