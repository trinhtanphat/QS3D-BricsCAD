using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneAssignmentBoundSmoke
    {
        private const int AssignmentTargetLimit = 10000;

        public static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            NonGenericCountedOversizeFailsBeforeEnumeration();
            ConflictingCountInterfacesFailBeforeEnumeration();
            CountVersionMutationFailsBeforeEnumeration();
            LazyOversizeStopsAtFirstImpossibleEntry();
            ExactBoundDuplicatesRemainSupported();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture("counted");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NoEnumerationCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source));

            Equal(0, source.EnumerationAttempts, "Counted oversize Zone assignment enumerated the source.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Counted oversize Zone assignment changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Counted oversize Zone assignment changed project timestamp.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, "Counted oversize Zone assignment changed ZoneId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Counted oversize Zone assignment dirtied the element.");
        }

        private static void NonGenericCountedOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture("non-generic-counted");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NonGenericNoEnumerationCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source));

            Equal(0, source.EnumerationAttempts, "Non-generic counted oversize Zone assignment enumerated the source.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Non-generic counted oversize Zone assignment changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Non-generic counted oversize Zone assignment changed project timestamp.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, "Non-generic counted oversize Zone assignment changed ZoneId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Non-generic counted oversize Zone assignment dirtied the element.");
        }

        private static void ConflictingCountInterfacesFailBeforeEnumeration()
        {
            var fixture = NewFixture("conflicting-counts");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new ConflictingCountInterfacesCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source));

            Equal(0, source.EnumerationAttempts, "Conflicting count contracts bypassed known-count Zone assignment rejection.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Conflicting count contracts changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Conflicting count contracts changed project timestamp.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, "Conflicting count contracts changed ZoneId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Conflicting count contracts dirtied the element.");
        }

        private static void CountVersionMutationFailsBeforeEnumeration()
        {
            var fixture = NewFixture("count-version");
            var beforeVersion = fixture.Project.ChangeVersion;
            var source = new VersionMutatingCountCollection(fixture.Project, fixture.Element);

            Throws<InvalidOperationException>(() =>
                ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source));

            Equal(1, source.CountReads, "Version-mutating counted source did not read Count exactly once.");
            Equal(0, source.EnumerationAttempts, "Version-mutating counted source was enumerated after ChangeVersion drift.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Count fixture did not produce the expected single ChangeVersion drift.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, "Version-mutating Count changed ZoneId through assignment.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Version-mutating Count caused Zone assignment dirty flags.");
        }

        private static void LazyOversizeStopsAtFirstImpossibleEntry()
        {
            var fixture = NewFixture("lazy");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new GuardedInfiniteRepeat(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, source));

            Equal(AssignmentTargetLimit + 1, source.MoveNextCalls, "Lazy Zone assignment requested an entry beyond 10,001.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Lazy oversize Zone assignment changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Lazy oversize Zone assignment changed project timestamp.");
            Equal(fixture.SourceZone.Id, fixture.Element.ZoneId, "Lazy oversize Zone assignment changed ZoneId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Lazy oversize Zone assignment dirtied the element.");
        }

        private static void ExactBoundDuplicatesRemainSupported()
        {
            var fixture = NewFixture("exact");
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = ProjectZoneService.Assign(
                fixture.Project,
                fixture.TargetZone.Id,
                Repeat(fixture.Element, AssignmentTargetLimit));

            Equal(1, changed, "Exact-bound duplicate Zone targets did not preserve deduplication semantics.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Exact-bound Zone assignment did not commit exactly once.");
            Equal(fixture.TargetZone.Id, fixture.Element.ZoneId, "Exact-bound Zone assignment did not update ZoneId.");
            True(
                (fixture.Element.Dirty & (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) ==
                (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity),
                "Exact-bound Zone assignment did not preserve dirty flags.");

            fixture.Element.MarkClean(ElementDirtyFlags.All);
            var beforeNoOpVersion = fixture.Project.ChangeVersion;
            var beforeNoOpUtc = fixture.Project.UpdatedUtc;
            var noOpChanged = ProjectZoneService.Assign(
                fixture.Project,
                fixture.TargetZone.Id,
                Repeat(fixture.Element, AssignmentTargetLimit));

            Equal(0, noOpChanged, "Exact-bound no-op Zone assignment reported a change.");
            Equal(beforeNoOpVersion, fixture.Project.ChangeVersion, "Exact-bound no-op Zone assignment changed project version.");
            Equal(beforeNoOpUtc, fixture.Project.UpdatedUtc, "Exact-bound no-op Zone assignment changed project timestamp.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Exact-bound no-op Zone assignment dirtied the element.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("zone-bound-" + suffix, "Zone assignment bound " + suffix);
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

        private static IEnumerable<ProjectElement> Repeat(ProjectElement element, int count)
        {
            for (var index = 0; index < count; index++)
                yield return element;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ZoneDefinition sourceZone, ZoneDefinition targetZone, ProjectElement element)
            {
                Project = project;
                SourceZone = sourceZone;
                TargetZone = targetZone;
                Element = element;
            }

            public ProjectState Project { get; }
            public ZoneDefinition SourceZone { get; }
            public ZoneDefinition TargetZone { get; }
            public ProjectElement Element { get; }
        }

        private sealed class NoEnumerationCollection : ICollection<ProjectElement>
        {
            private readonly ProjectElement _element;

            public NoEnumerationCollection(ProjectElement element, int count)
            {
                _element = element;
                Count = count;
            }

            public int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Counted oversize source must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class NonGenericNoEnumerationCollection : IEnumerable<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;

            public NonGenericNoEnumerationCollection(ProjectElement element, int count)
            {
                _element = element;
                Count = count;
            }

            public int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Non-generic counted oversize source must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountInterfacesCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;
            private readonly int _oversizeCount;

            public ConflictingCountInterfacesCollection(ProjectElement element, int oversizeCount)
            {
                _element = element;
                _oversizeCount = oversizeCount;
            }

            public int EnumerationAttempts { get; private set; }
            public int Count => 1;
            int IReadOnlyCollection<ProjectElement>.Count => _oversizeCount;
            int ICollection.Count => _oversizeCount;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Conflicting count contracts must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class VersionMutatingCountCollection : ICollection<ProjectElement>
        {
            private readonly ProjectState _project;
            private readonly ProjectElement _element;

            public VersionMutatingCountCollection(ProjectState project, ProjectElement element)
            {
                _project = project;
                _element = element;
            }

            public int CountReads { get; private set; }
            public int EnumerationAttempts { get; private set; }
            public int Count
            {
                get
                {
                    CountReads++;
                    if (CountReads == 1) _project.Touch();
                    return 1;
                }
            }
            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Version-mutating counted source must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class GuardedInfiniteRepeat : IEnumerable<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly int _maxAllowedMoveNextCalls;

            public GuardedInfiniteRepeat(ProjectElement element, int maxAllowedMoveNextCalls)
            {
                _element = element;
                _maxAllowedMoveNextCalls = maxAllowedMoveNextCalls;
            }

            public int MoveNextCalls { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly GuardedInfiniteRepeat _owner;

                public Enumerator(GuardedInfiniteRepeat owner)
                {
                    _owner = owner;
                }

                public ProjectElement Current => _owner._element;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner.MoveNextCalls > _owner._maxAllowedMoveNextCalls)
                        throw new Exception("Zone assignment requested target entry 10,002 or later.");
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
