using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;
using QS3D.Core.Services;
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
            SourceHandlesFollowDependencies();
            RevisionRoundTrip();
            RevisionPersistenceHardening();
            RevisionZeroQuantityChanges();
            QsdbRejectsUnsavableMutableState();
            RebarNotationRejectsEmptySegments();
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
            var qs3dBlankHandlePath = Path.Combine(directory, "qs3d-blank-handle.xlsx");
            var invalidHandlePath = Path.Combine(directory, "qs3d-invalid-handle.xlsx");
            var ed2Path = Path.Combine(directory, "ed2.xlsx");
            var reorderedEd2Path = Path.Combine(directory, "ed2-reordered.xlsx");
            var bltPath = Path.Combine(directory, "blt.xlsx");
            try
            {
                var row = new QuantityReportRow
                {
                    Floor = "F",
                    Zone = "Z",
                    Category = "WallFinish",
                    FamilyId = "finish-family",
                    FamilyName = "$12510 cost note",
                    ElementName = "Finish instance",
                    Material = "Concrete",
                    Note = "ED2 note",
                    DensityKgM3 = 2400d,
                    MassKg = 4500d,
                    GrossConcreteM3 = 1e-9d,
                    DrawingFingerprint = "DWG-FINGERPRINT-1",
                    Count = 1
                };
                row.ElementIds.Add("WF-1"); row.SourceHandles.Add("AB12"); row.SourceHandles.Add("30DE");
                XlsxQuantityExporter.Export(qs3dPath, new[] { row });
                var exported = XlsxHandleReader.ReadHandleLookup(qs3dPath, 2);
                Equal(2, exported.Handles.Count); Equal("AB12", exported.Handles[0]); Equal("30DE", exported.Handles[1]);
                Equal(1, exported.ElementIds.Count); Equal("WF-1", exported.ElementIds[0]);
                Equal("DWG-FINGERPRINT-1", exported.DrawingFingerprint); True(!exported.UsesLegacyDecimalHandles); True(exported.IsModernSchema);

                var blankHandleRow = new QuantityReportRow { Floor = "F", Category = "WallFinish", FamilyName = "$12510 cost note", DrawingFingerprint = "DWG-FINGERPRINT-1", Count = 1 };
                blankHandleRow.ElementIds.Add("WF-2");
                XlsxQuantityExporter.Export(qs3dBlankHandlePath, new[] { blankHandleRow });
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandleLookup(qs3dBlankHandlePath, 2));

                var invalidHandle = new QuantityReportRow { Floor = "F", Category = "WallFinish", FamilyName = "Finish", DrawingFingerprint = "DWG-FINGERPRINT-1", Count = 1 };
                invalidHandle.ElementIds.Add("WF-BAD"); invalidHandle.SourceHandles.Add("NOT-HEX");
                XlsxQuantityExporter.Export(invalidHandlePath, new[] { invalidHandle });
                Throws<InvalidDataException>(() => XlsxHandleReader.ReadHandleLookup(invalidHandlePath, 2));

                var secondDetail = new QuantityReportRow { Floor = "F", Category = "WallFinish", FamilyName = "Finish", DrawingFingerprint = "DWG-FINGERPRINT-1", Count = 1 };
                secondDetail.ElementIds.Add("WF-2"); secondDetail.SourceHandles.Add("40AA");
                var summary = new QuantityReportRow
                {
                    Floor = "F", Zone = "Z", Category = "WallFinish", FamilyId = "finish-family",
                    FamilyName = "$12510 cost note", Material = "Concrete", DensityKgM3 = 2400d,
                    MassKg = 4500d, DrawingFingerprint = "DWG-FINGERPRINT-1", Count = 1, GrossConcreteM3 = 1e-9d
                };
                summary.ElementIds.Add("WF-1"); summary.SourceHandles.Add("AB12"); summary.SourceHandles.Add("30DE");
                var secondSummary = new QuantityReportRow { Floor = "F", Category = "WallFinish", FamilyName = "Finish", DrawingFingerprint = "DWG-FINGERPRINT-1", Count = 1 };
                secondSummary.ElementIds.Add("WF-2"); secondSummary.SourceHandles.Add("40AA");
                XlsxQuantityExporter.ExportEd2(ed2Path, new[] { row, secondDetail }, new[] { summary, secondSummary });
                using (var archive = ZipFile.OpenRead(ed2Path))
                {
                    True(archive.GetEntry("xl/worksheets/sheet1.xml") != null);
                    True(archive.GetEntry("xl/worksheets/sheet2.xml") != null);
                    using (var reader = new StreamReader(archive.GetEntry("xl/workbook.xml")!.Open(), Encoding.UTF8))
                    {
                        var workbook = reader.ReadToEnd();
                        True(workbook.Contains("CHI_TIET")); True(workbook.Contains("TONG_HOP"));
                    }
                    using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open(), Encoding.UTF8))
                    {
                        var detailSheet = reader.ReadToEnd();
                        True(detailSheet.Contains("STT")); True(detailSheet.Contains("Tên cấu kiện"));
                        True(detailSheet.Contains("Vật liệu")); True(detailSheet.Contains("Family ID"));
                        True(detailSheet.Contains("Tầng/Zone")); True(detailSheet.Contains("Khối lượng riêng (kg/m³)"));
                        True(detailSheet.Contains("Khối lượng (kg)")); True(detailSheet.Contains("Ghi chú"));
                        True(detailSheet.Contains(">2400<")); True(detailSheet.Contains(">4500<"));
                        True(detailSheet.Contains("r=\"A2\" s=\"4\"><v>1</v>"));
                        True(detailSheet.Contains("r=\"G2\" s=\"4\"><v>1</v>"));
                        True(detailSheet.Contains("r=\"H2\" s=\"5\"><v>1E-09</v>"));
                        True(detailSheet.Contains("r=\"T2\" s=\"2\"><v>2400</v>"));
                        True(detailSheet.Contains("r=\"U2\" s=\"2\"><v>4500</v>"));
                        True(!detailSheet.Contains("r=\"T3\"")); True(!detailSheet.Contains("r=\"U3\""));
                        True(detailSheet.Contains("QS3D Element ID")); True(detailSheet.Contains("CAD Handle (hex)"));
                        True(detailSheet.Contains("QS3D Drawing Fingerprint"));
                    }
                    using (var reader = new StreamReader(archive.GetEntry("xl/styles.xml")!.Open(), Encoding.UTF8))
                    {
                        var styles = reader.ReadToEnd();
                        True(styles.Contains("numFmtId=\"164\" formatCode=\"#,##0.000\""));
                        True(styles.Contains("cellXfs count=\"6\""));
                    }
                }
                var ed2Detail = XlsxHandleReader.ReadHandleLookup(ed2Path, 3);
                Equal(1, ed2Detail.Handles.Count); Equal("40AA", ed2Detail.Handles[0]); Equal(1, ed2Detail.ElementIds.Count); Equal("WF-2", ed2Detail.ElementIds[0]); Equal("DWG-FINGERPRINT-1", ed2Detail.DrawingFingerprint);
                Equal("CHI_TIET", ed2Detail.WorksheetName); True(ed2Detail.IsModernSchema); True(ed2Detail.IsEd2Detail);
                summary.Count = 2;
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(ed2Path, new[] { summary }, new[] { summary }));

                CreateReorderedEd2Workbook(reorderedEd2Path);
                var reordered = XlsxHandleReader.ReadHandleLookup(reorderedEd2Path, 2);
                Equal("CHI_TIET", reordered.WorksheetName); True(reordered.IsEd2Detail);
                Equal("ED2-2", reordered.ElementIds.Single()); Equal("BEEF", reordered.Handles.Single());

                using (var stream = new FileStream(bltPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                using (var writer = new StreamWriter(archive.CreateEntry("xl/worksheets/sheet1.xml").Open(), new UTF8Encoding(false)))
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"E1\" t=\"inlineStr\"><is><t>Handle</t></is></c></row><row r=\"5\"><c r=\"A5\" t=\"inlineStr\"><is><t>$12510$12512</t></is></c><c r=\"E5\" t=\"inlineStr\"><is><t>CF4</t></is></c></row></sheetData></worksheet>");
                var legacy = XlsxHandleReader.ReadHandleLookup(bltPath, 5);
                Equal(2, legacy.Handles.Count); Equal("30DE", legacy.Handles[0]); Equal("30E0", legacy.Handles[1]);
                Equal(string.Empty, legacy.DrawingFingerprint); True(legacy.UsesLegacyDecimalHandles);
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

        private static void SourceHandlesFollowDependencies()
        {
            var project = NewRevisionProject();
            var room = project.Elements.Single();
            room.SourceHandles.Clear();
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "A1;B2";
            var finish = new ProjectElement("FINISH", ElementCategory.WallFinish, string.Empty, "f", "z");
            finish.DependsOn.Add(room.Id); room.DependsOn.Add(finish.Id); project.Elements.Add(finish);
            var handles = SourceHandleResolver.Resolve(project, new[] { finish.Id });
            Equal(2, handles.Count); Equal("A1", handles[0]); Equal("B2", handles[1]);
            var report = ProjectQuantityReportBuilder.Group(project).Single(x => x.Category == ElementCategory.WallFinish.ToString());
            Equal(2, report.SourceHandles.Count); Equal("A1", report.SourceHandles[0]); Equal("B2", report.SourceHandles[1]);
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

        private static void RevisionZeroQuantityChanges()
        {
            var before = new RevisionSnapshot { Id = "before", CreatedUtc = DateTime.UtcNow };
            var beforeElement = new RevisionElementSnapshot { ElementId = "E1", Category = "Beam" };
            before.Elements.Add(beforeElement);
            var after = new RevisionSnapshot { Id = "after", CreatedUtc = DateTime.UtcNow };
            var afterElement = new RevisionElementSnapshot { ElementId = "E1", Category = "Beam" };
            afterElement.Quantities["Zero"] = 0d; after.Elements.Add(afterElement);
            var added = new QuantityRevisionReport().Build(before, after).Single();
            Equal("Added", added.Change); Equal("Zero", added.QuantityName);
            var removed = new QuantityRevisionReport().Build(after, before).Single();
            Equal("Removed", removed.Change); Equal("Zero", removed.QuantityName);
        }

        private static void QsdbRejectsUnsavableMutableState()
        {
            var directory = TempDirectory("qsdb-mutable-validation"); var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var project = NewRevisionProject(); var store = new QsdbProjectStore(); store.Save(project, path);
                var original = File.ReadAllText(path);
                project.Metadata[string.Empty] = "invalid";
                Throws<InvalidDataException>(() => store.Save(project, path));
                Equal(original, File.ReadAllText(path));
                project.Metadata.Remove(string.Empty);
                var zone = project.Zones.Single(); var originalZoneName = zone.Name;
                Throws<ArgumentException>(() => zone.Name = string.Empty);
                Equal(originalZoneName, zone.Name);
                Equal(original, File.ReadAllText(path));

                var revisionPath = Path.Combine(directory, "baseline.qsrev");
                var snapshot = new RevisionService().Capture(NewRevisionProject(), "valid");
                var revisionStore = new RevisionSnapshotStore(); revisionStore.Save(snapshot, revisionPath);
                var revisionOriginal = File.ReadAllText(revisionPath);
                snapshot.Elements.Add(new RevisionElementSnapshot { ElementId = snapshot.Elements[0].ElementId, Category = "Beam" });
                Throws<InvalidDataException>(() => revisionStore.Save(snapshot, revisionPath));
                Equal(revisionOriginal, File.ReadAllText(revisionPath));
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RebarNotationRejectsEmptySegments()
        {
            Throws<FormatException>(() => RebarNotationParser.Parse("4D20++2D16"));
            Throws<FormatException>(() => RebarNotationParser.Parse("+4D20"));
            Throws<FormatException>(() => RebarNotationParser.Parse("4D20+"));
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

                Throws<System.Xml.XmlException>(() => XlsxQuantityExporter.Export(quantityPath, new[]
                {
                    new QuantityReportRow { Floor = "F", Category = "Beam", FamilyName = "Bad\u0001Name", Count = 1 }
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

        private static void CreateReorderedEd2Workbook(string path)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                WriteEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"TONG_HOP\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"CHI_TIET\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>");
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/></Relationships>");
                WriteEntry(archive, "xl/worksheets/sheet1.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData/></worksheet>");
                WriteEntry(archive, "xl/worksheets/sheet2.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>QS3D Element ID</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>CAD Handle (hex)</t></is></c><c r=\"C1\" t=\"inlineStr\"><is><t>QS3D Drawing Fingerprint</t></is></c></row><row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>ED2-2</t></is></c><c r=\"B2\" t=\"inlineStr\"><is><t>BEEF</t></is></c><c r=\"C2\" t=\"inlineStr\"><is><t>FP-2</t></is></c></row></sheetData></worksheet>");
            }
        }

        private static void WriteEntry(ZipArchive archive, string path, string contents)
        {
            using (var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false))) writer.Write(contents);
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
