using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorKnownCountTraversalSmoke
    {
        internal static void Run()
        {
            SharedMutationEntryPointsRejectUnderYieldWithoutMutation();
            SharedMutationEntryPointsRejectOverYieldWithoutMutation();
            ConflictingKnownCountsFailBeforeEnumeration();
            HonestCountedInputRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void SharedMutationEntryPointsRejectUnderYieldWithoutMutation()
        {
            AssertMismatch((fixture, targets) => ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets), "Assign", 2, 1);
            AssertMismatch((fixture, targets) => ProjectFloorService.AssignBottomLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignBottomLevel", 2, 1);
            AssertMismatch((fixture, targets) => ProjectFloorService.AssignTopLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignTopLevel", 2, 1);
            AssertMismatch((fixture, targets) => ProjectFloorService.ClearVerticalLevels(fixture.Project, targets), "ClearVerticalLevels", 2, 1);
        }

        private static void SharedMutationEntryPointsRejectOverYieldWithoutMutation()
        {
            AssertMismatch((fixture, targets) => ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets), "Assign", 1, 2);
            AssertMismatch((fixture, targets) => ProjectFloorService.AssignBottomLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignBottomLevel", 1, 2);
            AssertMismatch((fixture, targets) => ProjectFloorService.AssignTopLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignTopLevel", 1, 2);
            AssertMismatch((fixture, targets) => ProjectFloorService.ClearVerticalLevels(fixture.Project, targets), "ClearVerticalLevels", 1, 2);
        }

        private static void AssertMismatch(
            Action<Fixture, IEnumerable<ProjectElement>> action,
            string label,
            int advertisedCount,
            int yieldedCount)
        {
            var fixture = NewFixture();
            var targets = new MisreportedCollection(fixture.Element, advertisedCount, yieldedCount);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;
            var beforeBottom = fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey];
            var beforeTop = fixture.Element.Properties[ProjectFloorService.TopLevelIdKey];
            var beforeDirty = fixture.Element.Dirty;

            var error = Capture<InvalidOperationException>(() => action(fixture, targets));

            Contains("known count does not match", error.Message, label + " must reject Count/traversal mismatch.");
            Equal(yieldedCount, targets.ObservedEntries, label + " must compare Count against the completed bounded traversal.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " mismatch rejection must not mutate project version.");
            Equal(beforeFloor, fixture.Element.FloorId, label + " mismatch rejection must preserve FloorId.");
            Equal(beforeBottom, fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey], label + " mismatch rejection must preserve Bottom Level.");
            Equal(beforeTop, fixture.Element.Properties[ProjectFloorService.TopLevelIdKey], label + " mismatch rejection must preserve Top Level.");
            Equal(beforeDirty, fixture.Element.Dirty, label + " mismatch rejection must preserve dirty state.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var fixture = NewFixture();
            var targets = new ConflictingCountCollection(fixture.Element);
            var beforeVersion = fixture.Project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("conflicting known counts", error.Message, "Conflicting supported Floor target Count contracts must fail closed.");
            Equal(0, targets.ObservedEntries, "Conflicting Count contracts must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Conflicting Count rejection must not mutate project state.");
        }

        private static void HonestCountedInputRemainsAccepted()
        {
            var fixture = NewFixture();
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = ProjectFloorService.Assign(
                fixture.Project,
                fixture.TargetFloor.Id,
                new[] { fixture.Element });

            Equal(1, changed, "Honest counted Floor assignment must remain accepted.");
            Equal(fixture.TargetFloor.Id, fixture.Element.FloorId, "Honest counted input must perform the ordinary assignment.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Honest counted input must preserve one-touch mutation semantics.");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var fixture = NewFixture();
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = ProjectFloorService.Assign(
                fixture.Project,
                fixture.TargetFloor.Id,
                Stream(fixture.Element));

            Equal(1, changed, "Pure IEnumerable Floor assignment must remain supported without a Count contract.");
            Equal(fixture.TargetFloor.Id, fixture.Element.FloorId, "Pure streaming input must perform the ordinary assignment.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Pure streaming input must preserve one-touch mutation semantics.");
        }

        private static IEnumerable<ProjectElement> Stream(ProjectElement element)
        {
            yield return element;
        }

        private static Fixture NewFixture()
        {
            var project = new ProjectState("floor-count-traversal", "Floor Count traversal");
            var sourceFloor = new FloorDefinition("F1", "Floor 1", 0d);
            var targetFloor = new FloorDefinition("F2", "Floor 2", 3d);
            var topFloor = new FloorDefinition("F3", "Floor 3", 6d);
            project.Floors.Add(sourceFloor);
            project.Floors.Add(targetFloor);
            project.Floors.Add(topFloor);
            project.ActiveFloorId = sourceFloor.Id;

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, sourceFloor.Id, string.Empty);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = sourceFloor.Id;
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0";
            element.Properties[ProjectFloorService.TopLevelIdKey] = topFloor.Id;
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "0";
            project.Elements.Add(element);
            return new Fixture(project, targetFloor, element);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class Fixture
        {
            internal Fixture(ProjectState project, FloorDefinition targetFloor, ProjectElement element)
            {
                Project = project;
                TargetFloor = targetFloor;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal FloorDefinition TargetFloor { get; }
            internal ProjectElement Element { get; }
        }

        private sealed class MisreportedCollection : ICollection<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly int _yieldedCount;

            internal MisreportedCollection(ProjectElement element, int advertisedCount, int yieldedCount)
            {
                _element = element;
                Count = advertisedCount;
                _yieldedCount = yieldedCount;
            }

            internal int ObservedEntries { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                for (var index = 0; index < _yieldedCount; index++)
                {
                    ObservedEntries++;
                    yield return _element;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;

            internal ConflictingCountCollection(ProjectElement element)
            {
                _element = element;
            }

            internal int ObservedEntries { get; private set; }
            int ICollection<ProjectElement>.Count => 1;
            int IReadOnlyCollection<ProjectElement>.Count => 2;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                ObservedEntries++;
                yield return _element;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }

    internal static class ProjectFloorKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectFloorKnownCountTraversalSmoke.Run();
        }
    }
}
