using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbTimestampCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsEquivalentOffsetAtRoot();
            RejectsEquivalentOffsetOnElement();
            RejectsEquivalentOffsetOnAuditEvent();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture(path =>
            {
                var loaded = new QsdbProjectStore().Load(path);
                if (loaded.UpdatedUtc.Kind != DateTimeKind.Utc || loaded.Elements[0].UpdatedUtc.Kind != DateTimeKind.Utc || loaded.AuditEvents[0].Utc.Kind != DateTimeKind.Utc)
                    throw new InvalidOperationException("Canonical QSDB timestamps must continue to round-trip as UTC.");
            });
        }

        private static void RejectsEquivalentOffsetAtRoot()
        {
            WithFixture(path =>
            {
                MutateTimestamp(path, document => document.Root?.Attribute("updatedUtc"));
                ExpectInvalidData(path, "root updatedUtc");
            });
        }

        private static void RejectsEquivalentOffsetOnElement()
        {
            WithFixture(path =>
            {
                MutateTimestamp(path, document => document.Root?.Element("elements")?.Element("element")?.Attribute("updatedUtc"));
                ExpectInvalidData(path, "element updatedUtc");
            });
        }

        private static void RejectsEquivalentOffsetOnAuditEvent()
        {
            WithFixture(path =>
            {
                MutateTimestamp(path, document => document.Root?.Element("audit")?.Element("event")?.Attribute("utc"));
                ExpectInvalidData(path, "audit utc");
            });
        }

        private static void WithFixture(Action<string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-timestamp-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("TIMESTAMP-CANON", "Timestamp canonicality");
                project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = DateTime.UtcNow,
                    Action = "TimestampSmoke"
                });
                new QsdbProjectStore().SaveNew(project, path);
                assertion(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void MutateTimestamp(string path, Func<XDocument, XAttribute?> selector)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var attribute = selector(document) ?? throw new InvalidOperationException("Timestamp smoke fixture is missing the target attribute.");
            if (!DateTimeOffset.TryParse(attribute.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new InvalidOperationException("Timestamp smoke fixture did not serialize a parseable UTC token.");
            attribute.Value = parsed.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "+00:00";
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void ExpectInvalidData(string path, string surface)
        {
            try
            {
                new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("QSDB load must reject a non-canonical equivalent-offset timestamp at " + surface + ".");
            }
            catch (InvalidDataException)
            {
            }
        }
    }
}
