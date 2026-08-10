using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbFreeTextRoundtripSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-free-text-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "roundtrip.qsdb");
            try
            {
                var project = new ProjectState("P-free-text", "Free text roundtrip");
                project.Metadata["Notes"] = "  project note  ";

                var family = new ProjectFamily("F1", "Family", ElementCategory.Beam);
                family.Properties["Description"] = "  family description  ";
                project.Families.Add(family);

                var element = new ProjectElement("E1", ElementCategory.Beam, "F1", string.Empty, string.Empty);
                element.Properties["Comment"] = "  element comment  ";
                project.Elements.Add(element);

                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
                    Action = "  action  ",
                    ElementId = "  E1  ",
                    Detail = "  detail with intentional padding  ",
                    Actor = "  actor  ",
                    CorrelationId = "  correlation  "
                });

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);

                Equal("  project note  ", loaded.Metadata["Notes"], "project metadata");
                Equal("  family description  ", loaded.FindFamily("F1")!.Properties["Description"], "family property");
                Equal("  element comment  ", loaded.FindElement("E1")!.Properties["Comment"], "element property");
                Equal("  action  ", loaded.AuditEvents[0].Action, "audit action");
                Equal("  E1  ", loaded.AuditEvents[0].ElementId, "audit element id payload");
                Equal("  detail with intentional padding  ", loaded.AuditEvents[0].Detail, "audit detail");
                Equal("  actor  ", loaded.AuditEvents[0].Actor, "audit actor");
                Equal("  correlation  ", loaded.AuditEvents[0].CorrelationId, "audit correlation id");
            }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("QsdbFreeTextRoundtripSmoke: " + label + " was normalized. Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
