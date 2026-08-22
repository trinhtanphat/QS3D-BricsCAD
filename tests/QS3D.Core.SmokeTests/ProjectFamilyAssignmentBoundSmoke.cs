using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignmentBoundSmoke
    {
        private const int AssignmentTargetLimit = 10000;

        public static void Run()
        {
            GenericNegativeCountFailsBeforeEnumeration();
            ReadOnlyNegativeCountFailsBeforeEnumeration();
            NonGenericNegativeCountFailsBeforeEnumeration();
            CountedOversizeFailsBeforeEnumeration();
            ReadOnlyCountedOversizeFailsBeforeEnumeration();
            NonGenericCountedOversizeFailsBeforeEnumeration();
            ConflictingCountInterfacesFailBeforeEnumeration();
            CountVersionMutationFailsBeforeEnumeration();
            LazyOversizeStopsAtFirstImpossibleEntry();
            ExactBoundDuplicatesRemainSupported();
        }

        private static void GenericNegativeCountFailsBeforeEnumeration()
        {
            var fixture = NewFixture("negative-generic");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NoEnumerationCollection(fixture.Element, -1);

            var error = Capture<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Contains("invalid negative known count", error.Message, "Negative generic Family assignment Count must fail closed.");
            Equal(0, source.EnumerationAttempts, "Negative generic Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Negative generic Family assignment");
        }

        private static void ReadOnlyNegativeCountFailsBeforeEnumeration()
        {
            var fixture = NewFixture("negative-readonly");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new ReadOnlyNoEnumerationCollection(fixture.Element, -1);

            var error = Capture<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Contains("invalid negative known count", error.Message, "Negative read-only Family assignment Count must fail closed.");
            Equal(0, source.EnumerationAttempts, "Negative IReadOnlyCollection Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Negative read-only Family assignment");
        }

        private static void NonGenericNegativeCountFailsBeforeEnumeration()
        {
            var fixture = NewFixture("negative-non-generic");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NonGenericNoEnumerationCollection(fixture.Element, -1);

            var error = Capture<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Contains("invalid negative known count", error.Message, "Negative non-generic Family assignment Count must fail closed.");
            Equal(0, source.EnumerationAttempts, "Negative non-generic Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Negative non-generic Family assignment");
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture("counted");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NoEnumerationCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(0, source.EnumerationAttempts, "Counted oversize Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Counted oversize Family assignment");
        }

        private static void ReadOnlyCountedOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture("read-only-counted");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new ReadOnlyNoEnumerationCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(0, source.EnumerationAttempts, "Read-only counted oversize Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Read-only counted oversize Family assignment");
        }

        private static void NonGenericCountedOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture("non-generic-counted");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new NonGenericNoEnumerationCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(0, source.EnumerationAttempts, "Non-generic counted oversize Family assignment enumerated the source.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Non-generic counted oversize Family assignment");
        }

        private static void ConflictingCountInterfacesFailBeforeEnumeration()
        {
            var fixture = NewFixture("conflicting-counts");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new ConflictingCountInterfacesCollection(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(0, source.EnumerationAttempts, "Conflicting count contracts bypassed known-count Family assignment rejection.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Conflicting count Family assignment");
        }

        private static void CountVersionMutationFailsBeforeEnumeration()
        {
            var fixture = NewFixture("count-version");
            var beforeVersion = fixture.Project.ChangeVersion;
            var source = new VersionMutatingCountCollection(fixture.Project, fixture.Element);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(1, source.CountReads, "Version-mutating counted Family source did not read Count exactly once.");
            Equal(0, source.EnumerationAttempts, "Version-mutating counted Family source was enumerated after ChangeVersion drift.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Count fixture did not produce the expected single ChangeVersion drift.");
            Equal(fixture.SourceFamily.Id, fixture.Element.FamilyId, "Version-mutating Count changed FamilyId through assignment.");
            Equal("0.2", fixture.Element.Properties["ThicknessM"], "Version-mutating Count changed inherited properties through assignment.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Version-mutating Count caused Family assignment dirty flags.");
        }

        private static void LazyOversizeStopsAtFirstImpossibleEntry()
        {
            var fixture = NewFixture("lazy");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new GuardedInfiniteRepeat(fixture.Element, AssignmentTargetLimit + 1);

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source));

            Equal(AssignmentTargetLimit + 1, source.MoveNextCalls, "Lazy Family assignment requested target entry 10,002 or later.");
            AssertRejectedAssignmentUnchanged(fixture, beforeVersion, beforeUtc, "Lazy oversize Family assignment");
        }

        private static void ExactBoundDuplicatesRemainSupported()
        {
            var fixture = NewFixture("exact");
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = ProjectFamilyService.Assign(
                fixture.Project,
                fixture.TargetFamily.Id,
                Repeat(fixture.Element, AssignmentTargetLimit));

            Equal(1, changed, "Exact-bound duplicate Family targets did not preserve deduplication semantics.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Exact-bound Family assignment did not commit exactly once.");
            Equal(fixture.TargetFamily.Id, fixture.Element.FamilyId, "Exact-bound Family assignment did not update FamilyId.");
            Equal("0.3", fixture.Element.Properties["ThicknessM"], "Exact-bound Family assignment did not replace inherited defaults.");
            Equal("keep", fixture.Element.Properties["InstanceOverride"], "Exact-bound Family assignment did not preserve instance overrides.");
            Equal(ElementDirtyFlags.All, fixture.Element.Dirty, "Exact-bound Family assignment did not preserve dirty semantics.");

            fixture.Element.MarkClean(ElementDirtyFlags.All);
            var beforeNoOpVersion = fixture.Project.ChangeVersion;
            var beforeNoOpUtc = fixture.Project.UpdatedUtc;
            var beforeNoOpElementUtc = fixture.Element.UpdatedUtc;
            var noOpChanged = ProjectFamilyService.Assign(
                fixture.Project,
                fixture.TargetFamily.Id,
                Repeat(fixture.Element, AssignmentTargetLimit));

            Equal(0, noOpChanged, "Exact-bound no-op Family assignment reported a change.");
            Equal(beforeNoOpVersion, fixture.Project.ChangeVersion, "Exact-bound no-op Family assignment changed project version.");
            Equal(beforeNoOpUtc, fixture.Project.UpdatedUtc, "Exact-bound no-op Family assignment changed project timestamp.");
            Equal(beforeNoOpElementUtc, fixture.Element.UpdatedUtc, "Exact-bound no-op Family assignment changed element timestamp.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Exact-bound no-op Family assignment dirtied the element.");
            Equal("0.3", fixture.Element.Properties["ThicknessM"], "Exact-bound no-op Family assignment changed inherited defaults.");
            Equal("keep", fixture.Element.Properties["InstanceOverride"], "Exact-bound no-op Family assignment changed instance overrides.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("family-bound-" + suffix, "Family assignment bound " + suffix);
            var sourceFamily = new ProjectFamily("family-source", "Source family", ElementCategory.Beam);
            sourceFamily.Properties["ThicknessM"] = "0.2";
            var targetFamily = new ProjectFamily("family-target", "Target family", ElementCategory.Beam);
            targetFamily.Properties["ThicknessM"] = "0.3";
            project.Families.Add(sourceFamily);
            project.Families.Add(targetFamily);

            var element = new ProjectElement(
                "element-1",
                ElementCategory.Beam,
                sourceFamily.Id,
                string.Empty,
                string.Empty);
            element.Properties["ThicknessM"] = "0.2";
            element.Properties["InstanceOverride"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Fixture(project, sourceFamily, targetFamily, element);
        }

        private static void AssertRejectedAssignmentUnchanged(Fixture fixture, long beforeVersion, DateTime beforeUtc, string label)
        {
            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, label + " changed project timestamp.");
            Equal(fixture.SourceFamily.Id, fixture.Element.FamilyId, label + " changed FamilyId.");
            Equal("0.2", fixture.Element.Properties["ThicknessM"], label + " changed inherited properties.");
            Equal("keep", fixture.Element.Properties["InstanceOverride"], label + " changed instance overrides.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, label + " dirtied the element.");
        }

        private static IEnumerable<ProjectElement> Repeat(ProjectElement element, int count)
        {
            for (var index = 0; index < count; index++)
                yield return element;
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            Capture<T>(action);
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual=" + (actual ?? "<null>") + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ProjectFamily sourceFamily, ProjectFamily targetFamily, ProjectElement element)
            {
                Project = project;
                SourceFamily = sourceFamily;
                TargetFamily = targetFamily;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectFamily SourceFamily { get; }
            public ProjectFamily TargetFamily { get; }
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
                throw new Exception("Counted Family source must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyNoEnumerationCollection : IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement _element;

            public ReadOnlyNoEnumerationCollection(ProjectElement element, int count)
            {
                _element = element;
                Count = count;
            }

            public int EnumerationAttempts { get; private set; }
            public int Count { get; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Read-only counted Family source must be rejected before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
                throw new Exception("Non-generic counted Family source must be rejected before enumeration.");
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
                throw new Exception("Conflicting Family count contracts must be rejected before enumeration.");
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
                throw new Exception("Version-mutating counted Family source must be rejected before enumeration.");
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
                        throw new Exception("Family assignment requested target entry 10,002 or later.");
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
