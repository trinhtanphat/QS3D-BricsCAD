using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantitySmoke
    {
        public static void Run()
        {
            var project = new ProjectState("p", "P");
            project.Zones.Add(new ZoneDefinition("z", "Vùng-1")); project.Floors.Add(new FloorDefinition("f", "Nền 0.00", 0));
            var family = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall); project.Families.Add(family);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, "f", "z"); wall.SetQuantity("GrossVolumeM3", 3); wall.SetQuantity("NetVolumeM3", 2.6); wall.SetQuantity("LengthM", 5); wall.MarkClean(ElementDirtyFlags.All); project.Elements.Add(wall);
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 1 || rows[0].Count != 1 || Math.Abs(rows[0].NetConcreteM3 - 2.6) > 1e-12 || rows[0].ElementIds.Count != 1 || rows[0].ElementIds[0] != "W1") throw new Exception("Project quantity grouping failed.");
        }
    }
}
