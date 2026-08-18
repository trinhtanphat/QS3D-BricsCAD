using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneAssignmentKnownCountTraversalSmoke
    {
        internal static void Run()
        {
            UnderEnumerationFailsClosed();
            OverEnumerationFailsClosed();
            InBoundConflictingCountsFailBeforeEnumeration();
            HonestCountedInputStillAssigns();
            PureStreamingInputStillAssigns();
        }

        private static void UnderEnumerationFailsClosed()
        {
            var fixture = NewFixture("under");
            var source = new CountMismatchCollection(2, fixture.Element);
            AssertRejectedAtomically(
                fixture,
                () => ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source),
                "under-enumerating counted source");
        }

        private static void OverEnumerationFailsClosed()
        {
            var fixture = NewFixture("over");
            var source = new CountMismatchCollection(1, fixture.Element, fixture.Element);
            AssertRejectedAtomically(
                fixture,
                () => ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source),
                "over-enumerating counted source");
        }

        private static void InBoundConflictingCountsFailBeforeEnumeration()
        {
            var fixture = NewFixture("conflict");
            var source = new ConflictingCountCollection(fixture.Element);
            AssertRejectedAtomically(
                fixture,
                () => ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source),
                "conflicting in-bound known counts");
            Equal(0, source.EnumerationAttempts, "Conflicting in-bound Count contracts must fail before enumeration.");
        }

        private static void HonestCountedInputStillAssigns()
        {
            var fixture = NewFixture("honest");
            var changed = ProjectZoneService.Assign(
                fixture.Project,
                fixture.TargetZone.Id,
                new List<ProjectElement> { fixture.Element });

            Equal(1, changed, "Honest counted Zone assignment did not report one changed element.");
            Equal(fixture.TargetZone.Id, fixture.Element.ZoneId, "Honest counted Zone assignment did not update ZoneId.");
        }

        private static void PureStreamingInputStillAssigns()
        {
            var fixture = NewFixture("streaming");
            var changed = ProjectZoneService.Assign(
                fixture.Project,
                fixture.TargetZone.Id,
                Yield(fixture.Element));

            Equal(1, changed, "Pure streaming Zone assignment did not report one changed element.");
            Equal(fixture.TargetZone.Id, fixture.Element.ZoneId, "Pure streaming Zone assignment did not update ZoneId.");
        }

        private static IEnumerable<ProjectElement> Yield(ProjectElement element)
        {
            yield return element;
        }

        private static void AssertRejectedAtomically(Fixture fixture, Action action, string label)
        {
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;

            Throws<InvalidOperationException>(action, label);

            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, label + " changed project timestamp.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, label + " changed ZoneId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, label + " dirtied the element.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("zone-count-traversal-" + suffix, "Zone count traversal " + suffix);
            var sourceZone = new ZoneDefinition("zone-source", "Source zone");
            var targetZone = new ZoneDefinition("zone-target", "Target zone");
            project.Zones.Add(sourceZone);
            project.Zones.Add(targetZone);
            project.ActiveZoneId = sourceZone.Id;

            var element = new ProjectElement(
                "element-1",
                ElementCategory.Beam,
                string.Empty,
                string.Empty,
                sourceZone.Id);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Fixture(project, sourceZone, targetZone, element);
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectZoneAssignmentKnownCountTraversalSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class Fixture
        {
            internal Fixture(ProjectState project, ZoneDefinition sourceZone, ZoneDefinition targetZone, ProjectElement element)
            {
                Project = project;
                SourceZone = sourceZone;
                TargetZone = targetZone;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal ZoneDefinition SourceZone { get; }
            internal ZoneDefinition TargetZone { get; }
            internal ProjectElement Element { get; }
        }

        private sealed class CountMismatchCollection : ICollection<ProjectElement>
        {
            private readonly ProjectElement[] _items;

            internal CountMismatchCollection(int advertisedCount, params ProjectElement[] items)
            {
                Count = advertisedCount;
                _items = items;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<ProjectElement> GetEnumerator() => ((IEnumerable<ProjectElement>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(ProjectElement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;

            internal ConflictingCountCollection(ProjectElement element)
            {
                _element = element;
            }

            public int EnumerationAttempts { get; private set; }
            public int Count => 1;
            int IReadOnlyCollection<ProjectElement>.Count => 2;
            int ICollection.Count => 2;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Conflicting Count contracts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }
    }

    internal static class ProjectZoneAssignmentKnownCountTraversalSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectZoneAssignmentKnownCountTraversalSmoke.Run();
    }
}
