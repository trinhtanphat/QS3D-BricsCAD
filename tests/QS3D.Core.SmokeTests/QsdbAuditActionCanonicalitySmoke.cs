using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbAuditActionCanonicalitySmoke
    {
        internal static void Run()
        {
            RejectsMalformedInMemoryActionsBeforePublication();
            RejectsMalformedPersistedActions();
            CanonicalActionRoundTrips();
        }

        private static void RejectsMalformedInMemoryActionsBeforePublication()
        {
            AssertSaveRejected("", "blank");
            AssertSaveRejected("  ", "whitespace");
            AssertSaveRejected(" project.save ", "padded");
        }

        private static void AssertSaveRejected(string action, string label)
        {
            var path = TempPath(label);
            try
            {
                var project = NewProject();
                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = DateTime.UtcNow,
                    Action = action
                });
                var changeVersion = project.ChangeVersion;
                var updatedUtc = project.UpdatedUtc;

                Throws<InvalidDataException>(() => new QsdbProjectStore().Save(project, path), label + " save");
                Require(!File.Exists(path), label + " audit action published a project file");
                Equal(changeVersion, project.ChangeVersion, label + " audit action changed ChangeVersion");
                Equal(updatedUtc, project.UpdatedUtc, label + " audit action changed UpdatedUtc");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void RejectsMalformedPersistedActions()
        {
            var path = TempPath("load");
            try
            {
                var project = NewProject();
                AuditTrail.ForProject(project).Record("project.save", "", "seed");
                var store = new QsdbProjectStore();
                store.Save(project, path);

                RewriteAction(path, " project.save ", remove: false);
                Throws<InvalidDataException>(() => store.Load(path), "padded persisted action");

                RewriteAction(path, "", remove: false);
                Throws<InvalidDataException>(() => store.Load(path), "blank persisted action");

                RewriteAction(path, "", remove: true);
                Throws<InvalidDataException>(() => store.Load(path), "missing persisted action");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void CanonicalActionRoundTrips()
        {
            var path = TempPath("roundtrip");
            try
            {
                var project = NewProject();
                AuditTrail.ForProject(project).Record("project.save", "E1", "canonical");
                var store = new QsdbProjectStore();
                store.Save(project, path);

                var loaded = store.Load(path);
                Equal(1, loaded.AuditEvents.Count, "canonical audit count");
                Equal("project.save", loaded.AuditEvents[0].Action, "canonical audit action");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void RewriteAction(string path, string value, bool remove)
        {
            var document = XDocument.Load(path);
            var item = document.Root?.Element("audit")?.Element("event")
                ?? throw new Exception("QsdbAuditActionCanonicalitySmoke: missing audit event fixture.");
            if (remove)
                item.Attribute("action")?.Remove();
            else
                item.SetAttributeValue("action", value);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static ProjectState NewProject() =>
            new ProjectState("P-QSDB-AUDIT-ACTION", "QSDB audit action smoke");

        private static string TempPath(string label) =>
            Path.Combine(Path.GetTempPath(), "qs3d-audit-action-" + label + "-" + Guid.NewGuid().ToString("N") + ".qsdb");

        private static void Cleanup(string path)
        {
            Delete(path);
            Delete(path + ".bak");
            Delete(path + ".lock");
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
            var prefix = Path.GetFileName(path) + ".";
            foreach (var file in Directory.GetFiles(directory, prefix + "*.tmp")) Delete(file);
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("QsdbAuditActionCanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("QsdbAuditActionCanonicalitySmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("QsdbAuditActionCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QsdbAuditActionCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbAuditActionCanonicalitySmoke.Run();
    }
}
