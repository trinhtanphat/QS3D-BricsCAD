using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Revisions;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedDomainSmoke
    {
        public static void Run()
        {
            MaterialCatalogRevisionSemantics();
            MaterialCatalogUsesLastAvailableRevision();
            StructuralQuantities();
            StructuralOpeningDeduction();
            FixedPointRegeneration();
            GenericTakeoff();
            RebarSchedule();
            ProjectRebarSchedule();
            RebarXlsxExport();
            DetailedRevision();
            AdvancedHealth();
        }

        private static void MaterialCatalogRevisionSemantics()
        {
            var project = new ProjectState("material-catalog-revision", "Material catalog revision");
            var beforeUpsertVersion = project.ChangeVersion;
            ProjectMaterialCatalog.UpsertCustom(project, "custom-1", "Custom One", "m2", "Initial");
            Equal(beforeUpsertVersion + 1L, project.ChangeVersion);
            Equal(1, ProjectMaterialCatalog.GetCustom(project).Count);
            Equal("Custom One", ProjectMaterialCatalog.GetCustom(project)[0].Name);

            var afterUpsertVersion = project.ChangeVersion;
            var afterUpsertUpdatedUtc = project.UpdatedUtc;
            ProjectMaterialCatalog.UpsertCustom(project, "custom-1", "Custom One", "m2", "Initial");
            Equal(afterUpsertVersion, project.ChangeVersion);
            Equal(afterUpsertUpdatedUtc, project.UpdatedUtc);

            var beforeDeleteVersion = project.ChangeVersion;
            True(ProjectMaterialCatalog.DeleteCustom(project, "custom-1"));
            Equal(beforeDeleteVersion + 1L, project.ChangeVersion);
            Equal(0, ProjectMaterialCatalog.GetCustom(project).Count);
            True(!project.Metadata.ContainsKey(ProjectMaterialCatalog.MetadataKey));
        }

        private static void MaterialCatalogUsesLastAvailableRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-material-catalog-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("material-catalog-ceiling", "Material catalog ceiling"), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for material catalog revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(System.Globalization.CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var project = store.Load(path);
                Equal(long.MaxValue - 1L, project.ChangeVersion);
                True(!project.Metadata.ContainsKey(ProjectMaterialCatalog.MetadataKey));

                ProjectMaterialCatalog.UpsertCustom(project, "custom-ceiling", "Ceiling material", "m2", "Initial");

                Equal(long.MaxValue, project.ChangeVersion);
                Equal(1, ProjectMaterialCatalog.GetCustom(project).Count);
                Equal("Ceiling material", ProjectMaterialCatalog.GetCustom(project)[0].Name);
                True(project.Metadata.ContainsKey(ProjectMaterialCatalog.MetadataKey));

                var beforeRejectedUpdatedUtc = project.UpdatedUtc;
                var beforeRejectedMetadata = project.Metadata[ProjectMaterialCatalog.MetadataKey];
                var rejected = false;
                try
                {
                    ProjectMaterialCatalog.UpsertCustom(project, "custom-ceiling", "Ceiling material", "m2", "Changed description");
                }
                catch (OverflowException)
                {
                    rejected = true;
                }

                True(rejected);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[ProjectMaterialCatalog.MetadataKey]);
                Equal("Initial", ProjectMaterialCatalog.GetCustom(project)[0].Description);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void StructuralQuantities()
        {
            var project = NewProject(); var regenerator = new StructuralRegenerator();

            var beam = new ProjectElement("B1", ElementCategory.Beam, "", "f", "z");
            beam.Properties["LengthM"] = "5"; beam.Properties["WidthM"] = "0.3"; beam.Properties["HeightM"] = "0.5";
            regenerator.Regenerate(project, beam);
            Near(0.75, beam.Quantities["NetVolumeM3"]); Near(6.5, beam.Quantities["FormworkM2"]);

            var slab = new ProjectElement("S1", ElementCategory.Slab, "", "f", "z");
            slab.Properties["AreaM2"] = "20"; slab.Properties["OpeningAreaM2"] = "2"; slab.Properties["ThicknessM"] = "0.12"; slab.Properties["PerimeterM"] = "18";
            regenerator.Regenerate(project, slab);
            Near(2.16, slab.Quantities["NetVolumeM3"]); Near(20.16, slab.Quantities["FormworkM2"]);

            var column = new ProjectElement("C1", ElementCategory.Column, "", "f", "z");
            column.Properties["WidthM"] = "0.4"; column.Properties["DepthM"] = "0.4"; column.Properties["HeightM"] = "3.6";
            regenerator.Regenerate(project, column);
            Near(0.576, column.Quantities["NetVolumeM3"]); Near(5.76, column.Quantities["FormworkM2"]);

            var foundation = new ProjectElement("F1", ElementCategory.Foundation, "", "f", "z");
            foundation.Properties["AreaM2"] = "6"; foundation.Properties["ThicknessM"] = "0.5"; foundation.Properties["PerimeterM"] = "10";
            regenerator.Regenerate(project, foundation);
            Near(3, foundation.Quantities["NetVolumeM3"]); Near(5, foundation.Quantities["FormworkM2"]);

            var earth = new ProjectElement("E1", ElementCategory.Earthwork, "", "f", "z");
            earth.Properties["AreaM2"] = "12"; earth.Properties["DepthM"] = "1.5";
            regenerator.Regenerate(project, earth);
            Near(18, earth.Quantities["NetVolumeM3"]);
        }

        private static void StructuralOpeningDeduction()
        {
            var project = NewProject();
            var wall = new ProjectElement("SW1", ElementCategory.StructuralWall, "", "f", "z"); wall.Properties["LengthM"] = "5"; wall.Properties["HeightM"] = "3"; wall.Properties["ThicknessM"] = "0.2"; project.Elements.Add(wall);
            var opening = new ProjectElement("O1", ElementCategory.WallOpening, "", "f", "z"); opening.Properties["WidthM"] = "0.9"; opening.Properties["HeightM"] = "2.2"; project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening);
            new StructuralRegenerator().Regenerate(project, wall);
            Near(13.02, wall.Quantities["NetWallAreaM2"]); Near(2.604, wall.Quantities["NetVolumeM3"]); Near(26.04, wall.Quantities["FormworkM2"]);
        }

        private static void FixedPointRegeneration()
        {
            var project = NewProject();
            var wall = new ProjectElement("W-FIX", ElementCategory.ArchitecturalWall, "", "f", "z"); wall.Properties["LengthM"] = "5"; wall.Properties["HeightM"] = "3"; wall.Properties["ThicknessM"] = "0.2"; project.Elements.Add(wall);
            var opening = new ProjectElement("O-FIX", ElementCategory.WallOpening, "", "f", "z"); opening.Properties["WidthM"] = "0.9"; opening.Properties["HeightM"] = "2.2"; project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening); opening.MarkClean(ElementDirtyFlags.All);
            new WallRegenerator().Regenerate(project, wall); wall.MarkClean(ElementDirtyFlags.All);
            Near(2.604, wall.Quantities["NetVolumeM3"]);

            opening.SetProperty("WidthM", "1.2");
            True(opening.Dirty != ElementDirtyFlags.None); Equal(ElementDirtyFlags.None, wall.Dirty);
            var count = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
            True(count >= 2); Near(2.472, wall.Quantities["NetVolumeM3"]); Equal(ElementDirtyFlags.None, opening.Dirty); Equal(ElementDirtyFlags.None, wall.Dirty);
        }

        private static void GenericTakeoff()
        {
            var element = new ProjectElement("Q1", ElementCategory.CustomQuantity, "", "f", "z"); element.Properties["LengthM"] = "2.5"; element.Properties["AreaM2"] = "4.2";
            new GenericTakeoffRegenerator().Regenerate(NewProject(), element);
            Near(2.5, element.Quantities["LengthM"]); Near(4.2, element.Quantities["AreaM2"]); Near(1, element.Quantities["Count"]);
        }

        private static void RebarSchedule()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "B1", BarMark = "B1-MAIN", ShapeCode = "00", Notation = "4D20", CuttingLengthM = 5.5, WastePercent = 3 },
                new RebarScheduleInput { ElementId = "S1", BarMark = "S1-DIST", ShapeCode = "00", Notation = "D8@150", CuttingLengthM = 4, DistributionLengthM = 3 }
            });
            Equal(2, rows.Count); Equal(4, rows[0].Quantity); Near(22, rows[0].TotalLengthM); Equal(21, rows[1].Quantity); Near(84, rows[1].TotalLengthM);
            Near(rows[0].NetWeightKg * 1.03, rows[0].TotalWeightKg);
        }

        private static void ProjectRebarSchedule()
        {
            var project = NewProject();
            var beam = new ProjectElement("B2", ElementCategory.Beam, "", "f", "z");
            beam.Properties["LengthM"] = "6"; beam.Properties["RebarNotation"] = "2D18+2D20"; beam.Properties["RebarBarMark"] = "B2"; beam.Properties["RebarWastePercent"] = "2";
            project.Elements.Add(beam);
            var rows = ProjectRebarScheduleBuilder.Build(project);
            Equal(2, rows.Count); Equal("B2-1", rows[0].BarMark); Equal("B2-2", rows[1].BarMark); Equal(2, rows[0].Quantity); Near(12, rows[0].TotalLengthM);
        }

        private static void RebarXlsxExport()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-bbs-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var rows = RebarScheduleBuilder.Build(new[] { new RebarScheduleInput { ElementId = "B1", BarMark = "M1", Notation = "4D20", CuttingLengthM = 5 } });
                XlsxRebarScheduleExporter.Export(path, rows);
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Missing BBS worksheet.");
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                    {
                        var xml = reader.ReadToEnd();
                        True(xml.Contains("Bar Mark")); True(xml.Contains("KL tổng (kg)")); True(xml.Contains("state=\"frozen\"")); True(xml.Contains("autoFilter"));
                    }
                }
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }

        private static void DetailedRevision()
        {
            var project = NewProject(); var element = new ProjectElement("R1", ElementCategory.Beam, "fam-a", "f", "z"); element.Properties["Material"] = "C30"; element.SetQuantity("NetVolumeM3", 1.2); project.Elements.Add(element);
            var service = new RevisionService(); var before = service.Capture(project, "A");
            element.Properties["Material"] = "C35"; element.FloorId = "f2"; element.SetQuantity("NetVolumeM3", 1.3);
            var after = service.Capture(project, "B"); var delta = service.Compare(before, after).Single();
            Equal("Changed", delta.Change); True(delta.Fields.Any(x => x.Field == "Property:Material")); True(delta.Fields.Any(x => x.Field == "FloorId")); True(delta.Fields.Any(x => x.Field == "Quantity:NetVolumeM3"));
        }

        private static void AdvancedHealth()
        {
            var project = NewProject(); var family = new ProjectFamily("beam", "Dầm", ElementCategory.Beam); project.Families.Add(family);
            var beam = new ProjectElement("B3", ElementCategory.Beam, family.Id, "f", "z"); beam.Properties["RebarNotation"] = "bad"; project.Elements.Add(beam);
            var issues = new ModelHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "MISSING_MATERIAL" && x.ElementId == "B3")); True(issues.Any(x => x.Code == "INVALID_REBAR" && x.ElementId == "B3"));
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Advanced"); project.Zones.Add(new ZoneDefinition("z", "Vùng-1")); project.Floors.Add(new FloorDefinition("f", "Nền 0.00", 0)); project.Floors.Add(new FloorDefinition("f2", "Tầng 2", 3.6)); project.ActiveZoneId = "z"; project.ActiveFloorId = "f"; return project;
        }

        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    }
}
