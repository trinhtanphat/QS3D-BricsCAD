using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ReviewHardeningSmoke
    {
        public static void Run()
        {
            UnitConversions();
            RecognitionRules();
            ExcelHandleRoundTrip();
            RevisionRoundTrip();
            RevisionPersistenceHardening();
            ExportFailurePreservesDestination();
        }

        private static void UnitConversions()
        {
            Near(0.0254d, UnitScale.ToMeters(1d, DrawingUnit.Inch));
            Near(0.3048d, UnitScale.ToMeters(1d, DrawingUnit.Foot));
            Near(1000d, UnitScale.FromMeters(1d, DrawingUnit.Millimeter));
            Near(1609.344d, UnitScale.ToMeters(1d, DrawingUnit.Mile));
            Near(1000d, UnitScale.ToMeters(1d, DrawingUnit.Kilometer));
            Near(1e-6d, UnitScale.ToMeters(1d, DrawingUnit.Micrometer));
            Near(1200d / 3937d, UnitScale.ToMeters(1d, DrawingUnit.USSurveyFoot));
            var policy = new ProjectUnitPolicy(LengthUnit.Centimeter); Near(2.5d, policy.ToMeters(250d)); Near(250d, policy.FromMeters(2.5d));
            Equal(DrawingUnit.USSurveyMile, ProjectUnitPolicy.ToDrawingUnit(LengthUnit.USSurveyMile));
            Throws<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.NaN, DrawingUnit.Meter));
        }

        private static void RecognitionRules()
        {
            var snapshot = new EntitySnapshot("AB", "Line", "KC-DAM"); snapshot.Metadata["Text"] = "Dầm chính";
            var result = new RecognitionEngine().Suggest(snapshot);
            True(result.TopCandidate != null); Equal(ElementCategory.Beam, result.TopCandidate!.Category); True(result.Confidence >= .92d); True(!result.RequiresReview);

            var blt = new EntitySnapshot("30DC", "Solid3d", "blt_raft_foundation");
            var bltResult = new RecognitionEngine().Suggest(blt);
            True(bltResult.TopCandidate != null); Equal(ElementCategory.Foundation, bltResult.TopCandidate!.Category); True(bltResult.Confidence >= .92d); True(!bltResult.RequiresReview);
        }

        private static void ExcelHandleRoundTrip()
        {
            var directory = TempDirectory("excel-handle-roundtrip");
            var qs3dPath = Path.Combine(directory, "qs3d.xlsx");
            var bltPath = Path.Combine(directory, "blt.xlsx");
            try
            {
                var row = new QuantityReportRow { Floor = "F", Category = "WallFinish", FamilyName = "WF", Count = 1 };
                row.ElementIds.Add("WF-1"); row.SourceHandles.Add("AB12"); row.SourceHandles.Add("30DE");
                XlsxQuantityExporter.Export(qs3dPath, new[] { row });
                var exported = XlsxHandleReader.ReadHandles(qs3dPath, 2);
                Equal(2, exported.Count); Equal("AB12", exported[0]); Equal("30DE", exported[1]);

                using (var stream = new FileStream(bltPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                using (var writer = new StreamWriter(archive.CreateEntry("xl/worksheets/sheet1.xml").Open(), new UTF8Encoding(false)))
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"E1\" t=\"inlineStr\"><is><t>Handle</t></is></c></row><row r=\"5\"><c r=\"A5\" t=\"inlineStr\"><is><t>$12510$12512</t></is></c><c r=\"E5\" t=\"inlineStr\"><is><t>CF4</t></is></c></row></sheetData></worksheet>");
                var legacy = XlsxHandleReader.ReadHandles(bltPath, 5);
                Equal(2, legacy.Count); Equal("30DE", legacy[0]); Equal("30E0", legacy[1]);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RevisionRoundTrip()
        {
            var project = NewRevisionProject();
            var element = project.Elements.Single();
            var service = new RevisionService(); var before = service.Capture(project, "BASE");
            var directory = TempDirectory("revision-roundtrip"); var path = Path.Combine(directory, "review.qsrev");
            try
            {
                var store = new RevisionSnapshotStore(); store.Save(before, path); var loaded = store.Load(path); var item = loaded.Elements.Single();
                Equal("beam-family", item.FamilyId); Equal("f", item.FloorId); Equal("z", item.ZoneId); Equal("C30", item.Properties["Material"]); Equal("A1", item.SourceHandles.Single()); Near(1.25d, item.Quantities["NetVolumeM3"]);
                element.SetQuantity("NetVolumeM3", 1.5d); var after = service.Capture(project, "CURRENT"); var row = new QuantityRevisionReport().Build(loaded, after).Single(x => x.QuantityName == "NetVolumeM3"); Near(.25d, row.Delta);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RevisionPersistenceHardening()
        {
            var directory = TempDirectory("revision-hardening"); var path = Path.Combine(directory, "baseline.qsrev");
            try
            {
                var project = NewRevisionProject(); var service = new RevisionService(); var store = new RevisionSnapshotStore();
                store.Save(service.Capture(project, "BASE"), path);
                project.Elements.Single().SetQuantity("NetVolumeM3", 2d);
                store.Save(service.Capture(project, "SECOND"), path);
                True(File.Exists(path + ".bak"));

                File.WriteAllText(path, "<!DOCTYPE qs3dRevision [<!ENTITY payload 'unsafe'>]><qs3dRevision id=\"MALICIOUS\" createdUtc=\"2026-08-10T00:00:00Z\"><elements/></qs3dRevision>");
                var recovered = store.LoadWithBackupFallback(path);
                Equal("BASE", recovered.Id);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void ExportFailurePreservesDestination()
        {
            var directory = TempDirectory("export-atomic");
            var quantityPath = Path.Combine(directory, "quantity.xlsx");
            var rebarPath = Path.Combine(directory, "bbs.xlsx");
            try
            {
                File.WriteAllText(quantityPath, "quantity-sentinel");
                Throws<ArgumentOutOfRangeException>(() => XlsxQuantityExporter.Export(quantityPath, new[]
                {
                    new QuantityReportRow { Floor = "F", Category = "Beam", FamilyName = "B", Count = 1, GrossConcreteM3 = double.NaN }
                }));
                Equal("quantity-sentinel", File.ReadAllText(quantityPath));

                File.WriteAllText(rebarPath, "rebar-sentinel");
                Throws<ArgumentOutOfRangeException>(() => XlsxRebarScheduleExporter.Export(rebarPath, new[]
                {
                    new RebarScheduleRow { ElementId = "B1", BarMark = "M1", Notation = "4D20", DiameterMm = double.NaN, Quantity = 4, CuttingLengthM = 5d }
                }));
                Equal("rebar-sentinel", File.ReadAllText(rebarPath));
            }
            finally { DeleteDirectory(directory); }
        }

        private static ProjectState NewRevisionProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Revision");
            project.Zones.Add(new ZoneDefinition("z", "Vùng")); project.Floors.Add(new FloorDefinition("f", "Tầng", 0));
            var element = new ProjectElement("B1", ElementCategory.Beam, "beam-family", "f", "z"); element.Properties["Material"] = "C30"; element.SourceHandles.Add("A1"); element.SetQuantity("NetVolumeM3", 1.25d); project.Elements.Add(element);
            return project;
        }

        private static string TempDirectory(string name)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); return directory;
        }

        private static void DeleteDirectory(string directory) { try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { } }
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
