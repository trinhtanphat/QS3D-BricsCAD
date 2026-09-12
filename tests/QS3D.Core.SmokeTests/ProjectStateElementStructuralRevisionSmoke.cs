using System;
using System.Reflection;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateElementStructuralRevisionSmoke
    {
        public static void Run()
        {
            StructuralMutationsAdvanceExactlyOnce();
            NoOpMutationsDoNotAdvance();
            RejectedMutationsDoNotAdvance();
            RevisionOverflowFailsBeforeMutation();
            ProjectQuantityRuleRevisionLifecycleSmoke.Run();
            AuditEventStructuralMutationsAdvanceExactlyOnce();
            AuditEventNoOpMutationsDoNotAdvance();
            AuditEventRejectedMutationsDoNotAdvance();
            AuditEventPropertyMutationsAdvanceExactlyOnce();
            AuditEventPropertyNoOpsDoNotAdvance();
            AuditEventOwnershipTracksOnlyCurrentMembers();
            AuditTrailMutationsAdvanceExactlyOnce();
            AuditEventRevisionOverflowFailsBeforeMutation();
            AuditEventPropertyRevisionOverflowFailsBeforeMutation();
        }

        private static void StructuralMutationsAdvanceExactlyOnce()
        {
            var project = Project();
            var first = Element("E1");
            var second = Element("E2");
            var third = Element("E3");

            AssertAdvance(project, () => project.Elements.Add(first), "Add");
            AssertAdvance(project, () => project.Elements.Insert(0, second), "Insert");
            AssertAdvance(project, () => project.Elements[1] = third, "index replacement");
            if (!ReferenceEquals(project.Elements[1], third)) throw new Exception("Expected replacement element to be installed.");

            AssertAdvance(project, () =>
            {
                if (!project.Elements.Remove(second)) throw new Exception("Expected Remove to succeed.");
            }, "Remove");

            project.Elements.Add(first);
            AssertAdvance(project, () => project.Elements.RemoveAt(0), "RemoveAt");
            if (project.Elements.Count == 0) project.Elements.Add(first);
            AssertAdvance(project, project.Elements.Clear, "Clear");
            Equal(0, project.Elements.Count);
        }

        private static void NoOpMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Element("E1");
            project.Elements.Add(first);

            var version = project.ChangeVersion;
            project.Elements[0] = first;
            Equal(version, project.ChangeVersion);

            if (project.Elements.Remove(Element("missing"))) throw new Exception("Unexpected removal of missing element.");
            Equal(version, project.ChangeVersion);

            project.Elements.Clear();
            var emptyVersion = project.ChangeVersion;
            project.Elements.Clear();
            Equal(emptyVersion, project.ChangeVersion);
        }

        private static void RejectedMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Element("E1");
            project.Elements.Add(first);
            var version = project.ChangeVersion;
            var count = project.Elements.Count;

            Throws<ArgumentNullException>(() => project.Elements.Add(null!));
            AssertUnchanged(project, version, count, first, "null Add");

            Throws<ArgumentNullException>(() => project.Elements.Insert(0, null!));
            AssertUnchanged(project, version, count, first, "null Insert");

            Throws<ArgumentOutOfRangeException>(() => project.Elements.Insert(2, Element("out-of-range-insert")));
            AssertUnchanged(project, version, count, first, "out-of-range Insert");

            Throws<ArgumentOutOfRangeException>(() => project.Elements.RemoveAt(1));
            AssertUnchanged(project, version, count, first, "out-of-range RemoveAt");

            Throws<ArgumentOutOfRangeException>(() => project.Elements[1] = Element("out-of-range-set"));
            AssertUnchanged(project, version, count, first, "out-of-range index replacement");
        }

        private static void RevisionOverflowFailsBeforeMutation()
        {
            var project = Project();
            SetChangeVersion(project, long.MaxValue);
            var beforeCount = project.Elements.Count;
            try
            {
                project.Elements.Add(Element("overflow"));
            }
            catch (OverflowException)
            {
                Equal(beforeCount, project.Elements.Count);
                Equal(long.MaxValue, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected element structural mutation to fail before mutation when ChangeVersion overflows.");
        }

        private static void AuditEventStructuralMutationsAdvanceExactlyOnce()
        {
            var project = Project();
            var first = Audit("one");
            var second = Audit("two");
            var third = Audit("three");

            AssertAdvance(project, () => project.AuditEvents.Add(first), "audit Add");
            AssertAdvance(project, () => project.AuditEvents.Insert(0, second), "audit Insert");
            AssertAdvance(project, () => project.AuditEvents[1] = third, "audit index replacement");
            if (!ReferenceEquals(project.AuditEvents[1], third)) throw new Exception("Expected replacement audit event to be installed.");
            AssertAdvance(project, () =>
            {
                if (!project.AuditEvents.Remove(second)) throw new Exception("Expected audit Remove to succeed.");
            }, "audit Remove");
            project.AuditEvents.Add(first);
            AssertAdvance(project, () => project.AuditEvents.RemoveAt(0), "audit RemoveAt");
            if (project.AuditEvents.Count == 0) project.AuditEvents.Add(first);
            AssertAdvance(project, project.AuditEvents.Clear, "audit Clear");
        }

        private static void AuditEventNoOpMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Audit("one");
            project.AuditEvents.Add(first);
            var version = project.ChangeVersion;

            project.AuditEvents[0] = first;
            Equal(version, project.ChangeVersion, "audit same-reference replacement");
            if (project.AuditEvents.Remove(Audit("missing"))) throw new Exception("Unexpected removal of missing audit event.");
            Equal(version, project.ChangeVersion, "audit remove-missing");
            project.AuditEvents.Clear();
            var emptyVersion = project.ChangeVersion;
            project.AuditEvents.Clear();
            Equal(emptyVersion, project.ChangeVersion, "audit clear-empty");
        }

        private static void AuditEventRejectedMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Audit("one");
            project.AuditEvents.Add(first);
            var version = project.ChangeVersion;
            var count = project.AuditEvents.Count;

            Throws<ArgumentNullException>(() => project.AuditEvents.Add(null!));
            AssertAuditUnchanged(project, version, count, first, "audit null Add");
            Throws<ArgumentNullException>(() => project.AuditEvents.Insert(0, null!));
            AssertAuditUnchanged(project, version, count, first, "audit null Insert");
            Throws<ArgumentOutOfRangeException>(() => project.AuditEvents.Insert(2, Audit("out-of-range-insert")));
            AssertAuditUnchanged(project, version, count, first, "audit out-of-range Insert");
            Throws<ArgumentOutOfRangeException>(() => project.AuditEvents.RemoveAt(1));
            AssertAuditUnchanged(project, version, count, first, "audit out-of-range RemoveAt");
            Throws<ArgumentOutOfRangeException>(() => project.AuditEvents[1] = Audit("out-of-range-set"));
            AssertAuditUnchanged(project, version, count, first, "audit out-of-range index replacement");
        }

        private static void AuditEventPropertyMutationsAdvanceExactlyOnce()
        {
            var project = Project();
            var item = Audit("one");
            project.AuditEvents.Add(item);

            AssertAdvance(project, () => item.Utc = new DateTime(2026, 9, 12, 1, 2, 3, DateTimeKind.Utc), "audit Utc mutation");
            AssertAdvance(project, () => item.Action = "two", "audit Action mutation");
            AssertAdvance(project, () => item.ElementId = "E2", "audit ElementId mutation");
            AssertAdvance(project, () => item.Detail = "detail", "audit Detail mutation");
            AssertAdvance(project, () => item.Actor = "actor", "audit Actor mutation");
            AssertAdvance(project, () => item.CorrelationId = "corr", "audit CorrelationId mutation");
        }

        private static void AuditEventPropertyNoOpsDoNotAdvance()
        {
            var project = Project();
            var item = Audit("one");
            project.AuditEvents.Add(item);
            var version = project.ChangeVersion;

            item.Utc = item.Utc;
            item.Action = item.Action;
            item.ElementId = item.ElementId;
            item.Detail = item.Detail;
            item.Actor = item.Actor;
            item.CorrelationId = item.CorrelationId;
            Equal(version, project.ChangeVersion, "audit property no-op assignments");
        }

        private static void AuditEventOwnershipTracksOnlyCurrentMembers()
        {
            var project = Project();
            var first = Audit("first");
            var second = Audit("second");
            project.AuditEvents.Add(first);

            AssertAdvance(project, () => project.AuditEvents[0] = second, "audit ownership replacement");
            var afterReplacement = project.ChangeVersion;
            first.Action = "detached";
            Equal(afterReplacement, project.ChangeVersion, "detached audit event mutation");
            AssertAdvance(project, () => second.Action = "owned", "owned replacement mutation");

            AssertAdvance(project, () => project.AuditEvents.Add(second), "duplicate audit reference add");
            AssertAdvance(project, () => second.Detail = "still-single-subscription", "duplicate audit reference mutation");
            AssertAdvance(project, () => project.AuditEvents.RemoveAt(0), "remove one duplicate audit reference");
            AssertAdvance(project, () => second.Actor = "still-owned", "remaining duplicate audit reference mutation");
            AssertAdvance(project, () => project.AuditEvents.RemoveAt(0), "remove last duplicate audit reference");
            var afterDetach = project.ChangeVersion;
            second.CorrelationId = "detached";
            Equal(afterDetach, project.ChangeVersion, "last-reference detach");
        }

        private static void AuditTrailMutationsAdvanceExactlyOnce()
        {
            var project = Project();
            var trail = AuditTrail.ForProject(project);
            AssertAdvance(project, () => trail.Record("record", string.Empty, "detail"), "AuditTrail.Record");
            AssertAdvance(project, trail.Clear, "AuditTrail.Clear");
        }

        private static void AuditEventRevisionOverflowFailsBeforeMutation()
        {
            var project = Project();
            SetChangeVersion(project, long.MaxValue);
            var beforeCount = project.AuditEvents.Count;
            Throws<OverflowException>(() => project.AuditEvents.Add(Audit("overflow")));
            Equal(beforeCount, project.AuditEvents.Count, "audit overflow count");
            Equal(long.MaxValue, project.ChangeVersion, "audit overflow revision");
        }

        private static void AuditEventPropertyRevisionOverflowFailsBeforeMutation()
        {
            var project = Project();
            var item = Audit("before");
            project.AuditEvents.Add(item);
            SetChangeVersion(project, long.MaxValue);
            var beforeAction = item.Action;
            Throws<OverflowException>(() => item.Action = "after");
            Equal(beforeAction, item.Action, "audit property overflow value");
            Equal(long.MaxValue, project.ChangeVersion, "audit property overflow revision");
        }

        private static void AssertAdvance(ProjectState project, Action action, string operation)
        {
            var before = project.ChangeVersion;
            action();
            Equal(checked(before + 1L), project.ChangeVersion, operation);
        }

        private static void AssertUnchanged(ProjectState project, long version, int count, ProjectElement first, string operation)
        {
            Equal(version, project.ChangeVersion, operation + " revision");
            Equal(count, project.Elements.Count, operation + " count");
            if (!ReferenceEquals(first, project.Elements[0])) throw new Exception(operation + ": original element changed.");
        }

        private static void AssertAuditUnchanged(ProjectState project, long version, int count, AuditEvent first, string operation)
        {
            Equal(version, project.ChangeVersion, operation + " revision");
            Equal(count, project.AuditEvents.Count, operation + " count");
            if (!ReferenceEquals(first, project.AuditEvents[0])) throw new Exception(operation + ": original audit event changed.");
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

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static ProjectState Project() => new ProjectState("element-structural-revision", "Element structural revision");

        private static ProjectElement Element(string id) => new ProjectElement(id, ElementCategory.Beam);

        private static AuditEvent Audit(string action) => new AuditEvent
        {
            Utc = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc),
            Action = action
        };

        private static void SetChangeVersion(ProjectState project, long value)
        {
            var property = typeof(ProjectState).GetProperty(nameof(ProjectState.ChangeVersion), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new Exception("Expected ProjectState.ChangeVersion.");
            property.SetValue(project, value);
        }

        private static void Equal<T>(T expected, T actual, string? label = null)
        {
            if (Equals(expected, actual)) return;
            throw new Exception((label == null ? string.Empty : label + ": ") + "expected " + expected + ", got " + actual + ".");
        }
    }
}
