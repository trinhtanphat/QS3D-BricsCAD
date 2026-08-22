using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditReadIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNullActionOnRead();
            RejectsNonUtcTimestampOnRead();
            PreservesCanonicalCloneIsolationAndReadonlyCollection();
        }

        private static void RejectsNullActionOnRead()
        {
            var project = new ProjectState("AUDIT-READ-NULL", "Audit read null action");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = null!
            });
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => _ = AuditTrail.ForProject(project).Events);
            Equal(beforeVersion, project.ChangeVersion, "null-action read version");
            Equal(1, project.AuditEvents.Count, "null-action read count");
            if (project.AuditEvents[0].Action != null)
                throw new InvalidOperationException("Audit read validation mutated the malformed backing action.");
        }

        private static void RejectsNonUtcTimestampOnRead()
        {
            var project = new ProjectState("AUDIT-READ-UTC", "Audit read UTC");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Unspecified),
                Action = "existing.action"
            });
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => _ = AuditTrail.ForProject(project).Events);
            Equal(beforeVersion, project.ChangeVersion, "non-UTC read version");
            Equal(1, project.AuditEvents.Count, "non-UTC read count");
        }

        private static void PreservesCanonicalCloneIsolationAndReadonlyCollection()
        {
            var project = new ProjectState("AUDIT-READ-VALID", "Audit read valid");
            var stored = new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                ElementId = "E1",
                Detail = null!,
                Actor = "agent",
                CorrelationId = "corr"
            };
            project.AuditEvents.Add(stored);

            var snapshot = AuditTrail.ForProject(project).Events;
            Equal(1, snapshot.Count, "valid read count");
            Equal("existing.action", snapshot[0].Action, "valid read action");
            Equal(string.Empty, snapshot[0].Detail, "optional detail normalization");
            if (ReferenceEquals(stored, snapshot[0]))
                throw new InvalidOperationException("Audit read returned the mutable backing event instead of a clone.");

            snapshot[0].Action = "snapshot-only";
            Equal("existing.action", stored.Action, "clone isolation");

            if (snapshot is not IList<AuditEvent> list || !list.IsReadOnly)
                throw new InvalidOperationException("Audit read outer collection must remain read-only.");
            Throws<NotSupportedException>(() => list.Add(new AuditEvent()));
            Equal(1, snapshot.Count, "readonly read count");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("AuditReadIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("AuditReadIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
