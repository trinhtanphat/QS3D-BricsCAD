using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Coordination;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SixSheetWorkbookRoundTripsAllTraceKinds();
            MixedDrawingFailsBeforeReplacingExistingWorkbook();
        }

        private static void SixSheetWorkbookRoundTripsAllTraceKinds()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-six-sheet-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var detail = QuantityRow("drawing-fp", "EL-QTO-01", "00A", 3.2d, 0.2d, 3.0d, 12.5d);
                var summary = QuantityRow("drawing-fp", "EL-QTO-01", "00A", 3.2d, 0.2d, 3.0d, 12.5d);
                var clash = CoordinationClashExportRow.CreateExactHard(
                    "drawing-fp", "2AF93", "2B109", "PIPE-021", "BEAM-104", "Pipe", "Beam", "L03");
                var duplicate = CoordinationDuplicateExportRow.Create(
                    "drawing-fp", "COL-055", "2C001", "COL-056", "2C002",
                    DuplicateMatchKind.ExactGeometry, "Column", "Column", "L05");
                var profile = new CoordinationRuleProfile(
                    "PROJECT-QS", 3,
                    new[] { new CoordinationRule("PIPE_BEAM_HARD", 2, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Critical", 0d) });
                var created = new DateTimeOffset(2026, 8, 22, 6, 0, 0, TimeSpan.Zero);
                var model = new Qs3dReviewModelInfo("ABC-TOWER", "Tower_A.dwg", "drawing-fp", "REV-2026-08-22-01", created, 186.42d);
                var metadata = new[]
                {
                    new Qs3dReviewIssueMetadata(clash.ClashId, "Open", "Critical", 0.02d, 0.03d, 0.04d, createdAtUtc: created, lastCheckedAtUtc: created.AddMinutes(5), comment: "review clash"),
                    new Qs3dReviewIssueMetadata(duplicate.DuplicateId, "Reviewed", distanceMm: 0d, rotationDeltaDegrees: 0d, confidencePercent: 100d, createdAtUtc: created, lastCheckedAtUtc: created.AddMinutes(7), comment: "exact duplicate")
                };

                Qs3dReviewWorkbookExporter.Export(
                    path, new[] { detail }, new[] { summary }, new[] { clash }, new[] { duplicate }, profile, model, metadata);

                var qtoTrace = Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.QuantitySheet, 2);
                var clashTrace = Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.ClashSheet, 2);
                var duplicateTrace = Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.DuplicateSheet, 2);
                Equal(Qs3dReviewTraceKind.Quantity, qtoTrace.Kind, "QTO trace kind");
                Equal("EL-QTO-01", qtoTrace.ElementIds.Single(), "QTO semantic id");
                Equal("00A", qtoTrace.Handles.Single(), "QTO current handle");
                Equal("drawing-fp", clashTrace.DrawingFingerprint, "clash fingerprint");
                Equal(2, clashTrace.Handles.Count, "clash handle count");
                Equal("REV-2026-08-22-01", duplicateTrace.ModelRevision, "duplicate model revision");
                Equal(2, duplicateTrace.ElementIds.Count, "duplicate semantic pair count");

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
                {
                    var workbook = Read(archive, "xl/workbook.xml");
                    var expectedOrder = "01_TONG_HOP|02_CHI_TIET_QTO|03_CLASHES|04_DUPLICATES|05_RULES|06_MODEL_INFO";
                    var actualOrder = string.Join("|", new[]
                    {
                        "01_TONG_HOP", "02_CHI_TIET_QTO", "03_CLASHES", "04_DUPLICATES", "05_RULES", "06_MODEL_INFO"
                    }.Where(name => workbook.Contains("name=\"" + name + "\"", StringComparison.Ordinal)));
                    Equal(expectedOrder, actualOrder, "six-sheet order/presence");

                    var qtoXml = Read(archive, "xl/worksheets/sheet2.xml");
                    if (qtoXml.Contains("<c r=\"O2\"", StringComparison.Ordinal))
                        throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: no-evidence LengthM must be a blank cell, not numeric zero.");
                    if (!qtoXml.Contains("<col min=\"25\" max=\"28\" hidden=\"1\"", StringComparison.Ordinal))
                        throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: QTO technical trace columns must stay hidden in the customer view.");
                }
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void MixedDrawingFailsBeforeReplacingExistingWorkbook()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-six-sheet-atomic-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                File.WriteAllText(path, "KEEP-ME", Encoding.UTF8);
                var detail = QuantityRow("drawing-a", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
                var summary = QuantityRow("drawing-a", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
                var foreignClash = CoordinationClashExportRow.CreateExactHard(
                    "drawing-b", "B", "C", "EL-B", "EL-C", "Pipe", "Beam", "L01");
                var model = new Qs3dReviewModelInfo("P", "A.dwg", "drawing-a", "R1", DateTimeOffset.UtcNow);

                Throws<InvalidDataException>(() => Qs3dReviewWorkbookExporter.Export(
                    path, new[] { detail }, new[] { summary }, new[] { foreignClash }, Array.Empty<CoordinationDuplicateExportRow>(), null, model));
                if (!File.ReadAllText(path, Encoding.UTF8).Contains("KEEP-ME", StringComparison.Ordinal))
                    throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: fail-closed validation replaced the existing workbook.");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static QuantityReportRow QuantityRow(string fingerprint, string elementId, string handle, double gross, double deduction, double net, double formwork)
        {
            var row = new QuantityReportRow
            {
                Floor = "L03",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                ElementName = elementId,
                Material = "Concrete",
                DrawingFingerprint = fingerprint,
                Count = 1,
                GrossConcreteM3 = gross,
                DeductionM3 = deduction,
                NetConcreteM3 = net,
                FormworkM2 = formwork,
                HasGrossConcreteM3Evidence = true,
                HasDeductionM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
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

        private static string Read(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: missing " + path + ".");
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, true)) return reader.ReadToEnd();
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
