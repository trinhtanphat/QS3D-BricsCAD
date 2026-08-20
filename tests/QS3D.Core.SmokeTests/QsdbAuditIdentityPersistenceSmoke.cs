using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbAuditIdentityPersistenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonCanonicalMutableAuditIdentitiesWithoutPublication();
            AcceptsCanonicalAuditIdentityAndFreeFormDetail();
        }

        private static void RejectsNonCanonicalMutableAuditIdentitiesWithoutPublication()
        {
            AssertRejected(" E1 ", "corr-1", "padded element id");
            AssertRejected("E1\tchild", "corr-1", "control element id");
            AssertRejected("E1", " corr-1 ", "padded correlation id");
            AssertRejected("E1", "corr-1\nchild", "control correlation id");
        }

        private static void AssertRejected(string elementId, string correlationId, string label)
        {
            var project = CreateProject(elementId, correlationId, "detail");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var path = TemporaryPath(label);

            try
            {
                Throws<InvalidDataException>(() => new QsdbProjectStore().SaveNew(project, path));
                Equal(beforeVersion, project.ChangeVersion, label + " change version");
                Equal(beforeUpdatedUtc, project.UpdatedUtc, label + " updated UTC");
                if (File.Exists(path) || File.Exists(path + ".bak"))
                    throw new Exception("QsdbAuditIdentityPersistenceSmoke " + label + " published rejected audit state.");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void AcceptsCanonicalAuditIdentityAndFreeFormDetail()
        {
            var project = CreateProject("E1", "corr-1", "line one\nline two\tcolumn");
            var path = TemporaryPath("canonical");

            try
            {
                new QsdbProjectStore().SaveNew(project, path);
                var loaded = new QsdbProjectStore().Load(path);
                Equal(1, loaded.AuditEvents.Count, "canonical event count");
                Equal("E1", loaded.AuditEvents[0].ElementId, "canonical element id");
                Equal("corr-1", loaded.AuditEvents[0].CorrelationId, "canonical correlation id");
                Equal("line one\nline two\tcolumn", loaded.AuditEvents[0].Detail, "free-form detail");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static ProjectState CreateProject(string elementId, string correlationId, string detail)
        {
            var project = new ProjectState("AUDIT-PERSISTENCE", "Audit persistence identity");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                Action = "audit.persistence",
                ElementId = elementId,
                Detail = detail,
                Actor = "agent",
                CorrelationId = correlationId
            });
            return project;
        }

        private static string TemporaryPath(string label)
        {
            var safe = label.Replace(' ', '-');
            return Path.Combine(Path.GetTempPath(), "qs3d-audit-identity-" + safe + "-" + Guid.NewGuid().ToString("N") + ".qsdb");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("QsdbAuditIdentityPersistenceSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("QsdbAuditIdentityPersistenceSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
