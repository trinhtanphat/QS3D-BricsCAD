using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditHistoryReferenceAccountingAtomicitySmoke
    {
        private static readonly DateTime Utc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            CorruptReferenceAccountingRemoveRejectsBeforeMutation();
            CorruptReferenceAccountingReplaceRejectsBeforeMutation();
            CorruptReferenceAccountingClearRejectsBeforeMutation();
        }

        private static void CorruptReferenceAccountingRemoveRejectsBeforeMutation()
        {
            var project = Project("REMOVE");
            var item = Event("remove");
            project.AuditEvents.Add(item);
            RemoveReferenceAccounting(project, item);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => project.AuditEvents.RemoveAt(0));

            Equal(1, project.AuditEvents.Count, "remove count");
            Same(item, project.AuditEvents[0], "remove item");
            Equal(beforeVersion, project.ChangeVersion, "remove revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "remove timestamp");
        }

        private static void CorruptReferenceAccountingReplaceRejectsBeforeMutation()
        {
            var project = Project("REPLACE");
            var previous = Event("previous");
            var replacement = Event("replacement");
            project.AuditEvents.Add(previous);
            RemoveReferenceAccounting(project, previous);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => project.AuditEvents[0] = replacement);

            Equal(1, project.AuditEvents.Count, "replace count");
            Same(previous, project.AuditEvents[0], "replace item");
            Equal(beforeVersion, project.ChangeVersion, "replace revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "replace timestamp");
        }

        private static void CorruptReferenceAccountingClearRejectsBeforeMutation()
        {
            var project = Project("CLEAR");
            var item = Event("clear");
            project.AuditEvents.Add(item);
            RemoveReferenceAccounting(project, item);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => project.AuditEvents.Clear());

            Equal(1, project.AuditEvents.Count, "clear count");
            Same(item, project.AuditEvents[0], "clear item");
            Equal(beforeVersion, project.ChangeVersion, "clear revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "clear timestamp");
        }

        private static void RemoveReferenceAccounting(ProjectState project, AuditEvent item)
        {
            var observerField = typeof(ProjectState).GetField("_auditHistoryBudget", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke could not resolve the audit budget observer.");
            var observer = observerField.GetValue(project)
                ?? throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke audit budget observer is null.");
            var referencesField = observer.GetType().GetField("_referenceCounts", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke could not resolve reference accounting.");
            var references = referencesField.GetValue(observer) as IDictionary
                ?? throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke reference accounting has an unexpected shape.");
            if (!references.Contains(item))
                throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke expected admitted reference accounting.");
            references.Remove(item);
        }

        private static ProjectState Project(string suffix) => new ProjectState("AUDIT-REF-ATOMIC-" + suffix, "Audit reference atomic " + suffix);

        private static AuditEvent Event(string action) => new AuditEvent
        {
            Utc = Utc,
            Action = action,
            Detail = string.Empty
        };

        private static void Same(object expected, object actual, string label)
        {
            if (!ReferenceEquals(expected, actual))
                throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke " + label + ": references differ.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditHistoryReferenceAccountingAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
