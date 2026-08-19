using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignmentCountTraversalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ConflictingKnownCountsFailBeforeEnumeration();
            KnownCountUnderYieldFailsWithoutMutation();
            KnownCountOverYieldFailsAtFirstExtraEntryWithoutMutation();
            ExactKnownCountTraversalPreservesDuplicateTargetSemantics();
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var fixture = NewFixture("conflict");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new CountedTraversalSource(fixture.Element, actualCount: 1, genericCount: 1, readOnlyCount: 2, nonGenericCount: 1);

            ThrowsWithMessage<InvalidOperationException>(
                () => ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source),
                "conflicting known counts");

            Equal(0, source.EnumerationCount, "Conflicting Family assignment Counts must fail before enumeration.");
            AssertRejectedUnchanged(fixture, beforeVersion, beforeUtc, "Conflicting known Counts");
        }

        private static void KnownCountUnderYieldFailsWithoutMutation()
        {
            var fixture = NewFixture("under-yield");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new CountedTraversalSource(fixture.Element, actualCount: 1, genericCount: 2, readOnlyCount: 2, nonGenericCount: 2);

            ThrowsWithMessage<InvalidOperationException>(
                () => ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source),
                "known Count does not match traversed target count");

            Equal(1, source.EnumerationCount, "Under-yield Family assignment source must be enumerated exactly once.");
            Equal(2, source.MoveNextCalls, "Under-yield Family assignment must stop after the first false MoveNext.");
            AssertRejectedUnchanged(fixture, beforeVersion, beforeUtc, "Known Count under-yield");
        }

        private static void KnownCountOverYieldFailsAtFirstExtraEntryWithoutMutation()
        {
            var fixture = NewFixture("over-yield");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new CountedTraversalSource(fixture.Element, actualCount: 2, genericCount: 1, readOnlyCount: 1, nonGenericCount: 1);

            ThrowsWithMessage<InvalidOperationException>(
                () => ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source),
                "yielded more entries than its known Count");

            Equal(1, source.EnumerationCount, "Over-yield Family assignment source must be enumerated exactly once.");
            Equal(2, source.MoveNextCalls, "Over-yield Family assignment must fail on the first entry beyond known Count.");
            AssertRejectedUnchanged(fixture, beforeVersion, beforeUtc, "Known Count over-yield");
        }

        private static void ExactKnownCountTraversalPreservesDuplicateTargetSemantics()
        {
            var fixture = NewFixture("exact");
            var beforeVersion = fixture.Project.ChangeVersion;
            var source = new CountedTraversalSource(fixture.Element, actualCount: 2, genericCount: 2, readOnlyCount: 2, nonGenericCount: 2);

            var changed = ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source);

            Equal(1, changed, "Exact known Count traversal must preserve duplicate target de-duplication.");
            Equal(1, source.EnumerationCount, "Exact known Count Family assignment source must be enumerated once.");
            Equal(3, source.MoveNextCalls, "Exact known Count traversal must consume exactly two entries and one terminal MoveNext.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Exact known Count Family assignment must commit once.");
            Equal(fixture.TargetFamily.Id, fixture.Element.FamilyId, "Exact known Count Family assignment did not update FamilyId.");
            Equal(ElementDirtyFlags.All, fixture.Element.Dirty, "Exact known Count Family assignment did not preserve dirty semantics.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("family-count-" + suffix, "Family Count Traversal " + suffix);
            var sourceFamily = new ProjectFamily("family-source", "Source Family", ElementCategory.Beam);
            var targetFamily = new ProjectFamily("family-target", "Target Family", ElementCategory.Beam);
            project.Families.Add(sourceFamily);
            project.Families.Add(targetFamily);

            var element = new ProjectElement(
                "element-1",
                ElementCategory.Beam,
                sourceFamily.Id,
                string.Empty,
                string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Fixture(project, sourceFamily, targetFamily, element);
        }

        private static void AssertRejectedUnchanged(Fixture fixture, long beforeVersion, DateTime beforeUtc, string label)
        {
            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, label + " changed project timestamp.");
            Equal(fixture.SourceFamily.Id, fixture.Element.FamilyId, label + " changed FamilyId.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, label + " dirtied the element.");
        }

        private static void ThrowsWithMessage<TException>(Action action, string expectedFragment) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        "Expected diagnostic containing '" + expectedFragment + "', actual: " + ex.Message,
                        ex);
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class Fixture
        {
            internal Fixture(ProjectState project, ProjectFamily sourceFamily, ProjectFamily targetFamily, ProjectElement element)
            {
                Project = project;
                SourceFamily = sourceFamily;
                TargetFamily = targetFamily;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal ProjectFamily SourceFamily { get; }
            internal ProjectFamily TargetFamily { get; }
            internal ProjectElement Element { get; }
        }

        private sealed class CountedTraversalSource : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;
            private readonly int _actualCount;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal CountedTraversalSource(
                ProjectElement element,
                int actualCount,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount)
            {
                _element = element;
                _actualCount = actualCount;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            internal int EnumerationCount { get; private set; }
            internal int MoveNextCalls { get; private set; }
            int ICollection<ProjectElement>.Count => _genericCount;
            int IReadOnlyCollection<ProjectElement>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<ProjectElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Family assignment counted source was enumerated more than once.");
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ProjectElement>.Add(ProjectElement item) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Contains(ProjectElement item) => ReferenceEquals(item, _element);
            void ICollection<ProjectElement>.CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Remove(ProjectElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly CountedTraversalSource _owner;
                private int _index = -1;

                internal Enumerator(CountedTraversalSource owner)
                {
                    _owner = owner;
                }

                public ProjectElement Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount) return false;
                    Current = _owner._element;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
