using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantitySmoke
    {
        public static void Run()
        {
            LinkedOpeningReport();
            PreferredBqQuantityDoesNotEvaluateUnusedFallbacks();
            WallFinishPrefersRegeneratedNetArea();
        }

        private static void LinkedOpeningReport()
        {
            var project = new ProjectState("p", "P");
            project.DrawingFingerprint = "DWG-FP";
            project.Zones.Add(new ZoneDefinition("z", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("f", "Nền 0.00", 0));
            var wallFamily = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall); project.Families.Add(wallFamily);
            var openingFamily = new ProjectFamily("opening", "Lỗ Mở", ElementCategory.WallOpening); project.Families.Add(openingFamily);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, wallFamily.Id, "f", "z"); wall.Properties["LengthM"]="5"; wall.Properties["HeightM"]="3"; wall.Properties["ThicknessM"]="0.2"; wall.SourceHandles.Add("AB12"); project.Elements.Add(wall);
            var opening = new ProjectElement("O1", ElementCategory.WallOpening, openingFamily.Id, "f", "z"); opening.Properties["WidthM"]="0.9"; opening.Properties["HeightM"]="2.2"; project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening); opening.MarkClean(ElementDirtyFlags.All);
            new WallRegenerator().Regenerate(project, wall); wall.MarkClean(ElementDirtyFlags.All);
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 2) throw new Exception("Project quantity grouping failed.");
            var wallRow = rows[0].Category == ElementCategory.ArchitecturalWall.ToString() ? rows[0] : rows[1];
            if (wallRow.DrawingFingerprint != "DWG-FP" || wallRow.ElementIds.Count != 1 || wallRow.ElementIds[0] != "W1" || wallRow.SourceHandles.Count != 1 || wallRow.SourceHandles[0] != "AB12" || Math.Abs(wallRow.NetConcreteM3 - 2.604) > 1e-12) throw new Exception("Linked opening deduction/report failed.");
            if (!opening.Properties.TryGetValue("HostWallId", out var host) || host != "W1" || opening.DependsOn.Count != 1) throw new Exception("Host link failed.");
        }

        private static void PreferredBqQuantityDoesNotEvaluateUnusedFallbacks()
        {
            var project = new ProjectState("p2", "BQ fallback");
            project.Floors.Add(new FloorDefinition("f", "Tầng", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Vùng"));
            var family = new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            var wall = new ProjectElement("W-PREFERRED", ElementCategory.ArchitecturalWall, family.Id, "f", "z");
            wall.Quantities["GrossConcreteM3"] = 2d;
            wall.Quantities["GrossVolumeM3"] = double.NaN;
            wall.Quantities["NetConcreteM3"] = 1.5d;
            wall.Quantities["NetVolumeM3"] = double.PositiveInfinity;
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var row = ProjectQuantityReportBuilder.Group(project)[0];
            if (Math.Abs(row.GrossConcreteM3 - 2d) > 1e-12 || Math.Abs(row.NetConcreteM3 - 1.5d) > 1e-12 || Math.Abs(row.DeductionM3 - 0.5d) > 1e-12)
                throw new Exception("BQ must use preferred quantities lazily without evaluating invalid unused legacy fallbacks.");
        }

        private static void WallFinishPrefersRegeneratedNetArea()
        {
            var project = new ProjectState("p3", "Wall finish BQ precedence");
            project.Floors.Add(new FloorDefinition("f", "Tầng", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Vùng"));
            var family = new ProjectFamily("wf", "Sơn tường", ElementCategory.WallFinish);
            project.Families.Add(family);
            var finish = new ProjectElement("WF-NET", ElementCategory.WallFinish, family.Id, "f", "z");
            finish.Quantities["SideAreaM2"] = 99d;
            finish.Quantities["NetFinishAreaM2"] = 12.5d;
            finish.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(finish);

            var row = ProjectQuantityReportBuilder.Group(project).Single();
            if (Math.Abs(row.SideAreaM2 - 12.5d) > 1e-12)
                throw new Exception("WallFinish BQ must prefer regenerated NetFinishAreaM2 over legacy/raw SideAreaM2.");
        }
    }
}
