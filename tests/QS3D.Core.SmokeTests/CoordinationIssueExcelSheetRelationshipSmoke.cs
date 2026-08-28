using System;
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
    internal static class CoordinationIssueExcelSheetRelationshipSmoke
    {
        private const string WorksheetRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        internal static void Run()
        {
            CanonicalWorkbookRoundTripRemainsAccepted();
            WrongRelationshipTypeFailsClosed();
            ExternalRelationshipFailsClosed();
            DuplicateRelationshipIdFailsClosed();
            ParentTraversalTargetFailsClosed();
            Console.WriteLine("PASS coordination issue XLSX sheet relationship integrity");
        }

        private static void CanonicalWorkbookRoundTripRemainsAccepted()
        {
            var snapshot = CreateSnapshot(31L);
            var path = TempWorkbookPath("canonical");
            try
            {
                CoordinationIssueExcelWorkbook.Export(path, snapshot);
                var plan = Read(path, snapshot);
                if (plan.ChangedIssueCount != 0 || plan.SourceRevision != snapshot.Revision || plan.NextRevision != snapshot.Revision)
                    throw new InvalidOperationException("Canonical coordination issue workbook stopped round-tripping without mutation.");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void WrongRelationshipTypeFailsClosed()
        {
            AssertTamperRejected("wrong-type", (archive, relationships) =>
            {
                var relationship = RequiredRelationship(relationships, "rId1");
                relationship.SetAttributeValue("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
            });
        }

        private static void ExternalRelationshipFailsClosed()
        {
            AssertTamperRejected("external", (archive, relationships) =>
            {
                var relationship = RequiredRelationship(relationships, "rId1");
                relationship.SetAttributeValue("TargetMode", "External");
            });
        }

        private static void DuplicateRelationshipIdFailsClosed()
        {
            AssertTamperRejected("duplicate-id", (archive, relationships) =>
            {
                var relationship = RequiredRelationship(relationships, "rId1");
                relationships.Root!.Add(new XElement(relationship));
            });
        }

        private static void ParentTraversalTargetFailsClosed()
        {
            AssertTamperRejected("parent-traversal", (archive, relationships) =>
            {
                var source = archive.GetEntry("xl/worksheets/sheet1.xml")
                    ?? throw new InvalidOperationException("Canonical META worksheet package part was not found.");
                string xml;
                using (var stream = source.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                    xml = reader.ReadToEnd();

                var traversalEntry = archive.CreateEntry("xl/worksheets/../worksheets/sheet1.xml", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(traversalEntry.Open(), new UTF8Encoding(false)))
                    writer.Write(xml);

                var relationship = RequiredRelationship(relationships, "rId1");
                relationship.SetAttributeValue("Target", "worksheets/../worksheets/sheet1.xml");
            });
        }

        private static void AssertTamperRejected(string name, Action<ZipArchive, XDocument> tamper)
        {
            var snapshot = CreateSnapshot(32L);
            var path = TempWorkbookPath(name);
            try
            {
                CoordinationIssueExcelWorkbook.Export(path, snapshot);
                TamperRelationships(path, tamper);
                Expect<InvalidDataException>(() => Read(path, snapshot));
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static CoordinationIssueExcelImportPlan Read(string path, CoordinationIssuePersistenceSnapshot snapshot)
        {
            return CoordinationIssueExcelWorkbook.ReadAndPlanImport(
                path,
                snapshot,
                snapshot.Issues[0].UpdatedAtUtc.AddMinutes(1));
        }

        private static void TamperRelationships(string path, Action<ZipArchive, XDocument> tamper)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("xl/_rels/workbook.xml.rels")
                    ?? throw new InvalidOperationException("Workbook relationship package part was not found.");
                XDocument relationships;
                using (var stream = entry.Open())
                    relationships = XDocument.Load(stream, LoadOptions.PreserveWhitespace);

                tamper(archive, relationships);

                entry.Delete();
                var replacement = archive.CreateEntry("xl/_rels/workbook.xml.rels", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false)))
                    relationships.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        private static XElement RequiredRelationship(XDocument relationships, string id)
        {
            return relationships.Root?.Elements(PackageRelationshipNs + "Relationship")
                .SingleOrDefault(x => string.Equals((string?)x.Attribute("Id"), id, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Expected workbook relationship was not found: " + id + ".");
        }

        private static CoordinationIssuePersistenceSnapshot CreateSnapshot(long revision)
        {
            var project = new ProjectState("project-sheet-rel-smoke", "Coordination Sheet Relationship Smoke")
            {
                DrawingFingerprint = "DRAWING-SHEET-REL-SMOKE"
            };
            var drawingId = new DrawingId(Guid.Parse("4bf7ef08-fbcb-4d29-a0b4-4422d8de0b72"));
            var created = new DateTime(2026, 8, 28, 1, 0, 0, DateTimeKind.Utc);
            var issue = new CoordinationIssue(
                "issue-sheet-rel-001",
                CoordinationIssueKind.ClearanceClash,
                CoordinationIssueSeverity.Medium,
                "Sheet relationship integrity",
                "semantic-sheet-rel-left",
                "semantic-sheet-rel-right",
                new CadReference(drawingId, new CadHandle("31AA")),
                new CadReference(drawingId, new CadHandle("31BB")),
                "MEP/Structure",
                "Duct/Beam",
                "Supply",
                "Level-03",
                0.03d,
                created,
                "Coordinator");
            CoordinationIssuePersistence.Save(project, new[] { issue }, revision);
            return CoordinationIssuePersistence.Load(project)
                ?? throw new InvalidOperationException("Coordination sheet relationship smoke snapshot was not restored.");
        }

        private static string TempWorkbookPath(string suffix)
        {
            return Path.Combine(
                Path.GetTempPath(),
                "qs3d-coordination-sheet-rel-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }

        private static void Expect<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
