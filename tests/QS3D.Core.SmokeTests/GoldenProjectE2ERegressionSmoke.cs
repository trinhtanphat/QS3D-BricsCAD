using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class GoldenProjectE2ERegressionSmoke
    {
        internal static void Run()
        {
            ModelRegenerateExportPersistReopenRecalculate();
        }

        private static void ModelRegenerateExportPersistReopenRecalculate()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-golden-project-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var qsdbPath = Path.Combine(root, "golden.qsdb");
            var xlsxPath = Path.Combine(root, "golden.xlsx");
            var invalidPath = Path.Combine(root, "invalid.qsdb");

            try
            {
                var project = BuildProject();
                Recalculate(project);
                var before = ProjectQuantityReportBuilder.Detail(project);
                AssertGoldenProjection(project, before);

                var summary = ProjectQuantityReportBuilder.Group(project);
                QsCustomerWorkbookExporter.Export(xlsxPath, before, summary);
                True(File.Exists(xlsxPath) && new FileInfo(xlsxPath).Length > 0,
                    "Golden project did not produce a non-empty authoritative customer workbook.");

                var wallTrace = QsCustomerWorkbookTraceReader.Read(xlsxPath, QsCustomerWorkbookExporter.DetailSheet, 2);
                True(wallTrace.ElementIds.Count == 1 && wallTrace.ElementIds[0] == "W-GOLDEN",
                    "Golden workbook trace lost wall semantic identity.");
                True(wallTrace.Handles.Count == 1 && wallTrace.Handles[0] == "A100",
                    "Golden workbook trace lost wall source provenance.");
                Equal("DWG-GOLDEN-E2E", wallTrace.DrawingFingerprint,
                    "Golden workbook trace lost drawing fingerprint provenance.");

                var store = new QsdbProjectStore();
                store.Save(project, qsdbPath);
                var reopened = store.Load(qsdbPath);
                AssertRoundTripIdentity(reopened);

                Recalculate(reopened);
                var after = ProjectQuantityReportBuilder.Detail(reopened);
                AssertGoldenProjection(reopened, after);
                AssertEquivalent(before, after);

                reopened.Families.Single(x => x.Id == "F-WALL").Category = ElementCategory.Room;
                Throws<InvalidDataException>(() => store.Save(reopened, invalidPath));
                True(!File.Exists(invalidPath),
                    "Fail-closed Family/category validation published an invalid QSDB file.");

                Console.WriteLine("PASS synthetic golden-project E2E regression");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("golden-project-e2e", "Golden project E2E")
            {
                DrawingPath = "golden-project.dwg",
                DrawingFingerprint = "DWG-GOLDEN-E2E"
            };
            project.Floors.Add(new FloorDefinition("L1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));

            var wallFamily = new ProjectFamily("F-WALL", "Tường BT 200", ElementCategory.ArchitecturalWall);
            wallFamily.Properties["Material"] = "Concrete";
            project.Families.Add(wallFamily);
            var openingFamily = new ProjectFamily("F-OPENING", "Lỗ mở 900x2200", ElementCategory.WallOpening);
            openingFamily.Properties["Material"] = "Opening";
            project.Families.Add(openingFamily);

            var wall = new ProjectElement("W-GOLDEN", ElementCategory.ArchitecturalWall, wallFamily.Id, "L1", "Z1");
            wall.Properties["Name"] = "Tường trục A";
            wall.Properties["LengthM"] = "5";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";
            wall.SourceHandles.Add("A100");
            project.Elements.Add(wall);

            var opening = new ProjectElement("O-GOLDEN", ElementCategory.WallOpening, openingFamily.Id, "L1", "Z1");
            opening.Properties["Name"] = "Cửa đi D1";
            opening.Properties["WidthM"] = "0.9";
            opening.Properties["HeightM"] = "2.2";
            opening.SourceHandles.Add("A101");
            project.Elements.Add(opening);

            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            return project;
        }

        private static void Recalculate(ProjectState project)
        {
            var opening = project.Elements.Single(x => x.Id == "O-GOLDEN");
            var wall = project.Elements.Single(x => x.Id == "W-GOLDEN");
            new OpeningRegenerator().Regenerate(project, opening);
            opening.MarkClean(ElementDirtyFlags.All);
            new WallRegenerator().Regenerate(project, wall);
            wall.MarkClean(ElementDirtyFlags.All);
        }

        private static void AssertGoldenProjection(ProjectState project, System.Collections.Generic.IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(2, rows.Count, "Golden detail projection did not retain one row per semantic element.");
            var wall = rows.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == "W-GOLDEN");
            var opening = rows.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == "O-GOLDEN");

            Equal("Tầng 1", wall.Floor, "Golden wall lost Floor projection.");
            Equal("Zone 1", wall.Zone, "Golden wall lost Zone projection.");
            Equal("F-WALL", wall.FamilyId, "Golden wall lost Family identity.");
            Equal(ElementCategory.ArchitecturalWall.ToString(), wall.Category, "Golden wall lost category identity.");
            Equal("DWG-GOLDEN-E2E", wall.DrawingFingerprint, "Golden wall lost drawing provenance.");
            True(wall.SourceHandles.Count == 1 && wall.SourceHandles[0] == "A100", "Golden wall lost source Handle provenance.");
            Near(3d, wall.GrossConcreteM3, "Golden wall gross volume changed.");
            Near(2.604d, wall.NetConcreteM3, "Golden wall opening deduction changed.");

            Equal("F-OPENING", opening.FamilyId, "Golden opening lost Family identity.");
            Equal(ElementCategory.WallOpening.ToString(), opening.Category, "Golden opening lost category identity.");
            Near(1.98d, opening.DoorAreaM2, "Golden opening area changed.");

            var semanticOpening = project.Elements.Single(x => x.Id == "O-GOLDEN");
            True(semanticOpening.Properties.TryGetValue("HostWallId", out var host) && host == "W-GOLDEN",
                "Golden opening lost canonical host relation.");
            True(semanticOpening.DependsOn.Count == 1 && semanticOpening.DependsOn[0] == "W-GOLDEN",
                "Golden opening lost dependency provenance.");
        }

        private static void AssertRoundTripIdentity(ProjectState project)
        {
            Equal("golden-project-e2e", project.ProjectId, "Golden project identity changed after reopen.");
            Equal("DWG-GOLDEN-E2E", project.DrawingFingerprint, "Golden drawing fingerprint changed after reopen.");
            Equal(1, project.Floors.Count, "Golden Floor collection changed after reopen.");
            Equal(1, project.Zones.Count, "Golden Zone collection changed after reopen.");
            Equal(2, project.Families.Count, "Golden Family collection changed after reopen.");
            Equal(2, project.Elements.Count, "Golden element collection changed after reopen.");

            var wall = project.Elements.Single(x => x.Id == "W-GOLDEN");
            Equal("L1", wall.FloorId, "Golden wall Floor reference changed after reopen.");
            Equal("Z1", wall.ZoneId, "Golden wall Zone reference changed after reopen.");
            Equal("F-WALL", wall.FamilyId, "Golden wall Family reference changed after reopen.");
            Equal(ElementCategory.ArchitecturalWall, wall.Category, "Golden wall category changed after reopen.");
            True(wall.SourceHandles.Count == 1 && wall.SourceHandles[0] == "A100",
                "Golden wall source provenance changed after reopen.");
        }

        private static void AssertEquivalent(
            System.Collections.Generic.IReadOnlyList<QuantityReportRow> before,
            System.Collections.Generic.IReadOnlyList<QuantityReportRow> after)
        {
            Equal(before.Count, after.Count, "Golden detail row count changed across reopen/recalculate.");
            foreach (var expected in before)
            {
                var id = expected.ElementIds.Single();
                var actual = after.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == id);
                Equal(expected.Floor, actual.Floor, "Floor projection changed for " + id + ".");
                Equal(expected.Zone, actual.Zone, "Zone projection changed for " + id + ".");
                Equal(expected.FamilyId, actual.FamilyId, "Family projection changed for " + id + ".");
                Equal(expected.Category, actual.Category, "Category projection changed for " + id + ".");
                Equal(expected.DrawingFingerprint, actual.DrawingFingerprint, "Drawing provenance changed for " + id + ".");
                Near(expected.GrossConcreteM3, actual.GrossConcreteM3, "Gross concrete changed for " + id + ".");
                Near(expected.NetConcreteM3, actual.NetConcreteM3, "Net concrete changed for " + id + ".");
                Near(expected.DoorAreaM2, actual.DoorAreaM2, "Opening area changed for " + id + ".");
            }
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
