using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditHistoryBudgetMutationSmoke
    {
        private const int MaxStoredEvents = 10_000;
        private const int MaxStoredTextCharacters = 8 * 1024 * 1024;
        private static readonly DateTime Utc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            DirectAddAndInsertRejectAtCapacityWithoutMutation();
            DirectTextOverflowRejectsWithoutMutation();
            ReplacementUsesOldNewTextDelta();
            OwnedPropertyGrowthIsAtomicAtTextBudget();
            DuplicateReferencePropertyDeltaUsesMultiplicity();
            RemovalAndClearReleaseBudget();
            CorruptBackingCountRejectsMutationWithoutRepair();
        }

        private static void DirectAddAndInsertRejectAtCapacityWithoutMutation()
        {
            var project = Project("COUNT");
            var repeated = Event("a");
            for (var index = 0; index < MaxStoredEvents; index++) project.AuditEvents.Add(repeated);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var candidate = Event("overflow");

            Throws<InvalidOperationException>(() => project.AuditEvents.Add(candidate));
            AssertProject(project, MaxStoredEvents, beforeVersion, beforeUpdatedUtc, "count after rejected direct add");

            Throws<InvalidOperationException>(() => project.AuditEvents.Insert(0, candidate));
            AssertProject(project, MaxStoredEvents, beforeVersion, beforeUpdatedUtc, "count after rejected direct insert");

            project.AuditEvents[0] = candidate;
            Equal(MaxStoredEvents, project.AuditEvents.Count, "replacement at count capacity");
            Equal(beforeVersion + 1L, project.ChangeVersion, "replacement revision at count capacity");
        }

        private static void DirectTextOverflowRejectsWithoutMutation()
        {
            var project = Project("DIRECT-TEXT");
            project.AuditEvents.Add(Event("a", new string('x', MaxStoredTextCharacters - 2)));
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var before = project.AuditEvents[0];

            Throws<InvalidOperationException>(() => project.AuditEvents.Add(Event("aa")));

            Equal(1, project.AuditEvents.Count, "direct text overflow count");
            Same(before, project.AuditEvents[0], "direct text overflow existing reference");
            Equal(beforeVersion, project.ChangeVersion, "direct text overflow revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "direct text overflow timestamp");
        }

        private static void ReplacementUsesOldNewTextDelta()
        {
            var project = Project("REPLACE");
            var large = Event("a", new string('x', MaxStoredTextCharacters - 2));
            var small = Event("a");
            project.AuditEvents.Add(large);
            project.AuditEvents.Add(small);
            var beforeVersion = project.ChangeVersion;

            var overflowingReplacement = Event("aa");
            Throws<InvalidOperationException>(() => project.AuditEvents[1] = overflowingReplacement);
            Same(small, project.AuditEvents[1], "overflow replacement reference");
            Equal(beforeVersion, project.ChangeVersion, "overflow replacement revision");

            var shrink = Event("a");
            project.AuditEvents[0] = shrink;
            project.AuditEvents.Add(Event("aa"));
            Equal(3, project.AuditEvents.Count, "replacement shrink releases text budget");
        }

        private static void OwnedPropertyGrowthIsAtomicAtTextBudget()
        {
            var project = Project("PROPERTY");
            var item = Event("a", new string('x', MaxStoredTextCharacters - 2));
            project.AuditEvents.Add(item);

            var beforeExact = project.ChangeVersion;
            item.Action = "aa";
            Equal(beforeExact + 1L, project.ChangeVersion, "property growth to exact text budget revision");

            var beforeRejected = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            Throws<InvalidOperationException>(() => item.Action = "aaa");
            Equal("aa", item.Action, "rejected property growth value");
            Equal(beforeRejected, project.ChangeVersion, "rejected property growth revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "rejected property growth timestamp");

            item.Action = "a";
            Equal(beforeRejected + 1L, project.ChangeVersion, "property shrink revision");
        }

        private static void DuplicateReferencePropertyDeltaUsesMultiplicity()
        {
            var project = Project("MULTIPLICITY");
            var repeated = Event("a");
            project.AuditEvents.Add(repeated);
            project.AuditEvents.Add(repeated);
            project.AuditEvents.Add(Event("b", new string('x', MaxStoredTextCharacters - 4)));
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => repeated.Action = "aa");

            Equal("a", repeated.Action, "duplicate multiplicity value");
            Equal(beforeVersion, project.ChangeVersion, "duplicate multiplicity revision");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "duplicate multiplicity timestamp");
        }

        private static void RemovalAndClearReleaseBudget()
        {
            var project = Project("RELEASE");
            var full = Event("a", new string('x', MaxStoredTextCharacters - 1));
            project.AuditEvents.Add(full);
            project.AuditEvents.RemoveAt(0);
            project.AuditEvents.Add(full);
            Equal(1, project.AuditEvents.Count, "remove releases text budget");

            project.AuditEvents.Clear();
            project.AuditEvents.Add(full);
            Equal(1, project.AuditEvents.Count, "clear releases text budget");
        }

        private static void CorruptBackingCountRejectsMutationWithoutRepair()
        {
            var project = Project("CORRUPT-COUNT");
            var admitted = Event("a");
            var injected = Event("b");
            project.AuditEvents.Add(admitted);
            InjectWithoutAccounting(project, injected);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => project.AuditEvents.RemoveAt(1));
            AssertCorruptState(project, admitted, injected, beforeVersion, beforeUpdatedUtc, "corrupt removal");

            Throws<InvalidOperationException>(() => project.AuditEvents.Add(Event("c")));
            AssertCorruptState(project, admitted, injected, beforeVersion, beforeUpdatedUtc, "corrupt add");

            Throws<InvalidOperationException>(() => project.AuditEvents.Clear());
            AssertCorruptState(project, admitted, injected, beforeVersion, beforeUpdatedUtc, "corrupt clear");

            Throws<InvalidOperationException>(() => admitted.Action = "aa");
            Equal("a", admitted.Action, "corrupt owned mutation action");
            AssertCorruptState(project, admitted, injected, beforeVersion, beforeUpdatedUtc, "corrupt owned mutation");
        }

        private static void AssertCorruptState(ProjectState project, AuditEvent admitted, AuditEvent injected, long version, DateTime updatedUtc, string label)
        {
            Equal(2, project.AuditEvents.Count, label + " count");
            Same(admitted, project.AuditEvents[0], label + " admitted reference");
            Same(injected, project.AuditEvents[1], label + " injected reference");
            Equal(version, project.ChangeVersion, label + " revision");
            Equal(updatedUtc, project.UpdatedUtc, label + " timestamp");
        }

        private static void InjectWithoutAccounting(ProjectState project, AuditEvent item)
        {
            var itemsField = project.AuditEvents.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("AuditHistoryBudgetMutationSmoke could not resolve corruption-injection storage.");
            var items = itemsField.GetValue(project.AuditEvents) as List<AuditEvent>
                ?? throw new Exception("AuditHistoryBudgetMutationSmoke corruption-injection storage has an unexpected shape.");
            items.Add(item);
        }

        private static ProjectState Project(string suffix) => new ProjectState("AUDIT-BUDGET-" + suffix, "Audit budget " + suffix);

        private static AuditEvent Event(string action, string detail = "") => new AuditEvent
        {
            Utc = Utc,
            Action = action,
            Detail = detail
        };

        private static void AssertProject(ProjectState project, int count, long version, DateTime updatedUtc, string label)
        {
            Equal(count, project.AuditEvents.Count, label + " count");
            Equal(version, project.ChangeVersion, label + " revision");
            Equal(updatedUtc, project.UpdatedUtc, label + " timestamp");
        }

        private static void Same(object expected, object actual, string label)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("AuditHistoryBudgetMutationSmoke " + label + ": references differ.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditHistoryBudgetMutationSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditHistoryBudgetMutationSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
