using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SixSheetWorkbookRoundTripsAllTraceKinds();
            CanonicalLifecyclePairMismatchFailsClosed();
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
                var lifecycleRow = LifecycleRow(
                    "ABC-TOWER", "drawing-fp", "ISSUE-CLASH-001",
                    clash.LeftElementId, clash.LeftHandle, clash.RightElementId, clash.RightHandle,
                    created.UtcDateTime);
                var lifecycle = new Dictionary<string, CoordinationIssueExcelRow>(StringComparer.OrdinalIgnoreCase)
                {
                    { clash.ClashId, lifecycleRow }
                };
                var geometry = new[]
                {
                    new Qs3dReviewIssueGeometry(
                        clash.ClashId, 0.02d, 0.03d, 0.04d,
                        createdAtUtc: created, lastCheckedAtUtc: created.AddMinutes(5)),
                    new Qs3dReviewIssueGeometry(
                        duplicate.DuplicateId,
                        distanceMm: 0d, rotationDeltaDegrees: 0d, confidencePercent: 100d,
                        createdAtUtc: created, lastCheckedAtUtc: created.AddMinutes(7))
                };

                Qs3dReviewWorkbookExporter.Export(
                    path, new[] { detail }, new[] { summary }, new[] { clash }, new[] { duplicate }, profile, model, lifecycle, geometry);

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

                    var clashXml = Read(archive, "xl/worksheets/sheet3.xml");
                    Contains(clashXml, "ISSUE-CLASH-001", "canonical issue id was not projected into CLASHES");
                    Contains(clashXml, "InReview", "canonical issue status was not projected into CLASHES");
                    Contains(clashXml, "Critical", "canonical issue severity was not projected into CLASHES");
                    Contains(clashXml, "Coordination Lead", "canonical issue assignee was not projected into CLASHES");
                    if (!clashXml.Contains("<c r=\"R2\"", StringComparison.Ordinal) ||
                        !clashXml.Contains("<c r=\"S2\"", StringComparison.Ordinal) ||
                        !clashXml.Contains("<c r=\"T2\"", StringComparison.Ordinal))
                        throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: clash overlap geometry evidence was not exported.");

                    var duplicateXml = Read(archive, "xl/worksheets/sheet4.xml");
                    if (!duplicateXml.Contains("<c r=\"R2\"", StringComparison.Ordinal) ||
                        !duplicateXml.Contains("<c r=\"S2\"", StringComparison.Ordinal) ||
                        !duplicateXml.Contains("<c r=\"T2\"", StringComparison.Ordinal))
                        throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: duplicate geometry evidence was not exported.");
                }
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void CanonicalLifecyclePairMismatchFailsClosed()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-lifecycle-mismatch-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var detail = QuantityRow("drawing-fp", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
                var summary = QuantityRow("drawing-fp", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
                var clash = CoordinationClashExportRow.CreateExactHard(
                    "drawing-fp", "B", "C", "EL-B", "EL-C", "Pipe", "Beam", "L01");
                var model = new Qs3dReviewModelInfo("P", "A.dwg", "drawing-fp", "R1", DateTimeOffset.UtcNow);
                var wrong = LifecycleRow(
                    "P", "drawing-fp", "ISSUE-WRONG-PAIR",
                    "OTHER-A", "B", "OTHER-B", "C",
                    DateTime.UtcNow.AddMinutes(-5));
                var lifecycle = new Dictionary<string, CoordinationIssueExcelRow>(StringComparer.OrdinalIgnoreCase)
                {
                    { clash.ClashId, wrong }
                };

                Throws<InvalidDataException>(() => Qs3dReviewWorkbookExporter.Export(
                    path, new[] { detail }, new[] { summary }, new[] { clash }, Array.Empty<CoordinationDuplicateExportRow>(), null, model, lifecycle));
                if (File.Exists(path))
                    throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: lifecycle semantic mismatch created a workbook instead of failing closed.");
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

        private static CoordinationIssueExcelRow LifecycleRow(
            string projectId,
            string fingerprint,
            string issueId,
            string leftSemanticId,
            string leftHandle,
            string rightSemanticId,
            string rightHandle,
            DateTime createdAtUtc)
        {
            if (createdAtUtc.Kind != DateTimeKind.Utc) createdAtUtc = createdAtUtc.ToUniversalTime();
            var drawingId = new DrawingId(Guid.Parse("a320e15f-221c-4c7c-b8d3-1c1df35ca70e"));
            var issue = new CoordinationIssue(
                issueId,
                CoordinationIssueKind.HardClash,
                CoordinationIssueSeverity.Critical,
                "Coordination review " + issueId,
                leftSemanticId,
                rightSemanticId,
                new CadReference(drawingId, new CadHandle(leftHandle)),
                new CadReference(drawingId, new CadHandle(rightHandle)),
                "MEP/Structure",
                "Pipe/Beam",
                "Supply",
                "L03",
                0d,
                createdAtUtc,
                "Coordination Lead");
            issue.TransitionTo(CoordinationIssueStatus.InReview, createdAtUtc.AddMinutes(2));

            var project = new ProjectState(projectId, "QS3D Review Workbook Smoke")
            {
                DrawingFingerprint = fingerprint
            };
            CoordinationIssuePersistence.Save(project, new[] { issue }, 9L);
            var snapshot = CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: canonical coordination snapshot was not restored.");
            return CoordinationIssueExcelLifecycle.Project(snapshot).Single();
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

        private static void Contains(string text, string token, string message)
        {
            if (!text.Contains(token, StringComparison.Ordinal))
                throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: " + message + ".");
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
