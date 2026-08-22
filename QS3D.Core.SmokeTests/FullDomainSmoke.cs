using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class FullDomainSmoke
    {
        public static void Run()
        {
            StructuralCalculators(); StructuralRegeneration(); ColumnFootprintRegeneration(); GenericQuantity();
            RebarSchedule(); RebarSpacingAndStirrup(); RebarRegeneration(); RebarQuantityReport();
            RecognitionStrongAndAmbiguous(); RevisionQuantityDelta(); RevisionStoreRoundtrip(); RebarCsv();
        }

        private static void StructuralCalculators()
        {
            var beam = StructuralQuantityCalculator.Beam(5d, .2d, .4d); Near(.4d, beam.NetVolumeM3); Near(5.16d, beam.FormworkM2);
            var column = StructuralQuantityCalculator.Column(.3d, .4d, 3d); Near(.36d, column.NetVolumeM3); Near(4.2d, column.FormworkM2);
            var slab = StructuralQuantityCalculator.Slab(20d, 18d, .12d); Near(2.4d, slab.NetVolumeM3); Near(22.16d, slab.FormworkM2);
            var wall = StructuralQuantityCalculator.StructuralWall(5d, 3d, .2d); Near(3d, wall.NetVolumeM3); Near(31.2d, wall.FormworkM2);
            var footing = StructuralQuantityCalculator.Foundation(2d, 1.5d, .5d); Near(1.5d, footing.NetVolumeM3); Near(3.5d, footing.FormworkM2);
            var earth = StructuralQuantityCalculator.Earthwork(20d, .5d, .15d); Near(10d, earth.NetVolumeM3); Near(11.5d, earth.LooseVolumeM3);
        }

        private static void StructuralRegeneration()
        {
            var project = NewProject();
            var beam = new ProjectElement("B1", ElementCategory.Beam, "", "f", "z"); beam.Properties["LengthM"] = "5"; beam.Properties["WidthM"] = "0.2"; beam.Properties["HeightM"] = "0.4"; new StructuralRegenerator().Regenerate(project, beam); Near(.4d, beam.Quantities["NetConcreteM3"]); Near(5.16d, beam.Quantities["FormworkM2"]);
            var slab = new ProjectElement("S1", ElementCategory.Slab, "", "f", "z"); slab.Properties["AreaM2"] = "20"; slab.Properties["PerimeterM"] = "18"; slab.Properties["ThicknessM"] = "0.12"; new StructuralRegenerator().Regenerate(project, slab); Near(2.4d, slab.Quantities["NetConcreteM3"]); Near(20d, slab.Quantities["BottomAreaM2"]);
            var excavation = new ProjectElement("E1", ElementCategory.Earthwork, "", "f", "z"); excavation.Properties["AreaM2"] = "20"; excavation.Properties["DepthM"] = "0.5"; excavation.Properties["SwellFactor"] = "0.15"; new StructuralRegenerator().Regenerate(project, excavation); Near(10d, excavation.Quantities["ExcavationVolumeM3"]); Near(11.5d, excavation.Quantities["LooseExcavationVolumeM3"]);
        }

        private static void ColumnFootprintRegeneration()
        {
            var column = new ProjectElement("C1", ElementCategory.Column, "", "f", "z"); column.Properties["AreaM2"] = "0.09"; column.Properties["PerimeterM"] = "1.2"; column.Properties["HeightM"] = "3"; new StructuralRegenerator().Regenerate(NewProject(), column); Near(.27d, column.Quantities["NetConcreteM3"]); Near(3.6d, column.Quantities["FormworkM2"]);
        }

        private static void GenericQuantity()
        {
            var item = new ProjectElement("Q1", ElementCategory.CustomQuantity, "", "f", "z"); item.Properties["LengthM"] = "2.5"; item.Properties["AreaM2"] = "4.2"; new GenericQuantityRegenerator().Regenerate(NewProject(), item); Near(1d, item.Quantities["Count"]); Near(2.5d, item.Quantities["LengthM"]); Near(4.2d, item.Quantities["AreaM2"]);
        }

        private static void RebarSchedule()
        {
            var project = NewProject(); var bar = new ProjectElement("R1", ElementCategory.Rebar, "", "f", "z"); bar.Properties["Mark"] = "B1-TOP"; bar.Properties["DiameterMm"] = "20"; bar.Properties["Quantity"] = "4"; bar.Properties["CutLengthM"] = "5"; bar.Properties["HostElementId"] = "B1"; project.Elements.Add(bar); var rows = new RebarScheduleBuilder().Build(project); Equal(1, rows.Count); Equal(4, rows[0].Quantity); Near(20d, rows[0].TotalLengthM); Near(20d * (400d / 162d), rows[0].TotalWeightKg);
        }

        private static void RebarSpacingAndStirrup()
        {
            var spaced = new ProjectElement("R2", ElementCategory.Rebar, "", "f", "z"); spaced.Properties["Notation"] = "D8@150"; spaced.Properties["DistributionLengthM"] = "1"; spaced.Properties["CutLengthM"] = "1"; var row = new RebarScheduleBuilder().BuildElement(spaced); Equal(7, row.Quantity); Near(8d, row.DiameterMm);
            var stirrup = new ProjectElement("R3", ElementCategory.Rebar, "", "f", "z"); stirrup.Properties["DiameterMm"] = "8"; stirrup.Properties["Quantity"] = "10"; stirrup.Properties["Shape"] = "StirrupRect"; stirrup.Properties["WidthM"] = "0.3"; stirrup.Properties["HeightM"] = "0.5"; stirrup.Properties["CoverM"] = "0.025"; stirrup.Properties["HookLengthM"] = "0.1"; var stirrupRow = new RebarScheduleBuilder().BuildElement(stirrup); Near(1.6d, stirrupRow.CutLengthM); Near(16d, stirrupRow.TotalLengthM);
        }

        private static void RebarRegeneration()
        {
            var bar = new ProjectElement("R4", ElementCategory.Rebar, "", "f", "z"); bar.Properties["Notation"] = "4D20"; bar.Properties["CutLengthM"] = "5"; new RebarRegenerator().Regenerate(NewProject(), bar); Near(4d, bar.Quantities["Count"]); Near(20d, bar.Quantities["TotalLengthM"]); Near(20d * (400d / 162d), bar.Quantities["SteelWeightKg"]);
        }

        private static void RebarQuantityReport()
        {
            var project = NewProject(); var family = new ProjectFamily("rebar", "Cốt thép", ElementCategory.Rebar); project.Families.Add(family); var bar = new ProjectElement("R5", ElementCategory.Rebar, family.Id, "f", "z"); bar.Properties["Notation"] = "4D20"; bar.Properties["CutLengthM"] = "5"; new RebarRegenerator().Regenerate(project, bar); project.Elements.Add(bar); var rows = ProjectQuantityReportBuilder.Group(project); Equal(1, rows.Count); True(rows[0].SteelWeightKg > 49d); var totals = QuantityReportTotals.FromRows(rows); Near(rows[0].SteelWeightKg, totals.SteelWeightKg);
        }

        private static void RecognitionStrongAndAmbiguous()
        {
            var engine = new RecognitionEngine(); var beam = new EntitySnapshot("A1", "Line", "KC_DAM_B20"); beam.Metadata["NearbyText"] = "Dầm B20"; var strong = engine.Suggest(beam); Equal(ElementCategory.Beam, strong.TopCandidate!.Category); True(strong.Confidence > .99d); True(!strong.RequiresReview);
            var ambiguous = new EntitySnapshot("A2", "Line", "WALL_VACH"); var uncertain = engine.Suggest(ambiguous); True(uncertain.Candidates.Count >= 2); True(uncertain.RequiresReview); var batch = engine.SuggestBatch(new[] { beam, ambiguous }); Equal(1, batch.AutoAccepted.Count); Equal(1, batch.ReviewRequired.Count);
        }

        private static void RevisionQuantityDelta()
        {
            var project = NewProject(); var beam = new ProjectElement("B1", ElementCategory.Beam, "", "f", "z"); beam.SetQuantity("NetConcreteM3", .4d); beam.MarkClean(ElementDirtyFlags.All); project.Elements.Add(beam); var revisions = new RevisionService(); var before = revisions.Capture(project, "R1"); beam.SetQuantity("NetConcreteM3", .5d); var after = revisions.Capture(project, "R2"); var report = new QuantityRevisionReport(); var rows = report.Build(before, after); Equal(1, rows.Count); Equal("NetConcreteM3", rows[0].QuantityName); Near(.1d, rows[0].Delta); var summary = report.Summarize(rows); Equal(1, summary.Count); Near(.4d, summary[0].Before); Near(.5d, summary[0].After);
        }

        private static void RevisionStoreRoundtrip()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-rev-" + Guid.NewGuid().ToString("N") + ".qsrev"); try { var project = NewProject(); var element = new ProjectElement("B1", ElementCategory.Beam, "", "f", "z"); element.SetQuantity("NetConcreteM3", .4d); project.Elements.Add(element); var snapshot = new RevisionService().Capture(project, "BASE"); var store = new RevisionSnapshotStore(); store.Save(snapshot, path); var loaded = store.Load(path); Equal("BASE", loaded.Id); Equal(1, loaded.Elements.Count); Near(.4d, loaded.Elements[0].Quantities["NetConcreteM3"]); } finally { SafeDelete(path); SafeDelete(path + ".bak"); SafeDelete(path + ".tmp"); }
        }

        private static void RebarCsv()
        {
            var row = new RebarScheduleRow { Mark = "B1,TOP", FloorId = "T1", ZoneId = "Z1", Grade = "CB400-V", Shape = RebarShape.Straight, DiameterMm = 20, Quantity = 4, CutLengthM = 5, TotalLengthM = 20, UnitWeightKgPerM = 400d / 162d, TotalWeightKg = 20d * 400d / 162d }; var csv = RebarCsvExporter.ToCsv(new[] { row }); True(csv.Contains("\"B1,TOP\"")); True(csv.Contains("DiameterMm"));
        }

        private static ProjectState NewProject() { var project = new ProjectState(Guid.NewGuid().ToString("N"), "FullDomain"); project.Zones.Add(new ZoneDefinition("z", "Zone")); project.Floors.Add(new FloorDefinition("f", "Floor", 0d)); project.ActiveZoneId = "z"; project.ActiveFloorId = "f"; return project; }
        private static void SafeDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        private static void Near(double expected, double actual, double tolerance = 1e-9) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + "."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + "."); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    }
}
