using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Bim3dUnifiedXlsxAcceptanceSmoke
    {
        private const string Fingerprint = "BIM3D-P0-XLSX-DWG-001";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bim3d-xlsx-" + Guid.NewGuid().ToString("N"));
            var workbookPath = root + ".xlsx";
            var invalidPath = root + "-invalid.xlsx";

            try
            {
                var details = CreateP0Details();
                var summaries = details.Select(Clone).ToList();

                QsCustomerWorkbookExporter.Export(workbookPath, details, summaries);
                VerifyWorkbook(workbookPath, details);
                VerifyScopeMismatchFailsClosed(invalidPath, details, summaries);
            }
            finally
            {
                TryDelete(workbookPath);
                TryDelete(invalidPath);
            }
        }

        private static List<QuantityReportRow> CreateP0Details()
        {
            var rows = new List<QuantityReportRow>
            {
                Row("E-WALL-001", "A101", "ArchitecturalWall", "FAM-WALL-200", "Wall 200", "Concrete", "Level 1", "Zone A"),
                Row("E-BEAM-001", "A102", "Beam", "FAM-BEAM-300X500", "Beam 300x500", "Concrete", "Level 1", "Zone A"),
                Row("E-COLUMN-001", "A103", "Column", "FAM-COLUMN-400", "Column 400", "Concrete", "Level 1", "Zone A"),
                Row("E-SLAB-001", "A104", "Slab", "FAM-SLAB-150", "Slab 150", "Concrete", "Level 1", "Zone A"),
                Row("E-SWALL-001", "A105", "StructuralWall", "FAM-SWALL-250", "Structural Wall 250", "Concrete", "Level 1", "Zone B"),
                Row("E-FOUND-001", "A106", "Foundation", "FAM-FOUND-PAD", "Pad Foundation", "Concrete", "Foundation", "Zone B"),
                Row("E-DOOR-001", "A107", "Door", "FAM-DOOR-900", "Door 900", "Timber", "Level 1", "Zone A"),
                Row("E-OPEN-001", "A108", "WallOpening", "FAM-OPEN-1200", "Opening 1200", string.Empty, "Level 1", "Zone A")
            };

            SetConcrete(rows[0], 3.0, 0.6, 2.4, 24.0, 5.0);
            rows[0].DoorAreaM2 = 2.1;
            rows[0].HasDoorAreaM2Evidence = true;

            SetConcrete(rows[1], 0.9, 0.0, 0.9, 9.6, 6.0);
            rows[1].SideAreaM2 = 6.0;
            rows[1].BottomAreaM2 = 1.8;
            rows[1].HasSideAreaM2Evidence = true;
            rows[1].HasBottomAreaM2Evidence = true;

            SetConcrete(rows[2], 0.64, 0.0, 0.64, 6.4, 4.0);
            rows[2].LengthM = 0.0;
            rows[2].HasLengthMEvidence = false;

            SetConcrete(rows[3], 3.0, 0.0, 3.0, 20.0, 0.0);
            rows[3].LengthM = 0.0;
            rows[3].HasLengthMEvidence = false;
            rows[3].TopAreaM2 = 20.0;
            rows[3].HasTopAreaM2Evidence = true;

            SetConcrete(rows[4], 4.5, 0.9, 3.6, 31.0, 7.5);
            rows[4].DoorAreaM2 = 3.0;
            rows[4].HasDoorAreaM2Evidence = true;

            SetConcrete(rows[5], 1.44, 0.0, 1.44, 7.2, 0.0);
            rows[5].LengthM = 0.0;
            rows[5].HasLengthMEvidence = false;
            rows[5].SideAreaM2 = 5.6;
            rows[5].BottomAreaM2 = 1.6;
            rows[5].HasSideAreaM2Evidence = true;
            rows[5].HasBottomAreaM2Evidence = true;

            rows[6].DoorAreaM2 = 1.89;
            rows[6].HasDoorAreaM2Evidence = true;
            rows[6].Note = "Opening deduction carrier";

            rows[7].DoorAreaM2 = 2.52;
            rows[7].HasDoorAreaM2Evidence = true;
            rows[7].Note = "Wall opening deduction";

            return rows;
        }

        private static QuantityReportRow Row(
            string elementId,
            string handle,
            string category,
            string familyId,
            string familyName,
            string material,
            string floor,
            string zone)
        {
            var row = new QuantityReportRow
            {
                Floor = floor,
                Zone = zone,
                Category = category,
                FamilyId = familyId,
                FamilyName = familyName,
                ElementName = elementId,
                Material = material,
                DrawingFingerprint = Fingerprint,
                Count = 1,
                HasGrossConcreteM3Evidence = false,
                HasDeductionM3Evidence = false,
                HasNetConcreteM3Evidence = false,
                HasFormworkM2Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static void SetConcrete(QuantityReportRow row, double gross, double deduction, double net, double formwork, double length)
        {
            row.GrossConcreteM3 = gross;
            row.DeductionM3 = deduction;
            row.NetConcreteM3 = net;
            row.FormworkM2 = formwork;
            row.LengthM = length;
            row.HasGrossConcreteM3Evidence = true;
            row.HasDeductionM3Evidence = true;
            row.HasNetConcreteM3Evidence = true;
            row.HasFormworkM2Evidence = true;
            row.HasLengthMEvidence = true;
        }

        private static QuantityReportRow Clone(QuantityReportRow source)
        {
            var row = new QuantityReportRow
            {
                Floor = source.Floor,
                Zone = source.Zone,
                Category = source.Category,
                FamilyId = source.FamilyId,
                FamilyName = source.FamilyName,
                ElementName = source.ElementName,
                Material = source.Material,
                Note = source.Note,
                DrawingFingerprint = source.DrawingFingerprint,
                Count = source.Count,
                GrossConcreteM3 = source.GrossConcreteM3,
                DeductionM3 = source.DeductionM3,
                NetConcreteM3 = source.NetConcreteM3,
                FormworkM2 = source.FormworkM2,
                LengthM = source.LengthM,
                OuterPerimeterM = source.OuterPerimeterM,
                InnerPerimeterM = source.InnerPerimeterM,
                DoorAreaM2 = source.DoorAreaM2,
                SideAreaM2 = source.SideAreaM2,
                BottomAreaM2 = source.BottomAreaM2,
                TopAreaM2 = source.TopAreaM2,
                OtherAreaM2 = source.OtherAreaM2,
                HasGrossConcreteM3Evidence = source.HasGrossConcreteM3Evidence,
                HasDeductionM3Evidence = source.HasDeductionM3Evidence,
                HasNetConcreteM3Evidence = source.HasNetConcreteM3Evidence,
                HasFormworkM2Evidence = source.HasFormworkM2Evidence,
                HasLengthMEvidence = source.HasLengthMEvidence,
                HasOuterPerimeterMEvidence = source.HasOuterPerimeterMEvidence,
                HasInnerPerimeterMEvidence = source.HasInnerPerimeterMEvidence,
                HasDoorAreaM2Evidence = source.HasDoorAreaM2Evidence,
                HasSideAreaM2Evidence = source.HasSideAreaM2Evidence,
                HasBottomAreaM2Evidence = source.HasBottomAreaM2Evidence,
                HasTopAreaM2Evidence = source.HasTopAreaM2Evidence,
                HasOtherAreaM2Evidence = source.HasOtherAreaM2Evidence,
                DensityKgM3 = source.DensityKgM3,
                MassKg = source.MassKg
            };
            foreach (var id in source.ElementIds) row.ElementIds.Add(id);
            foreach (var handle in source.SourceHandles) row.SourceHandles.Add(handle);
            return row;
        }

        private static void VerifyWorkbook(string path, IReadOnlyList<QuantityReportRow> details)
        {
            True(File.Exists(path), "Unified P0 customer workbook was not created.");
            using var archive = ZipFile.OpenRead(path);

            var workbook = Read(archive, "xl/workbook.xml");
            Contains(workbook, "name=\"DGKL\"", "DGKL sheet is missing.");
            Contains(workbook, "name=\"COP_PHA\"", "COP_PHA sheet is missing.");
            Contains(workbook, "name=\"CHI_TIET\"", "CHI_TIET sheet is missing.");
            Contains(workbook, "name=\"TRACE_MODEL\"", "TRACE_MODEL sheet is missing.");
            Contains(workbook, "name=\"TRACE_MODEL\" sheetId=\"4\" state=\"hidden\"", "TRACE_MODEL must remain hidden from ordinary QS users.");

            var dgklXml = Read(archive, "xl/worksheets/sheet1.xml");
            Contains(dgklXml, "dimension ref=\"A1:M9\"", "DGKL must contain exactly eight P0 acceptance rows plus the hidden TRACE_KEY column.");
            foreach (var row in details)
            {
                Contains(dgklXml, ">" + row.Category + "<", "DGKL lost category " + row.Category + ".");
                Contains(dgklXml, ">" + row.ElementName + "<", "DGKL lost element name " + row.ElementName + ".");
            }

            var detailXml = Read(archive, "xl/worksheets/sheet3.xml");
            Contains(detailXml, "dimension ref=\"A1:L9\"", "CHI_TIET must contain exactly eight P0 acceptance rows plus the hidden TRACE_KEY column.");
            foreach (var row in details)
            {
                Contains(detailXml, ">" + row.FamilyName + "<", "CHI_TIET lost family/group name " + row.FamilyName + ".");
                Contains(detailXml, ">" + row.ElementName + "<", "CHI_TIET lost element name " + row.ElementName + ".");
            }

            Contains(detailXml, "r=\"E3\"", "Beam length evidence must be exported as a numeric cell.");
            NotContains(detailXml, "r=\"E8\"", "Door length is not applicable and must stay blank instead of exporting a zero cell.");
            NotContains(detailXml, "r=\"H8\"", "Door concrete volume is not applicable and must stay blank instead of exporting a zero cell.");

            var traceXml = Read(archive, "xl/worksheets/sheet4.xml");
            Contains(traceXml, Fingerprint, "TRACE_MODEL lost the drawing fingerprint.");
            foreach (var row in details)
            {
                Contains(traceXml, row.ElementIds.Single(), "TRACE_MODEL lost semantic Element ID " + row.ElementIds.Single() + ".");
                Contains(traceXml, row.SourceHandles.Single(), "TRACE_MODEL lost CAD Handle " + row.SourceHandles.Single() + ".");
            }
        }

        private static void VerifyScopeMismatchFailsClosed(
            string path,
            IReadOnlyList<QuantityReportRow> details,
            IReadOnlyList<QuantityReportRow> summaries)
        {
            var invalid = summaries.Select(Clone).ToList();
            invalid[0].SourceHandles.Clear();
            invalid[0].SourceHandles.Add("BEEF");

            try
            {
                QsCustomerWorkbookExporter.Export(path, details, invalid);
                throw new InvalidOperationException("Customer workbook must reject grouped/detail provenance mismatch.");
            }
            catch (InvalidDataException ex)
            {
                True(ex.Message.IndexOf("scope", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     ex.Message.IndexOf("provenance", StringComparison.OrdinalIgnoreCase) >= 0,
                     "Scope/provenance mismatch failed closed with an unexpected diagnostic: " + ex.Message);
            }
        }

        private static string Read(ZipArchive archive, string entryName)
        {
            var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Workbook entry missing: " + entryName + ".");
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private static void Contains(string text, string token, string message)
        {
            if (text.IndexOf(token, StringComparison.Ordinal) < 0) throw new InvalidOperationException(message);
        }

        private static void NotContains(string text, string token, string message)
        {
            if (text.IndexOf(token, StringComparison.Ordinal) >= 0) throw new InvalidOperationException(message);
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
