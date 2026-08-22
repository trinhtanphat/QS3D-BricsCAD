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
        private const int ExpectedCanonicalRegenerationWork = 10;

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
                var summary = ProjectQuantityReportBuilder.Group(project);
                AssertGoldenProjection(project, before);
                AssertGoldenSummary(summary);

                QsCustomerWorkbookExporter.Export(xlsxPath, before, summary);
                True(File.Exists(xlsxPath) && new FileInfo(xlsxPath).Length > 0,
                    "Golden project did not produce a non-empty customer workbook.");
                AssertWorkbookTrace(xlsxPath, before);

                var store = new QsdbProjectStore();
                store.Save(project, qsdbPath);
                var reopened = store.Load(qsdbPath);
                AssertRoundTripIdentity(reopened);

                foreach (var element in reopened.Elements)
                    element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                Recalculate(reopened);

                var after = ProjectQuantityReportBuilder.Detail(reopened);
                var afterSummary = ProjectQuantityReportBuilder.Group(reopened);
                AssertGoldenProjection(reopened, after);
                AssertGoldenSummary(afterSummary);
                AssertEquivalent(before, after);
                AssertEquivalent(summary, afterSummary);

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
            Set(aw, "LengthM", "5", "HeightM", "3", "ThicknessM", "0.2");

            var beam = AddElement(project, "BM-GOLDEN", "F-BM", ElementCategory.Beam, "Concrete", "A101");
            Set(beam, "LengthM", "4", "WidthM", "0.3", "HeightM", "0.5");

            var column = AddElement(project, "CL-GOLDEN", "F-CL", ElementCategory.Column, "Concrete", "A102");
            Set(column, "WidthM", "0.4", "DepthM", "0.4", "HeightM", "3");

            var slab = AddElement(project, "SL-GOLDEN", "F-SL", ElementCategory.Slab, "Concrete", "A103");
            Set(slab, "AreaM2", "20", "OpeningAreaM2", "2", "ThicknessM", "0.15", "PerimeterM", "18");

            var sw = AddElement(project, "SW-GOLDEN", "F-SW", ElementCategory.StructuralWall, "Concrete", "A104");
            Set(sw, "LengthM", "4", "HeightM", "3", "ThicknessM", "0.2");

            var foundation = AddElement(project, "FD-GOLDEN", "F-FD", ElementCategory.Foundation, "Concrete", "A105");
            Set(foundation, "BaseAreaM2", "4", "ThicknessM", "0.5", "PerimeterM", "8");

            var door = AddElement(project, "DR-GOLDEN", "F-DR", ElementCategory.Door, "Timber", "A106");
            Set(door, "WidthM", "0.9", "HeightM", "2.2");

            var opening = AddElement(project, "OP-GOLDEN", "F-OP", ElementCategory.WallOpening, "Opening", "A107");
            Set(opening, "WidthM", "1.2", "HeightM", "1");

            var hostLinks = new HostLinkService();
            hostLinks.LinkOpening(project, door.Id, aw.Id);
            hostLinks.LinkOpening(project, opening.Id, sw.Id);
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

        private static void Set(ProjectElement element, params string[] keyValues)
        {
            if (keyValues.Length % 2 != 0) throw new ArgumentException("Golden fixture properties require key/value pairs.");
            for (var index = 0; index < keyValues.Length; index += 2)
                element.Properties[keyValues[index]] = keyValues[index + 1];
        }

        private static void Recalculate(ProjectState project)
        {
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Equal(ExpectedCanonicalRegenerationWork, engine.RegenerateDirty(project),
                "Golden regeneration work changed: 8 P0 elements plus 2 host-wall refresh passes are expected.");
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
                var trace = QsCustomerWorkbookTraceReader.Read(xlsxPath, QsCustomerWorkbookExporter.DetailSheet, index + 2);
                True(trace.ElementIds.Count == 1 && trace.ElementIds[0] == expectedId,
                    "Workbook trace lost ElementId for " + expectedId + ".");
                True(trace.Handles.SequenceEqual(expected.SourceHandles, StringComparer.OrdinalIgnoreCase),
                    "Workbook trace did not preserve dependency-expanded Handle provenance for " + expectedId + ".");
                Equal(DrawingFingerprint, trace.DrawingFingerprint,
                    "Workbook trace lost DrawingFingerprint for " + expectedId + ".");
            }
        }

        private static void AssertGoldenProjection(ProjectState project, IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(8, rows.Count, "Golden detail projection must retain all P0 categories.");
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
            Near(4d, beam.LengthM, "Beam length changed.");
            True(beam.HasLengthMEvidence, "Beam lost length evidence.");

            AssertOpening(rows, "DR-GOLDEN", 1.98d, "Door");
            AssertOpening(rows, "OP-GOLDEN", 1.2d, "WallOpening");
            AssertHostRelation(project, "DR-GOLDEN", "AW-GOLDEN");
            AssertHostRelation(project, "OP-GOLDEN", "SW-GOLDEN");
        }

        private static void AssertCommon(
            IReadOnlyList<QuantityReportRow> rows,
            string id,
            string familyId,
            ElementCategory category,
            string material,
            string handle)
        {
            var row = Row(rows, id);
            Equal("Tầng 1", row.Floor, id + " lost Floor.");
            Equal("Zone 1", row.Zone, id + " lost Zone.");
            Equal(familyId, row.FamilyId, id + " lost FamilyId.");
            Equal(category.ToString(), row.Category, id + " lost category.");
            Equal(material, row.Material, id + " lost material.");
            Equal(DrawingFingerprint, row.DrawingFingerprint, id + " lost fingerprint.");
            Equal(1, row.Count, id + " count changed.");
            True(row.SourceHandles.Contains(handle, StringComparer.OrdinalIgnoreCase),
                id + " lost its direct source Handle from the locate provenance closure.");
        }

        private static void AssertVolume(IReadOnlyList<QuantityReportRow> rows, string id, double gross, double deduction, double net)
        {
            var row = Row(rows, id);
            Near(gross, row.GrossConcreteM3, id + " gross changed.");
            Near(deduction, row.DeductionM3, id + " deduction changed.");
            Near(net, row.NetConcreteM3, id + " net changed.");
            True(row.HasGrossConcreteM3Evidence && row.HasDeductionM3Evidence && row.HasNetConcreteM3Evidence,
                id + " lost volume evidence.");
        }

        private static void AssertOpening(IReadOnlyList<QuantityReportRow> rows, string id, double area, string label)
        {
            var row = Row(rows, id);
            Near(area, row.DoorAreaM2, label + " opening area changed.");
            True(row.HasDoorAreaM2Evidence, label + " lost opening-area evidence.");
            True(!row.HasGrossConcreteM3Evidence && !row.HasNetConcreteM3Evidence,
                label + " must not fabricate concrete volume evidence.");
        }

        private static void AssertHostRelation(ProjectState project, string openingId, string hostId)
        {
            var opening = project.Elements.Single(x => x.Id == openingId);
            True(opening.Properties.TryGetValue("HostWallId", out var host) && host == hostId,
                openingId + " lost HostWallId.");
            True(opening.DependsOn.Count == 1 && opening.DependsOn[0] == hostId,
                openingId + " lost dependency provenance.");
        }

        private static void AssertGoldenSummary(IReadOnlyList<QuantityReportRow> rows)
        {
            Equal(8, rows.Count, "Golden summary must retain eight P0 groups.");
            foreach (var row in rows) Equal(1, row.Count, "Summary group count changed for " + row.FamilyId + ".");
            AssertVolume(rows, "AW-GOLDEN", 3d, 0.396d, 2.604d);
            AssertVolume(rows, "BM-GOLDEN", 0.6d, 0d, 0.6d);
            AssertVolume(rows, "CL-GOLDEN", 0.48d, 0d, 0.48d);
            AssertVolume(rows, "SL-GOLDEN", 3d, 0.3d, 2.7d);
            AssertVolume(rows, "SW-GOLDEN", 2.4d, 0.24d, 2.16d);
            AssertVolume(rows, "FD-GOLDEN", 2d, 0d, 2d);
            Near(1.98d, Row(rows, "DR-GOLDEN").DoorAreaM2, "Door summary area changed.");
            Near(1.2d, Row(rows, "OP-GOLDEN").DoorAreaM2, "WallOpening summary area changed.");
        }

        private static void AssertRoundTripIdentity(ProjectState project)
        {
            Equal("golden-project-e2e", project.ProjectId, "ProjectId changed after reopen.");
            Equal(DrawingFingerprint, project.DrawingFingerprint, "Fingerprint changed after reopen.");
            Equal(1, project.Floors.Count, "Floor collection changed after reopen.");
            Equal(1, project.Zones.Count, "Zone collection changed after reopen.");
            Equal(8, project.Families.Count, "Family collection changed after reopen.");
            Equal(8, project.Elements.Count, "Element collection changed after reopen.");

            AssertIdentity(project, "AW-GOLDEN", "F-AW", ElementCategory.ArchitecturalWall, "A100");
            AssertIdentity(project, "BM-GOLDEN", "F-BM", ElementCategory.Beam, "A101");
            AssertIdentity(project, "CL-GOLDEN", "F-CL", ElementCategory.Column, "A102");
            AssertIdentity(project, "SL-GOLDEN", "F-SL", ElementCategory.Slab, "A103");
            AssertIdentity(project, "SW-GOLDEN", "F-SW", ElementCategory.StructuralWall, "A104");
            AssertIdentity(project, "FD-GOLDEN", "F-FD", ElementCategory.Foundation, "A105");
            AssertIdentity(project, "DR-GOLDEN", "F-DR", ElementCategory.Door, "A106");
            AssertIdentity(project, "OP-GOLDEN", "F-OP", ElementCategory.WallOpening, "A107");
            AssertHostRelation(project, "DR-GOLDEN", "AW-GOLDEN");
            AssertHostRelation(project, "OP-GOLDEN", "SW-GOLDEN");
        }

        private static void AssertIdentity(ProjectState project, string id, string family, ElementCategory category, string handle)
        {
            var element = project.Elements.Single(x => x.Id == id);
            Equal("L1", element.FloorId, id + " Floor reference changed after reopen.");
            Equal("Z1", element.ZoneId, id + " Zone reference changed after reopen.");
            Equal(family, element.FamilyId, id + " Family reference changed after reopen.");
            Equal(category, element.Category, id + " category changed after reopen.");
            True(element.SourceHandles.Count == 1 && element.SourceHandles[0] == handle,
                id + " Handle changed after reopen.");
        }

        private static void AssertEquivalent(IReadOnlyList<QuantityReportRow> before, IReadOnlyList<QuantityReportRow> after)
        {
            Equal(before.Count, after.Count, "Golden row count changed across reopen/recalculate.");
            foreach (var expected in before)
            {
                True(expected.ElementIds.Count > 0, "Golden comparison row lost semantic identity.");
                var actual = after.Single(x => x.ElementIds.SequenceEqual(expected.ElementIds, StringComparer.OrdinalIgnoreCase));
                var label = string.Join(",", expected.ElementIds);
                Equal(expected.Floor, actual.Floor, "Floor changed for " + label + ".");
                Equal(expected.Zone, actual.Zone, "Zone changed for " + label + ".");
                Equal(expected.FamilyId, actual.FamilyId, "Family changed for " + label + ".");
                Equal(expected.Category, actual.Category, "Category changed for " + label + ".");
                Equal(expected.Material, actual.Material, "Material changed for " + label + ".");
                Equal(expected.DrawingFingerprint, actual.DrawingFingerprint, "Fingerprint changed for " + label + ".");
                Equal(expected.Count, actual.Count, "Count changed for " + label + ".");
                True(expected.SourceHandles.SequenceEqual(actual.SourceHandles, StringComparer.OrdinalIgnoreCase),
                    "Handle provenance changed for " + label + ".");
                Near(expected.GrossConcreteM3, actual.GrossConcreteM3, "Gross changed for " + label + ".");
                Near(expected.DeductionM3, actual.DeductionM3, "Deduction changed for " + label + ".");
                Near(expected.NetConcreteM3, actual.NetConcreteM3, "Net changed for " + label + ".");
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
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
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
