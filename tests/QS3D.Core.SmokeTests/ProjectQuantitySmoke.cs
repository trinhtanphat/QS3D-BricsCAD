using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantitySmoke
    {
        public static void Run()
        {
            var project = new ProjectState("p", "P");
            project.Zones.Add(new ZoneDefinition("z", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("f", "Nền 0.00", 0));
            var wallFamily = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall); project.Families.Add(wallFamily);
            var openingFamily = new ProjectFamily("opening", "Lỗ Mở", ElementCategory.WallOpening); project.Families.Add(openingFamily);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, wallFamily.Id, "f", "z"); wall.Properties["LengthM"]="5"; wall.Properties["HeightM"]="3"; wall.Properties["ThicknessM"]="0.2"; project.Elements.Add(wall);
            var opening = new ProjectElement("O1", ElementCategory.WallOpening, openingFamily.Id, "f", "z"); opening.Properties["WidthM"]="0.9"; opening.Properties["HeightM"]="2.2"; project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening); opening.MarkClean(ElementDirtyFlags.All);
            new WallRegenerator().Regenerate(project, wall); wall.MarkClean(ElementDirtyFlags.All);
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 2) throw new Exception("Project quantity grouping failed.");
            var wallRow = rows[0].Category == ElementCategory.ArchitecturalWall.ToString() ? rows[0] : rows[1];
            if (wallRow.ElementIds.Count != 1 || wallRow.ElementIds[0] != "W1" || Math.Abs(wallRow.NetConcreteM3 - 2.604) > 1e-12) throw new Exception("Linked opening deduction/report failed.");
            if (!opening.Properties.TryGetValue("HostWallId", out var host) || host != "W1" || opening.DependsOn.Count != 1) throw new Exception("Host link failed.");
        }
    }
}
