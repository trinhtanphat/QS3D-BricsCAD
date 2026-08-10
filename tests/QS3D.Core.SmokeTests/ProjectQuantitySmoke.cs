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
            DetailRowsPreserveOneElementProvenance();
            MeasuredSolidMassOverridesDefaultPrismVolume();
            PreferredBqQuantityDoesNotEvaluateUnusedFallbacks();
            WallFinishPrefersRegeneratedNetArea();
        }

        private static void DetailRowsPreserveOneElementProvenance()
        {
            var project = new ProjectState("detail", "ED2 detail") { DrawingFingerprint = "DETAIL-FP" };
            project.Floors.Add(new FloorDefinition("f", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone A"));
            var family = new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            var first = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, "f", "z");
            first.SourceHandles.Add("A1");
            first.Quantities["NetConcreteM3"] = 1.25d;
            first.Quantities["GrossConcreteM3"] = 1.5d;
            var second = new ProjectElement("W2", ElementCategory.ArchitecturalWall, family.Id, "f", "z");
            second.SourceHandles.Add("A2");
            second.Quantities["NetConcreteM3"] = 2.25d;
            second.Quantities["GrossConcreteM3"] = 2.5d;
            project.Elements.Add(first);
            project.Elements.Add(second);

            var detail = ProjectQuantityReportBuilder.Detail(project);
            if (detail.Count != 2 || detail.Any(x => x.Count != 1 || x.ElementIds.Count != 1 || x.SourceHandles.Count != 1))
                throw new Exception("ED2 detail must retain exactly one semantic element and its source Handle per row.");
            if (detail[0].ElementIds[0] != "W1" || detail[0].SourceHandles[0] != "A1" || detail[0].DrawingFingerprint != "DETAIL-FP")
                throw new Exception("ED2 detail provenance/order failed.");
            if (detail[0].Zone != "Zone A") throw new Exception("ED2 detail must expose the semantic Zone.");

            var selectedDetail = ProjectQuantityReportBuilder.Detail(project, new[] { " w2 " });
            var selectedSummary = ProjectQuantityReportBuilder.Group(project, new[] { "W2" });
            if (selectedDetail.Count != 1 || selectedDetail[0].ElementIds.Single() != "W2" || selectedSummary.Single().Count != 1)
                throw new Exception("ED2 selected semantic scope failed.");

            project.Zones.Add(new ZoneDefinition("z2", "Zone B"));
            var third = new ProjectElement("W3", ElementCategory.ArchitecturalWall, family.Id, "f", "z2");
            third.SourceHandles.Add("A3");
            project.Elements.Add(third);
            var grouped = ProjectQuantityReportBuilder.Group(project);
            if (grouped.Count != 2 || grouped.Any(x => string.IsNullOrWhiteSpace(x.Zone)))
                throw new Exception("BQ/ED2 summary must not merge the same Floor/Family across different Zones.");

            try { ProjectQuantityReportBuilder.Detail(project, new[] { "missing" }); throw new Exception("Unknown ED2 element id must fail closed."); }
            catch (System.Collections.Generic.KeyNotFoundException) { }
            try { ProjectQuantityReportBuilder.Group(project, new[] { " " }); throw new Exception("Blank ED2 element id must fail closed."); }
            catch (ArgumentException) { }
        }

        private static void MeasuredSolidMassOverridesDefaultPrismVolume()
        {
            var project = new ProjectState("solid-mass", "Measured Solid");
            project.Floors.Add(new FloorDefinition("f", "Tầng", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            var family = new ProjectFamily("slab", "Sàn", ElementCategory.Slab);
            project.Families.Add(family);
            var slab = new ProjectElement("S1", ElementCategory.Slab, family.Id, "f", "z");
            slab.Properties["AreaM2"] = "20";
            slab.Properties["ThicknessM"] = "0.12";
            slab.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "1.75";
            slab.Properties[MeasuredSolidQuantityPolicy.SurfaceAreaProperty] = "55";
            project.Elements.Add(slab);

            var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
            if (regenerated != 1 || Math.Abs(slab.Quantities["GrossVolumeM3"] - 1.75d) > 1e-12 || Math.Abs(slab.Quantities["NetVolumeM3"] - 1.75d) > 1e-12)
                throw new Exception("Measured Solid3d volume must override the default Area × Thickness prism estimate.");
            var report = ProjectQuantityReportBuilder.Group(project).Single();
            if (Math.Abs(report.GrossConcreteM3 - 1.75d) > 1e-12 || Math.Abs(report.NetConcreteM3 - 1.75d) > 1e-12 || Math.Abs(report.OtherAreaM2 - 55d) > 1e-12)
                throw new Exception("Measured Solid3d volume/surface area did not reach the BQ report.");
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
