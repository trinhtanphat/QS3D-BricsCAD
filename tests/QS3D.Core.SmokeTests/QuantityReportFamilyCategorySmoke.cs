using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportFamilyCategorySmoke
    {
        public static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingFamilyCategoryPreservesInheritance();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = NewProject("mismatch");
            var wallFamily = new ProjectFamily("F-WALL", "Wall family", ElementCategory.ArchitecturalWall);
            wallFamily.Properties["Material"] = "Wrong inherited material";
            wallFamily.Properties["DensityKgM3"] = "2400";
            project.Families.Add(wallFamily);

            var slab = new ProjectElement("S1", ElementCategory.Slab, wallFamily.Id, "F1", "Z1");
            slab.Quantities["NetVolumeM3"] = 2d;
            project.Elements.Add(slab);

            Throws<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));
            Throws<InvalidOperationException>(() => ProjectQuantityReportBuilder.Detail(project));
        }

        private static void MatchingFamilyCategoryPreservesInheritance()
        {
            var project = NewProject("matching");
            var slabFamily = new ProjectFamily("F-SLAB", "Slab family", ElementCategory.Slab);
            slabFamily.Properties["Material"] = "Concrete";
            slabFamily.Properties["DensityKgM3"] = "2400";
            project.Families.Add(slabFamily);

            var slab = new ProjectElement("S1", ElementCategory.Slab, slabFamily.Id, "F1", "Z1");
            slab.Quantities["NetVolumeM3"] = 2d;
            project.Elements.Add(slab);

            var row = ProjectQuantityReportBuilder.Detail(project)[0];
            Equal("F-SLAB", row.FamilyId);
            Equal("Concrete", row.Material);
            Near(2400d, row.DensityKgM3 ?? double.NaN);
            Near(4800d, row.MassKg ?? double.NaN);
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
