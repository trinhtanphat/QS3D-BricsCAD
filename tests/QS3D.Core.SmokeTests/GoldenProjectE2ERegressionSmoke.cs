using System;
using System.Collections.Generic;
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
        private const string DrawingFingerprint = "DWG-GOLDEN-E2E";

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
                Recalculate(project, expectedRegenerated: 8);
                var before = ProjectQuantityReportBuilder.Detail(project);
                AssertGoldenProjection(project, before);

                var summary = ProjectQuantityReportBuilder.Group(project);
                AssertGoldenSummary(summary);
                QsCustomerWorkbookExporter.Export(xlsxPath, before, summary);
                True(File.Exists(xlsxPath) && new FileInfo(xlsxPath).Length > 0,
                    "Golden project did not produce a non-empty authoritative customer workbook.");
                AssertWorkbookTrace(xlsxPath, before);

                var store = new QsdbProjectStore();
                store.Save(project, qsdbPath);
                var reopened = store.Load(qsdbPath);
                AssertRoundTripIdentity(reopened);

                MarkAllForRecalculation(reopened);
                Recalculate(reopened, expectedRegenerated: 8);
                var after = ProjectQuantityReportBuilder.Detail(reopened);
                AssertGoldenProjection(reopened, after);
                AssertEquivalent(before, after);
                AssertEquivalent(summary, ProjectQuantityReportBuilder.Group(reopened));
                AssertGoldenSummary(ProjectQuantityReportBuilder.Group(reopened));

                reopened.Families.Single(x => x.Id == "F-AW").Category = ElementCategory.Room;
                Throws<InvalidDataException>(() => store.Save(reopened, invalidPath));
                True(!File.Exists(invalidPath),
                    "Fail-closed Family/category validation published an invalid QSDB file.");

                Console.WriteLine("PASS synthetic golden-project full-P0 E2E regression");
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
                DrawingFingerprint = DrawingFingerprint
            };
            project.Floors.Add(new FloorDefinition("L1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));

            var aw = AddElement(project, "AW-GOLDEN", "F-AW", ElementCategory.ArchitecturalWall, "Concrete", "A100");
            aw.Properties["Name"] = "Tường kiến trúc trục A";
            aw.Properties["LengthM"] = "5";
            aw.Properties["HeightM"] = "3";
            aw.Properties["ThicknessM"] = "0.2";

            var beam = AddElement(project, "BM-GOLDEN", "F-BM", ElementCategory.Beam, "Concrete", "A101");
            beam.Properties["Name"] = "Dầm B1";
            beam.Properties["LengthM"] = "4";
            beam.Properties["WidthM"] = "0.3";
            beam.Properties["HeightM"] = "0.5";

            var column = AddElement(project, "CL-GOLDEN", "F-CL", ElementCategory.Column, "Concrete", "A102");
            column.Properties["Name"] = "Cột C1";
            column.Properties["WidthM"] = "0.4";
            column.Properties["DepthM"] = "0.4";
            column.Properties["HeightM"] = "3";

            var slab = AddElement(project, "SL-GOLDEN", "F-SL", ElementCategory.Slab, "Concrete", "A103");
            slab.Properties["Name"] = "Sàn S1";
            slab.Properties["AreaM2"] = "20";
            slab.Properties["OpeningAreaM2"] = "2";
            slab.Properties["ThicknessM"] = "0.15";
            slab.Properties["PerimeterM"] = "18";

            var structuralWall = AddElement(project, "SW-GOLDEN", "F-SW", ElementCategory.StructuralWall, "Concrete", "A104");
            structuralWall.Properties["Name"] = "Vách SW1";
            structuralWall.Properties["LengthM"] = "4";
            structuralWall.Properties["HeightM"] = "3";
            structuralWall.Properties["ThicknessM"] = "0.2";

            var foundation = AddElement(project, "FD-GOLDEN", "F-FD", ElementCategory.Foundation, "Concrete", "A105");
            foundation.Properties["Name"] = "Móng F1";
            foundation.Properties["BaseAreaM2"] = "4";
            foundation.Properties["ThicknessM"] = "0.5";
            foundation.Properties["PerimeterM"] = "8";

            var door = AddElement(project, "DR-GOLDEN", "F-DR", ElementCategory.Door, "Timber", "A106");
            door.Properties["Name"] = "Cửa D1";
            door.Properties["WidthM"] = "0.9";
            door.Properties["HeightM"] = "2.2";

            var opening = AddElement(project, "OP-GOLDEN", "F-OP", ElementCategory.WallOpening, "Opening", "A107");
            opening.Properties["Name"] = "Lỗ mở O1";
            opening.Properties["WidthM"] = "1.2";
            opening.Properties["HeightM"] = "1";

            var hostLinks = new HostLinkService();
            hostLinks.LinkOpening(project, door.Id, aw.Id);
            hostLinks.LinkOpening(project, opening.Id, structuralWall.Id);
            return project;
        }

        private static ProjectElement AddElement(
            ProjectState project,
            string elementId,
            string familyId,
            ElementCategory category,
            string material,
            string handle)
        {
            var family = new ProjectFamily(familyId, "Golden " + category, category);
            family.Properties["Material"] = material;
            project.Families.Add(family);

            var element = new ProjectElement(elementId, category, family.Id, "L1", "Z1");
            element.SourceHandles.Add(handle);
            project.Elements.Add(element);
            return element;
        }

        private static void MarkAllForRecalculation(ProjectState project)
        {
            foreach (var element in project.Elements)
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
        }

        private static void Recalculate(ProjectState project, int expectedRegenerated)
        {
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Equal(expectedRegenerated, engine.RegenerateDirty(project),
                "Golden project canonical regeneration count changed.");
            Equal(0, engine.RegenerateDirty(project),
                "Golden project repeated no-op regeneration must remain idempotent.");
        }

        private static void AssertWorkbookTrace(string xlsxPath, IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(8, rows.Count, "Golden workbook trace requires all P0 detail rows.");
            for (var index = 0; index < rows.Count; index++)
            {
                var expected = rows[index];
                var expectedId = expected.ElementIds.Single();
                var trace = QsCustomerWorkbookTraceReader.Read(
                    xlsxPath,
                    QsCustomerWorkbookExporter.DetailSheet,
                    index + 2);
                True(trace.ElementIds.Count == 1 && trace.ElementIds[0] == expectedId,
                    "Golden workbook trace lost semantic identity for " + expectedId + ".");
                True(trace.Handles.Count == 1 && trace.Handles[0] == expected.SourceHandles.Single(),
                    "Golden workbook trace lost source Handle provenance for " + expectedId + ".");
                Equal(DrawingFingerprint, trace.DrawingFingerprint,
                    "Golden workbook trace lost drawing fingerprint provenance for " + expectedId + ".");
            }
        }

        private static void AssertGoldenProjection(ProjectState project, IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(8, rows.Count, "Golden detail projection did not retain the full P0 category envelope.");
            AssertCommon(rows, "AW-GOLDEN", "F-AW", ElementCategory.ArchitecturalWall, "Concrete", "A100");
            AssertCommon(rows, "BM-GOLDEN", "F-BM", ElementCategory.Beam, "Concrete", "A101");
            AssertCommon(rows, "CL-GOLDEN", "F-CL", ElementCategory.Column, "Concrete", "A102");
            AssertCommon(rows, "SL-GOLDEN", "F-SL", ElementCategory.Slab, "Concrete", "A103");
            AssertCommon(rows, "SW-GOLDEN", "F-SW", ElementCategory.StructuralWall, "Concrete", "A104");
            AssertCommon(rows, "FD-GOLDEN", "F-FD", ElementCategory.Foundation, "Concrete", "A105");
            AssertCommon(rows, "DR-GOLDEN", "F-DR", ElementCategory.Door, "Timber", "A106");
            AssertCommon(rows, "OP-GOLDEN", "F-OP", ElementCategory.WallOpening, "Opening", "A107");

            AssertVolume(rows, "AW-GOLDEN", 3d, 0.396d, 2.604d);
            AssertVolume(rows, "BM-GOLDEN", 0.6d, 0d, 0.6d);
            AssertVolume(rows, "CL-GOLDEN", 0.48d, 0d, 0.48d);
            AssertVolume(rows, "SL-GOLDEN", 3d, 0.3d, 2.7d);
            AssertVolume(rows, "SW-GOLDEN", 2.4d, 0.24d, 2.16d);
            AssertVolume(rows, "FD-GOLDEN", 2d, 0d, 2d);

            var beam = Row(rows, "BM-GOLDEN");
            Near(4d, beam.LengthM, "Golden Beam length changed.");
            True(beam.HasLengthMEvidence, "Golden Beam lost length evidence.");

            var door = Row(rows, "DR-GOLDEN");
            Near(1.98d, door.DoorAreaM2, "Golden Door opening area changed.");
            True(door.HasDoorAreaM2Evidence, "Golden Door lost opening-area evidence.");
            True(!door.HasGrossConcreteM3Evidence && !door.HasNetConcreteM3Evidence,
                "Golden Door must not fabricate concrete volume evidence.");

            var opening = Row(rows, "OP-GOLDEN");
            Near(1.2d, opening.DoorAreaM2, "Golden WallOpening area changed.");
            True(opening.HasDoorAreaM2Evidence, "Golden WallOpening lost opening-area evidence.");
            True(!opening.HasGrossConcreteM3Evidence && !opening.HasNetConcreteM3Evidence,
                "Golden WallOpening must not fabricate concrete volume evidence.");

            AssertHostRelation(project, "DR-GOLDEN", "AW-GOLDEN");
            AssertHostRelation(project, "OP-GOLDEN", "SW-GOLDEN");
        }

        private static void AssertCommon(
            IReadOnlyList<QuantityReportRow> rows,
            string elementId,
            string familyId,
            ElementCategory category,
            string material,
            string handle)
        {
            var row = Row(rows, elementId);
            Equal("Tầng 1", row.Floor, elementId + " lost Floor projection.");
            Equal("Zone 1", row.Zone, elementId + " lost Zone projection.");
            Equal(familyId, row.FamilyId, elementId + " lost Family identity.");
            Equal(category.ToString(), row.Category, elementId + " lost category identity.");
            Equal(material, row.Material, elementId + " lost material identity.");
            Equal(DrawingFingerprint, row.DrawingFingerprint, elementId + " lost drawing provenance.");
            Equal(1, row.Count, elementId + " detail count changed.");
            True(row.SourceHandles.Count == 1 && row.SourceHandles[0] == handle,
                elementId + " lost source Handle provenance.");
        }

        private static void AssertVolume(
            IReadOnlyList<QuantityReportRow> rows,
            string elementId,
            double gross,
            double deduction,
            double net)
        {
            var row = Row(rows, elementId);
            Near(gross, row.GrossConcreteM3, elementId + " gross volume changed.");
            Near(deduction, row.DeductionM3, elementId + " deduction changed.");
            Near(net, row.NetConcreteM3, elementId + " net volume changed.");
            True(row.HasGrossConcreteM3Evidence && row.HasDeductionM3Evidence && row.HasNetConcreteM3Evidence,
                elementId + " lost deterministic volume evidence.");
        }

        private static void AssertHostRelation(ProjectState project, string openingId, string hostId)
        {
            var opening = project.Elements.Single(x => x.Id == openingId);
            True(opening.Properties.TryGetValue("HostWallId", out var host) && host == hostId,
                openingId + " lost canonical HostWallId relation.");
            True(opening.DependsOn.Count == 1 && opening.DependsOn[0] == hostId,
                openingId + " lost dependency provenance.");
        }

        private static void AssertGoldenSummary(IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(8, rows.Count, "Golden summary must preserve one group for every P0 Family/category.");
            foreach (var row in rows)
                Equal(1, row.Count, "Golden summary group count changed for " + row.FamilyId + ".");

            AssertVolume(rows, "AW-GOLDEN", 3d, 0.396d, 2.604d);
            AssertVolume(rows, "BM-GOLDEN", 0.6d, 0d, 0.6d);
            AssertVolume(rows, "CL-GOLDEN", 0.48d, 0d, 0.48d);
            AssertVolume(rows, "SL-GOLDEN", 3d, 0.3d, 2.7d);
            AssertVolume(rows, "SW-GOLDEN", 2.4d, 0.24d, 2.16d);
            AssertVolume(rows, "FD-GOLDEN", 2d, 0d, 2d);
            Near(1.98d, Row(rows, "DR-GOLDEN").DoorAreaM2, "Golden Door summary area changed.");
            Near(1.2d, Row(rows, "OP-GOLDEN").DoorAreaM2, "Golden WallOpening summary area changed.");
        }

        private static void AssertRoundTripIdentity(ProjectState project)
        {
            Equal("golden-project-e2e", project.ProjectId, "Golden project identity changed after reopen.");
            Equal(DrawingFingerprint, project.DrawingFingerprint, "Golden drawing fingerprint changed after reopen.");
            Equal(1, project.Floors.Count, "Golden Floor collection changed after reopen.");
            Equal(1, project.Zones.Count, "Golden Zone collection changed after reopen.");
            Equal(8, project.Families.Count, "Golden Family collection changed after reopen.");
            Equal(8, project.Elements.Count, "Golden P0 element collection changed after reopen.");

            var expected = new[]
            {
                new { Id = "AW-GOLDEN", Family = "F-AW", Category = ElementCategory.ArchitecturalWall, Handle = "A100" },
                new { Id = "BM-GOLDEN", Family = "F-BM", Category = ElementCategory.Beam, Handle = "A101" },
                new { Id = "CL-GOLDEN", Family = "F-CL", Category = ElementCategory.Column, Handle = "A102" },
                new { Id = "SL-GOLDEN", Family = "F-SL", Category = ElementCategory.Slab, Handle = "A103" },
                new { Id = "SW-GOLDEN", Family = "F-SW", Category = ElementCategory.StructuralWall, Handle = "A104" },
                new { Id = "FD-GOLDEN", Family = "F-FD", Category = ElementCategory.Foundation, Handle = "A105" },
                new { Id = "DR-GOLDEN", Family = "F-DR", Category = ElementCategory.Door, Handle = "A106" },
                new { Id = "OP-GOLDEN", Family = "F-OP", Category = ElementCategory.WallOpening, Handle = "A107" }
            };

            foreach (var item in expected)
            {
                var element = project.Elements.Single(x => x.Id == item.Id);
                Equal("L1", element.FloorId, item.Id + " Floor reference changed after reopen.");
                Equal("Z1", element.ZoneId, item.Id + " Zone reference changed after reopen.");
                Equal(item.Family, element.FamilyId, item.Id + " Family reference changed after reopen.");
                Equal(item.Category, element.Category, item.Id + " category changed after reopen.");
                True(element.SourceHandles.Count == 1 && element.SourceHandles[0] == item.Handle,
                    item.Id + " source provenance changed after reopen.");
            }

            AssertHostRelation(project, "DR-GOLDEN", "AW-GOLDEN");
            AssertHostRelation(project, "OP-GOLDEN", "SW-GOLDEN");
        }

        private static void AssertEquivalent(
            IReadOnlyList<QuantityReportRow> before,
            IReadOnlyList<QuantityReportRow> after)
        {
            Equal(before.Count, after.Count, "Golden row count changed across reopen/recalculate.");
            foreach (var expected in before)
            {
                True(expected.ElementIds.Count > 0, "Golden comparison row lost semantic identity.");
                var actual = after.Single(x => x.ElementIds.SequenceEqual(expected.ElementIds, StringComparer.OrdinalIgnoreCase));
                var label = string.Join(",", expected.ElementIds);
                Equal(expected.Floor, actual.Floor, "Floor projection changed for " + label + ".");
                Equal(expected.Zone, actual.Zone, "Zone projection changed for " + label + ".");
                Equal(expected.FamilyId, actual.FamilyId, "Family projection changed for " + label + ".");
                Equal(expected.Category, actual.Category, "Category projection changed for " + label + ".");
                Equal(expected.Material, actual.Material, "Material projection changed for " + label + ".");
                Equal(expected.DrawingFingerprint, actual.DrawingFingerprint, "Drawing provenance changed for " + label + ".");
                Equal(expected.Count, actual.Count, "Count changed for " + label + ".");
                True(expected.SourceHandles.SequenceEqual(actual.SourceHandles, StringComparer.OrdinalIgnoreCase),
                    "Source Handle provenance changed for " + label + ".");
                Near(expected.GrossConcreteM3, actual.GrossConcreteM3, "Gross concrete changed for " + label + ".");
                Near(expected.DeductionM3, actual.DeductionM3, "Deduction changed for " + label + ".");
                Near(expected.NetConcreteM3, actual.NetConcreteM3, "Net concrete changed for " + label + ".");
                Near(expected.FormworkM2, actual.FormworkM2, "Formwork changed for " + label + ".");
                Near(expected.LengthM, actual.LengthM, "Length changed for " + label + ".");
                Near(expected.DoorAreaM2, actual.DoorAreaM2, "Opening area changed for " + label + ".");
                Equal(expected.HasGrossConcreteM3Evidence, actual.HasGrossConcreteM3Evidence, "Gross evidence changed for " + label + ".");
                Equal(expected.HasDeductionM3Evidence, actual.HasDeductionM3Evidence, "Deduction evidence changed for " + label + ".");
                Equal(expected.HasNetConcreteM3Evidence, actual.HasNetConcreteM3Evidence, "Net evidence changed for " + label + ".");
                Equal(expected.HasFormworkM2Evidence, actual.HasFormworkM2Evidence, "Formwork evidence changed for " + label + ".");
                Equal(expected.HasLengthMEvidence, actual.HasLengthMEvidence, "Length evidence changed for " + label + ".");
                Equal(expected.HasDoorAreaM2Evidence, actual.HasDoorAreaM2Evidence, "Opening evidence changed for " + label + ".");
            }
        }

        private static QuantityReportRow Row(IReadOnlyList<QuantityReportRow> rows, string elementId)
        {
            return rows.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == elementId);
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
