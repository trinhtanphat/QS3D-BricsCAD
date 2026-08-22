using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Bim3dGoldenProjectE2ESmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bim3d-golden-" + Guid.NewGuid().ToString("N"));
            var qsdbPath = root + ".qsdb";
            var xlsxPath = root + ".xlsx";

            try
            {
                var project = CreateSyntheticProject();
                var beforeDetail = ProjectQuantityReportBuilder.Detail(project);
                var beforeSummary = ProjectQuantityReportBuilder.Group(project);
                AssertGoldenReport(beforeDetail, beforeSummary);

                new QsdbProjectStore().SaveNew(project, qsdbPath);
                var reopened = new QsdbProjectStore().Load(qsdbPath);

                Equal(project.ProjectId, reopened.ProjectId, "Project identity changed across QSDB round-trip.");
                Equal(project.DrawingFingerprint, reopened.DrawingFingerprint, "Drawing provenance changed across QSDB round-trip.");
                Equal(project.ActiveFloorId, reopened.ActiveFloorId, "Active Floor changed across QSDB round-trip.");
                Equal(project.ActiveZoneId, reopened.ActiveZoneId, "Active Zone changed across QSDB round-trip.");

                var afterDetail = ProjectQuantityReportBuilder.Detail(reopened);
                var afterSummary = ProjectQuantityReportBuilder.Group(reopened);
                AssertGoldenReport(afterDetail, afterSummary);
                AssertReportParity(beforeDetail, afterDetail);
                AssertReportParity(beforeSummary, afterSummary);

                QsCustomerWorkbookExporter.Export(xlsxPath, afterDetail, afterSummary);
                True(File.Exists(xlsxPath), "Golden-project customer workbook was not created.");
                using (var stream = File.OpenRead(xlsxPath))
                {
                    True(stream.Length > 4, "Golden-project customer workbook is unexpectedly empty.");
                    Equal((byte)'P', (byte)stream.ReadByte(), "Golden-project workbook is not an XLSX ZIP package.");
                    Equal((byte)'K', (byte)stream.ReadByte(), "Golden-project workbook is not an XLSX ZIP package.");
                }
            }
            finally
            {
                TryDelete(qsdbPath);
                TryDelete(qsdbPath + ".bak");
                TryDelete(xlsxPath);
            }
        }

        private static ProjectState CreateSyntheticProject()
        {
            var project = new ProjectState("BIM3D-GOLDEN-P1", "BIM3D golden project")
            {
                DrawingFingerprint = "BIM3D-GOLDEN-DWG-001",
                ActiveZoneId = "Z-01",
                ActiveFloorId = "F-01"
            };

            project.Zones.Add(new ZoneDefinition("Z-01", "Zone A"));
            project.Floors.Add(new FloorDefinition("F-01", "Level 1", 0d));

            var wallFamily = new ProjectFamily("FAM-WALL-200", "Wall 200", ElementCategory.ArchitecturalWall);
            wallFamily.Properties["Material"] = "Concrete";
            project.Families.Add(wallFamily);

            var beamFamily = new ProjectFamily("FAM-BEAM-300X500", "Beam 300x500", ElementCategory.Beam);
            beamFamily.Properties["Material"] = "Concrete";
            project.Families.Add(beamFamily);

            var wall = new ProjectElement("W-001", ElementCategory.ArchitecturalWall, wallFamily.Id, "F-01", "Z-01")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            wall.SourceHandles.Add("AB12");
            wall.Properties["Name"] = "Wall W-001";
            wall.SetQuantity("LengthM", 5d);
            wall.SetQuantity("GrossConcreteM3", 3d);
            wall.SetQuantity("DeductionM3", 0.6d);
            wall.SetQuantity("NetConcreteM3", 2.4d);
            wall.SetQuantity("FormworkM2", 24d);
            project.Elements.Add(wall);

            var beam = new ProjectElement("B-001", ElementCategory.Beam, beamFamily.Id, "F-01", "Z-01")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            beam.SourceHandles.Add("CD34");
            beam.Properties["Name"] = "Beam B-001";
            beam.SetQuantity("LengthM", 6d);
            beam.SetQuantity("GrossConcreteM3", 0.9d);
            beam.SetQuantity("NetConcreteM3", 0.9d);
            beam.SetQuantity("FormworkM2", 9.6d);
            project.Elements.Add(beam);

            return project;
        }

        private static void AssertGoldenReport(System.Collections.Generic.IReadOnlyList<QuantityReportRow> detail, System.Collections.Generic.IReadOnlyList<QuantityReportRow> summary)
        {
            Equal(2, detail.Count, "Golden project must produce one detail row per semantic element.");
            Equal(2, summary.Count, "Golden project must preserve the two category/family groups.");

            var wall = detail.Single(row => row.ElementIds.Count == 1 && row.ElementIds[0] == "W-001");
            Equal("Level 1", wall.Floor, "Wall Floor identity was lost in quantity reporting.");
            Equal("Zone A", wall.Zone, "Wall Zone identity was lost in quantity reporting.");
            Equal("ArchitecturalWall", wall.Category, "Wall category identity was lost in quantity reporting.");
            Equal("FAM-WALL-200", wall.FamilyId, "Wall Family identity was lost in quantity reporting.");
            Equal("BIM3D-GOLDEN-DWG-001", wall.DrawingFingerprint, "Wall drawing provenance was lost in quantity reporting.");
            Equal("AB12", wall.SourceHandles.Single(), "Wall source-handle provenance was lost in quantity reporting.");
            Near(5d, wall.LengthM, "Wall length changed in quantity reporting.");
            Near(3d, wall.GrossConcreteM3, "Wall gross concrete changed in quantity reporting.");
            Near(0.6d, wall.DeductionM3, "Wall deduction changed in quantity reporting.");
            Near(2.4d, wall.NetConcreteM3, "Wall net concrete changed in quantity reporting.");
            Near(24d, wall.FormworkM2, "Wall formwork changed in quantity reporting.");

            var beam = detail.Single(row => row.ElementIds.Count == 1 && row.ElementIds[0] == "B-001");
            Equal("Beam", beam.Category, "Beam category identity was lost in quantity reporting.");
            Equal("FAM-BEAM-300X500", beam.FamilyId, "Beam Family identity was lost in quantity reporting.");
            Equal("CD34", beam.SourceHandles.Single(), "Beam source-handle provenance was lost in quantity reporting.");
            Near(6d, beam.LengthM, "Beam length changed in quantity reporting.");
            Near(0.9d, beam.NetConcreteM3, "Beam net concrete changed in quantity reporting.");
            Near(9.6d, beam.FormworkM2, "Beam formwork changed in quantity reporting.");
        }

        private static void AssertReportParity(System.Collections.Generic.IReadOnlyList<QuantityReportRow> before, System.Collections.Generic.IReadOnlyList<QuantityReportRow> after)
        {
            Equal(before.Count, after.Count, "Quantity row count changed after reopen/recalculation.");
            for (var index = 0; index < before.Count; index++)
            {
                var left = before[index];
                var right = after[index];
                Equal(left.Floor, right.Floor, "Floor projection changed after reopen/recalculation.");
                Equal(left.Zone, right.Zone, "Zone projection changed after reopen/recalculation.");
                Equal(left.Category, right.Category, "Category projection changed after reopen/recalculation.");
                Equal(left.FamilyId, right.FamilyId, "Family projection changed after reopen/recalculation.");
                Equal(left.DrawingFingerprint, right.DrawingFingerprint, "Drawing provenance changed after reopen/recalculation.");
                Equal(string.Join(";", left.ElementIds), string.Join(";", right.ElementIds), "Element provenance changed after reopen/recalculation.");
                Equal(string.Join(";", left.SourceHandles), string.Join(";", right.SourceHandles), "Handle provenance changed after reopen/recalculation.");
                Near(left.LengthM, right.LengthM, "Length changed after reopen/recalculation.");
                Near(left.GrossConcreteM3, right.GrossConcreteM3, "Gross concrete changed after reopen/recalculation.");
                Near(left.DeductionM3, right.DeductionM3, "Deduction changed after reopen/recalculation.");
                Near(left.NetConcreteM3, right.NetConcreteM3, "Net concrete changed after reopen/recalculation.");
                Near(left.FormworkM2, right.FormworkM2, "Formwork changed after reopen/recalculation.");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-9)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
