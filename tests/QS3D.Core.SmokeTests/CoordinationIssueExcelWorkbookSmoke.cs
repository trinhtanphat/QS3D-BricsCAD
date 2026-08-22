using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIssueExcelWorkbookSmoke
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        internal static void Run()
        {
            RoundTripPreservesCanonicalIssueAndNoOpPlan();
            EditableWorkbookProducesLifecyclePlan();
            ImmutableTraceTamperFailsClosed();
            Console.WriteLine("PASS coordination issue XLSX provenance round-trip");
        }

        private static void RoundTripPreservesCanonicalIssueAndNoOpPlan()
        {
            var snapshot = CreateSnapshot(21L);
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-issues-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                CoordinationIssueExcelWorkbook.Export(path, snapshot);
                var plan = CoordinationIssueExcelWorkbook.ReadAndPlanImport(
                    path,
                    snapshot,
                    snapshot.Issues[0].UpdatedAtUtc.AddMinutes(1));
                if (plan.ChangedIssueCount != 0 || plan.Issues.Count != 1)
                    throw new InvalidOperationException("Unedited coordination issue workbook produced unexpected mutations.");
                if (plan.SourceRevision != snapshot.Revision || plan.NextRevision != snapshot.Revision)
                    throw new InvalidOperationException("Unedited coordination issue workbook advanced the persistence revision.");
                if (!string.Equals(plan.Issues[0].IssueId, snapshot.Issues[0].IssueId, StringComparison.Ordinal) ||
                    plan.Issues[0].Status != snapshot.Issues[0].Status ||
                    plan.Issues[0].Severity != snapshot.Issues[0].Severity)
                    throw new InvalidOperationException("Coordination issue workbook round-trip changed canonical issue state.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void EditableWorkbookProducesLifecyclePlan()
        {
            var snapshot = CreateSnapshot(23L);
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-issues-edit-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                CoordinationIssueExcelWorkbook.Export(path, snapshot);
                ReplaceWorksheetCells(path, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["E2"] = CoordinationIssueStatus.InReview.ToString(),
                    ["F2"] = CoordinationIssueSeverity.Critical.ToString(),
                    ["G2"] = "Coordination Lead",
                    ["H2"] = "QS Lead",
                    ["I2"] = "Accepted from edited workbook"
                });

                var plan = CoordinationIssueExcelWorkbook.ReadAndPlanImport(
                    path,
                    snapshot,
                    snapshot.Issues[0].UpdatedAtUtc.AddMinutes(2));
                if (plan.ChangedIssueCount != 1 || plan.SourceRevision != 23L || plan.NextRevision != 24L)
                    throw new InvalidOperationException("Edited coordination workbook did not produce exactly one next-revision change.");

                var changed = plan.Issues.Single();
                if (changed.Status != CoordinationIssueStatus.InReview || changed.Severity != CoordinationIssueSeverity.Critical)
                    throw new InvalidOperationException("Edited coordination workbook did not apply status/severity through the XLSX parser.");
                if (!string.Equals(changed.Assignee, "Coordination Lead", StringComparison.Ordinal))
                    throw new InvalidOperationException("Edited coordination workbook did not apply assignee through the XLSX parser.");
                if (changed.Comments.Count != 1 ||
                    !string.Equals(changed.Comments[0].Author, "QS Lead", StringComparison.Ordinal) ||
                    !string.Equals(changed.Comments[0].Text, "Accepted from edited workbook", StringComparison.Ordinal))
                    throw new InvalidOperationException("Edited coordination workbook did not append the canonical comment through the XLSX parser.");

                var source = snapshot.Issues.Single();
                if (source.Status != CoordinationIssueStatus.Open || source.Severity != CoordinationIssueSeverity.Medium ||
                    !string.Equals(source.Assignee, "Coordinator", StringComparison.Ordinal) || source.Comments.Count != 0)
                    throw new InvalidOperationException("Edited workbook parsing mutated the canonical source snapshot before persistence commit.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void ImmutableTraceTamperFailsClosed()
        {
            var snapshot = CreateSnapshot(22L);
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-issues-tamper-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                CoordinationIssueExcelWorkbook.Export(path, snapshot);
                ReplaceWorksheetText(path, "Hard clash workbook", "Tampered immutable title");
                Expect<InvalidDataException>(() => CoordinationIssueExcelWorkbook.ReadAndPlanImport(
                    path,
                    snapshot,
                    snapshot.Issues[0].UpdatedAtUtc.AddMinutes(1)));
                if (!string.Equals(snapshot.Issues[0].Title, "Hard clash workbook", StringComparison.Ordinal))
                    throw new InvalidOperationException("Rejected workbook tamper mutated the source issue.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static CoordinationIssuePersistenceSnapshot CreateSnapshot(long revision)
        {
            var project = new ProjectState("project-issue-workbook", "Coordination Issue Workbook Smoke")
            {
                DrawingFingerprint = "DRAWING-ISSUE-WORKBOOK"
            };
            var drawingId = new DrawingId(Guid.Parse("04a8a08c-2cd7-4c88-b1cf-d7c80c633a9f"));
            var created = new DateTime(2026, 8, 22, 4, 0, 0, DateTimeKind.Utc);
            var issue = new CoordinationIssue(
                "issue-workbook-001",
                CoordinationIssueKind.ClearanceClash,
                CoordinationIssueSeverity.Medium,
                "Hard clash workbook",
                "semantic-workbook-left",
                "semantic-workbook-right",
                new CadReference(drawingId, new CadHandle("11AA")),
                new CadReference(drawingId, new CadHandle("22BB")),
                "MEP/Structure",
                "Duct/Beam",
                "Supply",
                "Level-02",
                0.025d,
                created,
                "Coordinator");
            CoordinationIssuePersistence.Save(project, new[] { issue }, revision);
            return CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Coordination workbook snapshot was not restored.");
        }

        private static void ReplaceWorksheetCells(string path, IReadOnlyDictionary<string, string> replacements)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet2.xml")
                    ?? throw new InvalidOperationException("ISSUES worksheet package part was not found.");
                XDocument document;
                using (var stream = entry.Open()) document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);

                foreach (var replacement in replacements)
                {
                    var cell = document.Descendants(SpreadsheetNs + "c")
                        .SingleOrDefault(x => string.Equals((string?)x.Attribute("r"), replacement.Key, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Expected workbook cell was not found: " + replacement.Key + ".");
                    var text = cell.Descendants(SpreadsheetNs + "t").SingleOrDefault()
                        ?? throw new InvalidOperationException("Expected inline string cell was not found: " + replacement.Key + ".");
                    text.Value = replacement.Value;
                }

                entry.Delete();
                var replacementEntry = archive.CreateEntry("xl/worksheets/sheet2.xml", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(replacementEntry.Open(), new UTF8Encoding(false)))
                    document.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        private static void ReplaceWorksheetText(string path, string expected, string replacement)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet2.xml")
                    ?? throw new InvalidOperationException("ISSUES worksheet package part was not found.");
                string xml;
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                    xml = reader.ReadToEnd();
                if (xml.IndexOf(expected, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Expected immutable workbook value was not found for tamper smoke.");
                xml = xml.Replace(expected, replacement);
                entry.Delete();
                var replacementEntry = archive.CreateEntry("xl/worksheets/sheet2.xml", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(replacementEntry.Open(), new UTF8Encoding(false))) writer.Write(xml);
            }
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try
            {
                action();
                throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
            }
            catch (T)
            {
            }
        }
    }
}
