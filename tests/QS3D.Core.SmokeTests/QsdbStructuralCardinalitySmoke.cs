using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbStructuralCardinalitySmoke
    {
        private const int MaxTopLevelEntries = 100000;
        private const int MaxNestedEntries = 10000;
        private const string Timestamp = "2026-08-25T00:00:00.0000000Z";

        public static void Run()
        {
            OversizedTopLevelZonesAreRejected();
            OversizedNestedHandlesAreRejected();
            ExactNestedHandleBoundaryLoads();
            OversizedInMemorySaveIsRejectedBeforeMutation();
        }

        private static void OversizedTopLevelZonesAreRejected()
        {
            var document = NewDocument();
            var zones = document.Root!.Element("zones")!;
            for (var index = 0; index <= MaxTopLevelEntries; index++)
                zones.Add(new XElement("zone", new XAttribute("id", "Z" + index), new XAttribute("name", "Zone " + index)));

            ExpectInvalid(document, "zones", MaxTopLevelEntries);
        }

        private static void OversizedNestedHandlesAreRejected()
        {
            var document = NewDocument();
            document.Root!.Element("elements")!.Add(NewElement(MaxNestedEntries + 1));
            ExpectInvalid(document, "element E1 handles", MaxNestedEntries);
        }

        private static void ExactNestedHandleBoundaryLoads()
        {
            var document = NewDocument();
            document.Root!.Element("elements")!.Add(NewElement(MaxNestedEntries));

            var path = TempPath();
            try
            {
                document.Save(path, SaveOptions.DisableFormatting);
                var loaded = new QsdbProjectStore().Load(path);
                var element = loaded.Elements.Single();
                Require(element.SourceHandles.Count == MaxNestedEntries,
                    "QSDB exact nested cardinality boundary did not load all source handles.");
                Require(element.SourceHandles[0] == "H0" && element.SourceHandles[MaxNestedEntries - 1] == "H9999",
                    "QSDB exact nested cardinality boundary changed source-handle ordering or values.");
            }
            finally
            {
                Delete(path);
            }
        }

        private static void OversizedInMemorySaveIsRejectedBeforeMutation()
        {
            var project = new ProjectState("save-cardinality-smoke", "Save cardinality smoke");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            for (var index = 0; index <= MaxNestedEntries; index++)
                element.SourceHandles.Add("H" + index);
            project.Elements.Add(element);

            var beforeSchema = project.SchemaVersion;
            var beforeUpdated = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var path = TempPath();
            try
            {
                var rejected = false;
                try
                {
                    new QsdbProjectStore().Save(project, path);
                }
                catch (InvalidDataException ex)
                {
                    rejected = ex.Message.IndexOf("element E1 handles", StringComparison.OrdinalIgnoreCase) >= 0 &&
                               ex.Message.IndexOf(MaxNestedEntries.ToString(), StringComparison.Ordinal) >= 0;
                }

                Require(rejected, "QSDB save accepted oversized in-memory nested cardinality.");
                Require(project.SchemaVersion == beforeSchema && project.UpdatedUtc == beforeUpdated && project.ChangeVersion == beforeVersion,
                    "Rejected QSDB cardinality save mutated project persistence state.");
                Require(!File.Exists(path) && !File.Exists(path + ".bak"),
                    "Rejected QSDB cardinality save published project or backup bytes.");
            }
            finally
            {
                Delete(path);
                Delete(path + ".bak");
                Delete(path + ".tmp");
            }
        }

        private static XElement NewElement(int handleCount)
        {
            return new XElement("element",
                new XAttribute("id", "E1"),
                new XAttribute("category", ElementCategory.ArchitecturalWall),
                new XAttribute("familyId", string.Empty),
                new XAttribute("floorId", string.Empty),
                new XAttribute("zoneId", string.Empty),
                new XAttribute("drawingFingerprint", string.Empty),
                new XAttribute("dirty", "0"),
                new XAttribute("updatedUtc", Timestamp),
                new XElement("handles", Enumerable.Range(0, handleCount).Select(index => new XElement("h", "H" + index))),
                new XElement("dependencies"),
                new XElement("properties"),
                new XElement("quantities"));
        }

        private static XDocument NewDocument()
        {
            return new XDocument(new XElement("qs3d",
                new XAttribute("schema", ProjectState.CurrentSchemaVersion),
                new XAttribute("projectId", "cardinality-smoke"),
                new XAttribute("name", "Cardinality smoke"),
                new XAttribute("updatedUtc", Timestamp),
                new XAttribute("changeVersion", "0"),
                new XAttribute("drawingPath", string.Empty),
                new XAttribute("drawingFingerprint", string.Empty),
                new XAttribute("activeZoneId", string.Empty),
                new XAttribute("activeFloorId", string.Empty),
                new XElement("metadata"),
                new XElement("zones"),
                new XElement("floors"),
                new XElement("families"),
                new XElement("rules"),
                new XElement("elements"),
                new XElement("audit")));
        }

        private static void ExpectInvalid(XDocument document, string label, int maximum)
        {
            var path = TempPath();
            try
            {
                document.Save(path, SaveOptions.DisableFormatting);
                var rejected = false;
                try
                {
                    new QsdbProjectStore().Load(path);
                }
                catch (InvalidDataException ex)
                {
                    rejected = ex.Message.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0 &&
                               ex.Message.IndexOf(maximum.ToString(), StringComparison.Ordinal) >= 0;
                }

                Require(rejected, "QSDB accepted oversized structural cardinality for " + label + ".");
            }
            finally
            {
                Delete(path);
            }
        }

        private static string TempPath() =>
            Path.Combine(Path.GetTempPath(), "qs3d-cardinality-" + Guid.NewGuid().ToString("N") + ".qsdb");

        private static void Delete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort smoke cleanup only.
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
