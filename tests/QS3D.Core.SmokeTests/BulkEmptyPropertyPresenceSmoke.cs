using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEmptyPropertyPresenceSmoke
    {
        private const string Key = "Comment";

        public static void Run()
        {
            AbsentToEmptyCreatesExplicitProperty();
            ExistingEmptyToEmptyRemainsNoOp();
            ExistingNonEmptyToEmptyRemainsMutation();
        }

        private static void AbsentToEmptyCreatesExplicitProperty()
        {
            var fixture = NewFixture("absent-empty");
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(fixture.Project, new[] { fixture.Element }, Key, string.Empty);

            if (changed.Count != 1 || !string.Equals(changed[0], fixture.Element.Id, StringComparison.Ordinal))
                throw new Exception("Absent-to-empty bulk property mutation did not report the changed element.");
            if (!fixture.Element.Properties.TryGetValue(Key, out var value) || value != string.Empty)
                throw new Exception("Absent-to-empty bulk property mutation did not preserve explicit empty property presence.");
            RequireFlags(fixture.Element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity,
                "Absent-to-empty bulk property mutation did not mark property/quantity freshness.");
            if (fixture.Project.ChangeVersion != beforeVersion + 1L)
                throw new Exception("Absent-to-empty bulk property mutation should touch project freshness exactly once.");
        }

        private static void ExistingEmptyToEmptyRemainsNoOp()
        {
            var fixture = NewFixture("empty-noop");
            fixture.Element.Properties[Key] = string.Empty;
            fixture.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUpdatedUtc = fixture.Element.UpdatedUtc;

            var changed = new BulkEditService().SetProperty(fixture.Project, new[] { fixture.Element }, Key, string.Empty);

            if (changed.Count != 0) throw new Exception("Existing empty property was reported as changed when assigned empty again.");
            if (fixture.Project.ChangeVersion != beforeVersion) throw new Exception("Existing empty property no-op touched project freshness.");
            if (fixture.Element.Dirty != ElementDirtyFlags.None || fixture.Element.UpdatedUtc != beforeUpdatedUtc)
                throw new Exception("Existing empty property no-op dirtied element freshness.");
            if (!fixture.Element.Properties.ContainsKey(Key))
                throw new Exception("Existing empty property no-op removed explicit property presence.");
        }

        private static void ExistingNonEmptyToEmptyRemainsMutation()
        {
            var fixture = NewFixture("nonempty-empty");
            fixture.Element.Properties[Key] = "before";
            fixture.Element.MarkClean(ElementDirtyFlags.All);

            var changed = new BulkEditService().SetProperty(fixture.Project, new[] { fixture.Element }, Key, string.Empty);

            if (changed.Count != 1) throw new Exception("Non-empty-to-empty bulk property mutation was not reported.");
            if (!fixture.Element.Properties.TryGetValue(Key, out var value) || value != string.Empty)
                throw new Exception("Non-empty-to-empty bulk property mutation did not store the explicit empty value.");
            RequireFlags(fixture.Element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity,
                "Non-empty-to-empty bulk property mutation lost property/quantity dirty behavior.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("P-BULK-EMPTY-" + suffix, "Bulk empty property presence");
            var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            return new Fixture(project, element);
        }

        private static void RequireFlags(ElementDirtyFlags actual, ElementDirtyFlags required, string message)
        {
            if ((actual & required) != required)
                throw new Exception(message + " Required=" + required + ", actual=" + actual + ".");
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
