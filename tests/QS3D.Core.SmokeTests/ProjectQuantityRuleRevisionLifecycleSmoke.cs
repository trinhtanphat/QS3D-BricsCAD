using System;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityRuleRevisionLifecycleSmoke
    {
        public static void Run()
        {
            StructuralMutationsAdvanceExactlyOnce();
            NoOpMutationsDoNotAdvance();
            RejectedMutationsDoNotAdvance();
            RevisionOverflowFailsBeforeMutation();
        }

        private static void StructuralMutationsAdvanceExactlyOnce()
        {
            var project = Project();
            var first = Rule("R1", "Q1");
            var second = Rule("R2", "Q2");
            var third = Rule("R3", "Q3");

            AssertAdvance(project, () => project.QuantityRules.Add(first), "Add");
            AssertAdvance(project, () => project.QuantityRules.Insert(0, second), "Insert");
            AssertAdvance(project, () => project.QuantityRules[1] = third, "index replacement");
            if (!ReferenceEquals(project.QuantityRules[1], third)) throw new Exception("Expected replacement rule to be installed.");

            AssertAdvance(project, () =>
            {
                if (!project.QuantityRules.Remove(second)) throw new Exception("Expected Remove to succeed.");
            }, "Remove");

            project.QuantityRules.Add(first);
            AssertAdvance(project, () => project.QuantityRules.RemoveAt(0), "RemoveAt");
            if (project.QuantityRules.Count == 0) project.QuantityRules.Add(first);
            AssertAdvance(project, project.QuantityRules.Clear, "Clear");
            Equal(0, project.QuantityRules.Count);
        }

        private static void NoOpMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Rule("R1", "Q1");
            project.QuantityRules.Add(first);

            var version = project.ChangeVersion;
            project.QuantityRules[0] = first;
            Equal(version, project.ChangeVersion, "same-reference replacement");

            if (project.QuantityRules.Remove(Rule("missing", "missing"))) throw new Exception("Unexpected removal of missing rule.");
            Equal(version, project.ChangeVersion, "missing Remove");

            project.QuantityRules.Clear();
            var emptyVersion = project.ChangeVersion;
            project.QuantityRules.Clear();
            Equal(emptyVersion, project.ChangeVersion, "empty Clear");
        }

        private static void RejectedMutationsDoNotAdvance()
        {
            var project = Project();
            var first = Rule("R1", "Q1");
            project.QuantityRules.Add(first);
            var version = project.ChangeVersion;
            var count = project.QuantityRules.Count;

            Throws<ArgumentNullException>(() => project.QuantityRules.Add(null!));
            AssertUnchanged(project, version, count, first, "null Add");

            Throws<ArgumentNullException>(() => project.QuantityRules.Insert(0, null!));
            AssertUnchanged(project, version, count, first, "null Insert");

            Throws<ArgumentOutOfRangeException>(() => project.QuantityRules.Insert(2, Rule("out-of-range-insert", "Q2")));
            AssertUnchanged(project, version, count, first, "out-of-range Insert");

            Throws<ArgumentOutOfRangeException>(() => project.QuantityRules.RemoveAt(1));
            AssertUnchanged(project, version, count, first, "out-of-range RemoveAt");

            Throws<ArgumentOutOfRangeException>(() => project.QuantityRules[1] = Rule("out-of-range-set", "Q3"));
            AssertUnchanged(project, version, count, first, "out-of-range index replacement");
        }

        private static void RevisionOverflowFailsBeforeMutation()
        {
            var project = Project();
            SetChangeVersion(project, long.MaxValue);
            var beforeCount = project.QuantityRules.Count;
            try
            {
                project.QuantityRules.Add(Rule("overflow", "Q"));
            }
            catch (OverflowException)
            {
                Equal(beforeCount, project.QuantityRules.Count, "overflow count");
                Equal(long.MaxValue, project.ChangeVersion, "overflow revision");
                return;
            }

            throw new Exception("Expected quantity-rule structural mutation to fail before mutation when ChangeVersion overflows.");
        }

        private static void AssertAdvance(ProjectState project, Action action, string operation)
        {
            var before = project.ChangeVersion;
            action();
            Equal(checked(before + 1L), project.ChangeVersion, operation);
        }

        private static void AssertUnchanged(ProjectState project, long version, int count, QuantityRule first, string operation)
        {
            Equal(version, project.ChangeVersion, operation + " revision");
            Equal(count, project.QuantityRules.Count, operation + " count");
            if (!ReferenceEquals(first, project.QuantityRules[0])) throw new Exception(operation + ": original rule changed.");
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

        private static ProjectState Project() => new ProjectState("quantity-rule-revision", "Quantity rule revision");

        private static QuantityRule Rule(string id, string output) =>
            new QuantityRule(id, ElementCategory.Beam, output, "1", "v1");

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
