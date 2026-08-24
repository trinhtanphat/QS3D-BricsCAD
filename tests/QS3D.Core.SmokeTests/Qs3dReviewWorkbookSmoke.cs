using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
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
            ExcelResavedRelationshipsAndSharedStringsRoundTrip();
            TraceKeyTamperFailsClosed();
            TraceReaderRejectsFormulaIdentityCell();
            BoundedLiveHandleBatchesCoverMoreThanTenThousand();
            CanonicalIssuesProjectWithoutReRunningDetectors();
            CanonicalLifecyclePairMismatchFailsClosed();
            MixedDrawingFailsBeforeReplacingExistingWorkbook();
        }

        private static void ExcelResavedRelationshipsAndSharedStringsRoundTrip()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-excel-resave-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                ExportTraceFixture(path);
                ConvertInlineStringsToSharedStrings(path);
                RemapWorksheetPart(path, "sheet2.xml", "review-qto.xml");
                RemapWorksheetPart(path, "sheet3.xml", "review-clashes.xml");
                RemapWorksheetPart(path, "sheet4.xml", "review-duplicates.xml");

                var traces = new[]
                {
                    Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.QuantitySheet, 2),
                    Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.ClashSheet, 2),
                    Qs3dReviewWorkbookTraceReader.Read(path, Qs3dReviewWorkbookExporter.DuplicateSheet, 2)
                };
                foreach (var trace in traces)
                    Qs3dReviewTraceValidator.ValidateIdentity(trace, "drawing-fp", "REV-TRACE-01");
            }
            finally { TryDelete(path); }
        }

        private static void TraceKeyTamperFailsClosed()
        {
            var cases = new[]
            {
                new { SheetPart = "xl/worksheets/sheet2.xml", Cell = "B2", Value = "EL-QTO-TAMPER", Sheet = Qs3dReviewWorkbookExporter.QuantitySheet },
                new { SheetPart = "xl/worksheets/sheet3.xml", Cell = "K2", Value = "2AF94", Sheet = Qs3dReviewWorkbookExporter.ClashSheet },
                new { SheetPart = "xl/worksheets/sheet4.xml", Cell = "J2", Value = "COL-TAMPER", Sheet = Qs3dReviewWorkbookExporter.DuplicateSheet }
            };
            foreach (var item in cases)
            {
                var path = Path.Combine(Path.GetTempPath(), "qs3d-review-trace-tamper-" + Guid.NewGuid().ToString("N") + ".xlsx");
                try
                {
                    ExportTraceFixture(path);
                    MutateCell(path, item.SheetPart, item.Cell, item.Value, false);
                    var trace = Qs3dReviewWorkbookTraceReader.Read(path, item.Sheet, 2);
                    Throws<InvalidDataException>(() => Qs3dReviewTraceValidator.ValidateIdentity(trace, "drawing-fp", "REV-TRACE-01"));
                }
                finally { TryDelete(path); }
            }
        }

        private static void TraceReaderRejectsFormulaIdentityCell()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-formula-identity-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                ExportTraceFixture(path);
                MutateCell(path, "xl/worksheets/sheet2.xml", "B2", "EL-QTO-01", true);
                Throws<InvalidDataException>(() => Qs3dReviewWorkbookTraceReader.Read(
                    path, Qs3dReviewWorkbookExporter.QuantitySheet, 2));
            }
            finally { TryDelete(path); }
        }

        private static void BoundedLiveHandleBatchesCoverMoreThanTenThousand()
        {
            var handles = Enumerable.Range(1, 10001).Select(value => value.ToString("X")).ToArray();
            var batches = Qs3dReviewLiveHandleBatchPlanner.Create(handles, 4096);
            Equal(3, batches.Count, "live Handle batch count above legacy 10,000 cap");
            Equal(4096, batches[0].Count, "first live Handle batch size");
            Equal(4096, batches[1].Count, "second live Handle batch size");
            Equal(1809, batches[2].Count, "final live Handle batch size");
            if (!batches.SelectMany(batch => batch).SequenceEqual(handles, StringComparer.Ordinal))
                throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: bounded live Handle batches changed order or scope.");
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
                Qs3dReviewTraceValidator.ValidateIdentity(qtoTrace, "drawing-fp", "REV-2026-08-22-01");
                Throws<InvalidDataException>(() => Qs3dReviewTraceValidator.ValidateIdentity(
                    qtoTrace, "other-drawing-fp", "REV-2026-08-22-01"));
                Throws<InvalidDataException>(() => Qs3dReviewTraceValidator.ValidateIdentity(
                    qtoTrace, "drawing-fp", "REV-2026-08-22-02"));
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

        private static void CanonicalIssuesProjectWithoutReRunningDetectors()
        {
            var project = new ProjectState("REVIEW-PROJECT", "Review projection")
            {
                DrawingFingerprint = "review-drawing-fp"
            };
            project.Floors.Add(new FloorDefinition("L01", "Level 01", 0d));
            project.Elements.Add(Element("PIPE-01", ElementCategory.CustomQuantity, "A1"));
            project.Elements.Add(Element("BEAM-01", ElementCategory.Beam, "B2"));
            project.Elements.Add(Element("BEAM-02", ElementCategory.Beam, "C3"));

            var created = new DateTime(2026, 8, 24, 1, 2, 3, DateTimeKind.Utc);
            var drawingId = new DrawingId(Guid.Parse("e8ee83d0-2855-4e50-a647-545f280f75d1"));
            var clash = new CoordinationIssue(
                "ISSUE-CLASH-01", CoordinationIssueKind.ExactHardClash, CoordinationIssueSeverity.Critical,
                "Pipe intersects beam", "PIPE-01", "BEAM-01",
                new CadReference(drawingId, new CadHandle("A1")), new CadReference(drawingId, new CadHandle("B2")),
                "MEP/Structure", "Pipe/Beam", "Supply", "L01", 0d, created, "QS Lead");
            var duplicate = new CoordinationIssue(
                "ISSUE-DUPLICATE-01", CoordinationIssueKind.Review, CoordinationIssueSeverity.High,
                "Duplicate beams", "BEAM-01", "BEAM-02",
                new CadReference(drawingId, new CadHandle("B2")), new CadReference(drawingId, new CadHandle("C3")),
                "Structure", "Beam/Beam", "Structure", "L01", 0d, created.AddMinutes(1), "QS Lead");
            CoordinationIssuePersistence.Save(project, new[] { clash, duplicate }, 7L);
            var snapshot = CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: coordination snapshot was not restored.");

            var projection = Qs3dReviewIssueProjection.Build(project, snapshot);
            Equal(1, projection.Clashes.Count, "canonical clash projection count");
            Equal(1, projection.Duplicates.Count, "canonical duplicate projection count");
            Equal("ISSUE-CLASH-01", projection.Clashes.Single().ClashId, "canonical clash issue id");
            Equal("ISSUE-DUPLICATE-01", projection.Duplicates.Single().DuplicateId, "canonical duplicate issue id");
            Equal(DuplicateMatchKind.None, projection.Duplicates.Single().MatchKinds, "evidence-neutral persisted review projection");
            Equal(string.Empty, projection.Clashes.Single().RuleId, "persisted clash must not fabricate a rule id");
            Equal(string.Empty, projection.Duplicates.Single().RuleId, "persisted review must not fabricate a rule id");
            Equal(2, projection.LifecycleByFindingId.Count, "canonical lifecycle projection count");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-neutral-projection-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var quantity = QuantityRow("review-drawing-fp", "PIPE-01", "A1", 1d, 0d, 1d, 2d);
                Qs3dReviewWorkbookExporter.Export(
                    path, new[] { quantity }, new[] { quantity }, projection.Clashes, projection.Duplicates,
                    null, new Qs3dReviewModelInfo("REVIEW-PROJECT", "review.dwg", "review-drawing-fp", "R1", DateTimeOffset.UtcNow),
                    projection.LifecycleByFindingId);
                var duplicateXml = ReadEntry(path, "xl/worksheets/sheet4.xml");
                Contains(duplicateXml, "ReviewOnly", "persisted review row must disclose evidence-neutral classification");
                if (duplicateXml.Contains("SemanticIdentity", StringComparison.Ordinal) ||
                    duplicateXml.Contains("QS3D_PERSISTED_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Qs3dReviewWorkbookSmoke: persisted review projection fabricated detector/rule evidence.");
            }
            finally { TryDelete(path); }
        }

        private static ProjectElement Element(string id, ElementCategory category, string handle)
        {
            var element = new ProjectElement(id, category)
            {
                FloorId = "L01",
                DrawingFingerprint = "review-drawing-fp"
            };
            element.SourceHandles.Add(handle);
            return element;
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

        private static void ExportTraceFixture(string path)
        {
            var detail = QuantityRow("drawing-fp", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
            var summary = QuantityRow("drawing-fp", "EL-QTO-01", "A", 1d, 0d, 1d, 2d);
            var clash = CoordinationClashExportRow.CreateExactHard(
                "drawing-fp", "2AF93", "2B109", "PIPE-021", "BEAM-104", "Pipe", "Beam", "L03");
            var duplicate = CoordinationDuplicateExportRow.Create(
                "drawing-fp", "COL-055", "2C001", "COL-056", "2C002",
                DuplicateMatchKind.ExactGeometry, "Column", "Column", "L05");
            Qs3dReviewWorkbookExporter.Export(
                path,
                new[] { detail },
                new[] { summary },
                new[] { clash },
                new[] { duplicate },
                null,
                new Qs3dReviewModelInfo("TRACE-PROJECT", "trace.dwg", "drawing-fp", "REV-TRACE-01", DateTimeOffset.UtcNow));
        }

        private static void ConvertInlineStringsToSharedStrings(string path)
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace package = "http://schemas.openxmlformats.org/package/2006/relationships";
            XNamespace content = "http://schemas.openxmlformats.org/package/2006/content-types";
            var strings = new List<string>();
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var worksheets = archive.Entries
                    .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal) &&
                                    entry.FullName.EndsWith(".xml", StringComparison.Ordinal))
                    .Select(entry => entry.FullName)
                    .ToList();
                foreach (var worksheet in worksheets)
                {
                    MutateXmlEntry(archive, worksheet, document =>
                    {
                        foreach (var cell in document.Descendants(ns + "c")
                            .Where(item => string.Equals(item.Attribute("t")?.Value, "inlineStr", StringComparison.Ordinal)).ToList())
                        {
                            var value = string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));
                            int index;
                            if (!indexes.TryGetValue(value, out index))
                            {
                                index = strings.Count;
                                strings.Add(value);
                                indexes.Add(value, index);
                            }
                            cell.Elements(ns + "is").Remove();
                            cell.SetAttributeValue("t", "s");
                            cell.Add(new XElement(ns + "v", index));
                        }
                    });
                }

                var shared = archive.CreateEntry("xl/sharedStrings.xml", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(shared.Open(), new UTF8Encoding(false)))
                    new XDocument(new XElement(ns + "sst",
                        new XAttribute("count", strings.Count),
                        new XAttribute("uniqueCount", strings.Count),
                        strings.Select(value => new XElement(ns + "si", new XElement(ns + "t", value)))))
                        .Save(writer, SaveOptions.DisableFormatting);

                MutateXmlEntry(archive, "xl/_rels/workbook.xml.rels", document =>
                    document.Root!.Add(new XElement(package + "Relationship",
                        new XAttribute("Id", "rIdSharedStrings"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"),
                        new XAttribute("Target", "sharedStrings.xml"))));
                MutateXmlEntry(archive, "[Content_Types].xml", document =>
                    document.Root!.Add(new XElement(content + "Override",
                        new XAttribute("PartName", "/xl/sharedStrings.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"))));
            }
        }

        private static void RemapWorksheetPart(string path, string oldName, string newName)
        {
            XNamespace package = "http://schemas.openxmlformats.org/package/2006/relationships";
            XNamespace content = "http://schemas.openxmlformats.org/package/2006/content-types";
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var oldPath = "xl/worksheets/" + oldName;
                var newPath = "xl/worksheets/" + newName;
                var source = archive.GetEntry(oldPath) ?? throw new InvalidOperationException("Missing worksheet fixture part " + oldPath + ".");
                byte[] bytes;
                using (var input = source.Open())
                using (var memory = new MemoryStream())
                {
                    input.CopyTo(memory);
                    bytes = memory.ToArray();
                }
                source.Delete();
                var replacement = archive.CreateEntry(newPath, CompressionLevel.Optimal);
                using (var output = replacement.Open()) output.Write(bytes, 0, bytes.Length);

                MutateXmlEntry(archive, "xl/_rels/workbook.xml.rels", document =>
                {
                    var relationship = document.Descendants(package + "Relationship")
                        .Single(item => string.Equals(item.Attribute("Target")?.Value.Replace('\\', '/'), "worksheets/" + oldName, StringComparison.Ordinal));
                    relationship.SetAttributeValue("Target", "worksheets/" + newName);
                });
                MutateXmlEntry(archive, "[Content_Types].xml", document =>
                {
                    var entry = document.Descendants(content + "Override")
                        .Single(item => string.Equals(item.Attribute("PartName")?.Value, "/" + oldPath, StringComparison.Ordinal));
                    entry.SetAttributeValue("PartName", "/" + newPath);
                });
            }
        }

        private static void MutateCell(string path, string entryName, string cellReference, string value, bool formula)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                MutateXmlEntry(archive, entryName, document =>
                {
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var cell = document.Descendants(ns + "c")
                        .Single(item => string.Equals(item.Attribute("r")?.Value, cellReference, StringComparison.Ordinal));
                    var text = cell.Descendants(ns + "t").Single();
                    text.Value = value;
                    if (formula) cell.AddFirst(new XElement(ns + "f", "1+1"));
                });
            }
        }

        private static void MutateXmlEntry(ZipArchive archive, string entryName, Action<XDocument> mutation)
        {
            var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Missing XLSX fixture entry " + entryName + ".");
            XDocument document;
            using (var stream = entry.Open()) document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            mutation(document);
            entry.Delete();
            var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false)))
                document.Save(writer, SaveOptions.DisableFormatting);
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var archive = ZipFile.OpenRead(path)) return Read(archive, entryName);
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
