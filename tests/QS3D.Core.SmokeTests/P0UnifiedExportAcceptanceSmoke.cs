using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class P0UnifiedExportAcceptanceSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-p0-export-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var details = BuildP0Rows();
                var summaries = details.Select(Clone).ToArray();
                var path = Path.Combine(root, "p0-unified-export.xlsx");
                QsCustomerWorkbookExporter.Export(path, details, summaries);

                var dgklXml = ReadEntry(path, "xl/worksheets/sheet1.xml");
                var detailXml = ReadEntry(path, "xl/worksheets/sheet3.xml");
                foreach (var row in details)
                {
                    Require(dgklXml.Contains(">" + row.Category + "</t>"), "DGKL lost P0 category: " + row.Category + ".");
                    Require(dgklXml.Contains(">" + row.Material + "</t>"), "DGKL lost material/concrete-grade identity for " + row.Category + ".");
                    Require(detailXml.Contains(">" + row.FamilyName + "</t>"), "CHI_TIET lost family/group display identity: " + row.FamilyName + ".");
                    Require(detailXml.Contains(">" + row.ElementName + "</t>"), "CHI_TIET lost element display identity: " + row.ElementName + ".");
                    Require(detailXml.Contains(">" + row.Floor + "</t>"), "CHI_TIET lost floor identity for " + row.Category + ".");
                }

                for (var index = 0; index < details.Count; index++)
                {
                    var expected = details[index];
                    var trace = QsCustomerWorkbookTraceReader.Read(path, QsCustomerWorkbookExporter.DetailSheet, index + 2);
                    Require(trace.ElementIds.Count == 1 && trace.ElementIds[0] == expected.ElementIds[0],
                        "P0 export trace lost semantic Element ID for " + expected.Category + ".");
                    Require(trace.Handles.Count == 1 && trace.Handles[0] == expected.SourceHandles[0],
                        "P0 export trace lost CAD Handle for " + expected.Category + ".");
                    Require(trace.DrawingFingerprint == expected.DrawingFingerprint,
                        "P0 export trace lost drawing fingerprint for " + expected.Category + ".");
                }

                Require(!detailXml.Contains("r=\"H8\""), "Door gross concrete must remain blank when no evidence exists.");
                Require(!detailXml.Contains("r=\"K8\""), "Door formwork must remain blank when no evidence exists.");
                Require(!detailXml.Contains("r=\"H9\""), "WallOpening gross concrete must remain blank when no evidence exists.");
                Require(!detailXml.Contains("r=\"K9\""), "WallOpening formwork must remain blank when no evidence exists.");
                Require(!detailXml.Contains("r=\"O8\""), "CHI_TIET sample-layout contract must not reintroduce legacy opening-area columns.");
                Require(!detailXml.Contains("r=\"O9\""), "CHI_TIET sample-layout contract must not reintroduce legacy opening-area columns.");

                var traceXml = ReadEntry(path, "xl/worksheets/sheet4.xml");
                Require(Count(traceXml, "DWG-P0-EXPORT") == 22,
                    "TRACE_MODEL must cover 8 DGKL, 6 FormworkM2-backed COP_PHA, and 8 CHI_TIET P0 projections.");
                Console.WriteLine("PASS P0 unified export acceptance");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static IReadOnlyList<QuantityReportRow> BuildP0Rows()
        {
            var categories = new[] { "ArchitecturalWall", "Beam", "Column", "Slab", "StructuralWall", "Foundation", "Door", "WallOpening" };
            var rows = new List<QuantityReportRow>(categories.Length);
            for (var index = 0; index < categories.Length; index++)
            {
                var category = categories[index];
                var isOpening = category == "Door" || category == "WallOpening";
                var row = new QuantityReportRow
                {
                    Floor = "L0" + (index + 1), Zone = "Z" + (index + 1), Category = category,
                    FamilyId = "F-P0-" + category.ToUpperInvariant(), FamilyName = category + " Family",
                    ElementName = category + " #1", Material = isOpening ? "Opening" : "Concrete",
                    DrawingFingerprint = "DWG-P0-EXPORT", Count = 1,
                    GrossConcreteM3 = isOpening ? 0d : index + 1d, NetConcreteM3 = isOpening ? 0d : index + 0.5d,
                    FormworkM2 = isOpening ? 0d : index + 10d, DoorAreaM2 = isOpening ? index + 2d : 0d,
                    HasGrossConcreteM3Evidence = !isOpening, HasDeductionM3Evidence = false,
                    HasNetConcreteM3Evidence = !isOpening, HasFormworkM2Evidence = !isOpening,
                    HasLengthMEvidence = false, HasOuterPerimeterMEvidence = false, HasInnerPerimeterMEvidence = false,
                    HasDoorAreaM2Evidence = isOpening, HasSideAreaM2Evidence = false, HasBottomAreaM2Evidence = false,
                    HasTopAreaM2Evidence = false, HasOtherAreaM2Evidence = false
                };
                row.ElementIds.Add("P0-" + (index + 1));
                row.SourceHandles.Add((0xA0 + index).ToString("X"));
                rows.Add(row);
            }
            return rows.AsReadOnly();
        }

        private static QuantityReportRow Clone(QuantityReportRow source)
        {
            var row = new QuantityReportRow
            {
                Floor = source.Floor, Zone = source.Zone, Category = source.Category, FamilyId = source.FamilyId,
                FamilyName = source.FamilyName, ElementName = source.ElementName, Material = source.Material,
                DrawingFingerprint = source.DrawingFingerprint, Count = source.Count, GrossConcreteM3 = source.GrossConcreteM3,
                DeductionM3 = source.DeductionM3, NetConcreteM3 = source.NetConcreteM3, FormworkM2 = source.FormworkM2,
                LengthM = source.LengthM, OuterPerimeterM = source.OuterPerimeterM, InnerPerimeterM = source.InnerPerimeterM,
                DoorAreaM2 = source.DoorAreaM2, SideAreaM2 = source.SideAreaM2, BottomAreaM2 = source.BottomAreaM2,
                TopAreaM2 = source.TopAreaM2, OtherAreaM2 = source.OtherAreaM2,
                HasGrossConcreteM3Evidence = source.HasGrossConcreteM3Evidence, HasDeductionM3Evidence = source.HasDeductionM3Evidence,
                HasNetConcreteM3Evidence = source.HasNetConcreteM3Evidence, HasFormworkM2Evidence = source.HasFormworkM2Evidence,
                HasLengthMEvidence = source.HasLengthMEvidence, HasOuterPerimeterMEvidence = source.HasOuterPerimeterMEvidence,
                HasInnerPerimeterMEvidence = source.HasInnerPerimeterMEvidence, HasDoorAreaM2Evidence = source.HasDoorAreaM2Evidence,
                HasSideAreaM2Evidence = source.HasSideAreaM2Evidence, HasBottomAreaM2Evidence = source.HasBottomAreaM2Evidence,
                HasTopAreaM2Evidence = source.HasTopAreaM2Evidence, HasOtherAreaM2Evidence = source.HasOtherAreaM2Evidence
            };
            foreach (var id in source.ElementIds) row.ElementIds.Add(id);
            foreach (var handle in source.SourceHandles) row.SourceHandles.Add(handle);
            return row;
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                using (var reader = new StreamReader(entry.Open())) return reader.ReadToEnd();
            }
        }

        private static int Count(string value, string token)
        {
            var count = 0; var offset = 0;
            while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0) { count++; offset += token.Length; }
            return count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
