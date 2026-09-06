using System;
using System.Reflection;
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

        private static ProjectElement Element(string id) => new ProjectElement(id, ElementCategory.Wall);

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
