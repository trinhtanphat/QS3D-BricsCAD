using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignmentKnownCountCurrentSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeCurrent();
            StreamingHardLimitRejectsBeforeCurrent();
        }

        private static void KnownCountOverrunRejectsBeforeCurrent()
        {
            var fixture = NewFixture("known-count-current");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new CountedPoisonSource(fixture.Element);

            ThrowsWithMessage<InvalidOperationException>(
                () => ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source),
                "yielded more entries than its known Count");

            Equal(1, source.EnumerationCount,
                "Known-Count Family assignment source must be enumerated exactly once.");
            Equal(2, source.MoveNextCalls,
                "Known-Count overrun must observe the first extra MoveNext.");
            Equal(1, source.CurrentReads,
                "Known-Count overrun must reject before observing the first extra Current.");
            AssertRejectedUnchanged(fixture, beforeVersion, beforeUtc, "Known-Count Current overrun");
        }

        private static void StreamingHardLimitRejectsBeforeCurrent()
        {
            var fixture = NewFixture("streaming-cap-current");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var source = new StreamingPoisonSource(fixture.Element);

            ThrowsWithMessage<InvalidOperationException>(
                () => ProjectFamilyService.Assign(fixture.Project, fixture.TargetFamily.Id, source),
                "supports at most 10000 target entries");

            Equal(1, source.EnumerationCount,
                "Streaming Family assignment source must be enumerated exactly once.");
            Equal(10001, source.MoveNextCalls,
                "Streaming hard-limit rejection must observe the first extra MoveNext.");
            Equal(10000, source.CurrentReads,
                "Streaming hard-limit rejection must occur before observing the first extra Current.");
            AssertRejectedUnchanged(fixture, beforeVersion, beforeUtc, "Streaming Current hard limit");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("family-current-" + suffix, "Family Current " + suffix);
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

        private sealed class CountedPoisonSource : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;

            internal CountedPoisonSource(ProjectElement element)
            {
                _element = element;
            }

            internal int EnumerationCount { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            int ICollection<ProjectElement>.Count => 1;
            int IReadOnlyCollection<ProjectElement>.Count => 1;
            int ICollection.Count => 1;
            bool ICollection<ProjectElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Family assignment counted source was enumerated more than once.");
                return new CountedPoisonEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ProjectElement>.Add(ProjectElement item) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Contains(ProjectElement item) => ReferenceEquals(item, _element);
            void ICollection<ProjectElement>.CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Remove(ProjectElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class CountedPoisonEnumerator : IEnumerator<ProjectElement>
            {
                private readonly CountedPoisonSource _owner;
                private int _index = -1;

                internal CountedPoisonEnumerator(CountedPoisonSource owner)
                {
                    _owner = owner;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index >= 1)
                            throw new InvalidOperationException("POISON counted Current beyond known Count.");
                        return _owner._element;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < 2;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingPoisonSource : IEnumerable<ProjectElement>
        {
            private readonly ProjectElement _element;

            internal StreamingPoisonSource(ProjectElement element)
            {
                _element = element;
            }

            internal int EnumerationCount { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Family assignment streaming source was enumerated more than once.");
                return new StreamingPoisonEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class StreamingPoisonEnumerator : IEnumerator<ProjectElement>
            {
                private readonly StreamingPoisonSource _owner;
                private int _index = -1;

                internal StreamingPoisonEnumerator(StreamingPoisonSource owner)
                {
                    _owner = owner;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index >= 10000)
                            throw new InvalidOperationException("POISON streaming Current beyond Family assignment hard limit.");
                        return _owner._element;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < 10001;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
