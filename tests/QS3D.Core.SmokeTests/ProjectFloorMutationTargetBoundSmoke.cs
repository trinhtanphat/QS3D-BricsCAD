using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorMutationTargetBoundSmoke
    {
        private const int MaximumTargets = 10000;

        internal static void Run()
        {
            KnownGenericNegativeFailsBeforeEnumerationOrMutation();
            KnownReadOnlyNegativeFailsBeforeEnumeration();
            KnownNonGenericNegativeFailsBeforeEnumeration();
            SharedMutationEntryPointsRejectNegativeKnownCountBeforeEnumeration();
            KnownGenericOversizeFailsBeforeEnumerationOrMutation();
            KnownReadOnlyOversizeFailsBeforeEnumeration();
            KnownNonGenericOversizeFailsBeforeEnumeration();
            SharedMutationEntryPointsRejectKnownOversizeBeforeEnumeration();
            DishonestCountStopsAtFirstDisallowedEntry();
            ExactBoundaryRemainsAcceptedAndDeduplicated();
            ForeignTargetStillFailsWithoutMutation();
        }

        private static void KnownGenericNegativeFailsBeforeEnumerationOrMutation()
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: -1,
                yieldedCount: 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;
            var beforeDirty = fixture.Element.Dirty;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("invalid negative known count", error.Message, "Negative generic Floor target Count must fail closed.");
            Equal(0, targets.ObservedEntries, "Negative generic ICollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Negative generic Count must fail before project mutation.");
            Equal(beforeFloor, fixture.Element.FloorId, "Negative generic Count must preserve FloorId.");
            Equal(beforeDirty, fixture.Element.Dirty, "Negative generic Count must preserve dirty state.");
        }

        private static void KnownReadOnlyNegativeFailsBeforeEnumeration()
        {
            var fixture = NewFixture();
            var targets = new ReadOnlyProbeCollection(
                fixture.Element,
                reportedCount: -1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("invalid negative known count", error.Message, "Negative IReadOnlyCollection Count must fail closed.");
            Equal(0, targets.ObservedEntries, "Negative IReadOnlyCollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Negative read-only Count rejection must be atomic.");
        }

        private static void KnownNonGenericNegativeFailsBeforeEnumeration()
        {
            var fixture = NewFixture();
            var targets = new NonGenericProbeCollection(
                fixture.Element,
                reportedCount: -1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("invalid negative known count", error.Message, "Negative non-generic ICollection Count must fail closed.");
            Equal(0, targets.ObservedEntries, "Negative non-generic ICollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Negative non-generic Count rejection must be atomic.");
        }

        private static void SharedMutationEntryPointsRejectNegativeKnownCountBeforeEnumeration()
        {
            AssertNegativeEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets), "Assign");
            AssertNegativeEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.AssignBottomLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignBottomLevel");
            AssertNegativeEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.AssignTopLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignTopLevel");
            AssertNegativeEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.ClearVerticalLevels(fixture.Project, targets), "ClearVerticalLevels");
        }

        private static void AssertNegativeEntryPointRejectsBeforeEnumeration(
            Action<Fixture, ProbeCollection> action,
            string label)
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: -1,
                yieldedCount: 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;
            var beforeBottom = fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey];
            var beforeTop = fixture.Element.Properties[ProjectFloorService.TopLevelIdKey];

            var error = Capture<InvalidOperationException>(() => action(fixture, targets));

            Contains("invalid negative known count", error.Message, label + " must report the negative known-count contract.");
            Equal(0, targets.ObservedEntries, label + " must reject negative known Count before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " negative-count rejection must not mutate project version.");
            Equal(beforeFloor, fixture.Element.FloorId, label + " negative-count rejection must preserve FloorId.");
            Equal(beforeBottom, fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey], label + " negative-count rejection must preserve Bottom Level.");
            Equal(beforeTop, fixture.Element.Properties[ProjectFloorService.TopLevelIdKey], label + " negative-count rejection must preserve Top Level.");
        }

        private static void KnownGenericOversizeFailsBeforeEnumerationOrMutation()
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: MaximumTargets + 1,
                yieldedCount: MaximumTargets + 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;
            var beforeDirty = fixture.Element.Dirty;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("10000 element limit", error.Message, "Known oversized Floor mutation input must report the bounded-ingestion contract.");
            Equal(0, targets.ObservedEntries, "Known oversized ICollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Known oversized input must fail before project mutation.");
            Equal(beforeFloor, fixture.Element.FloorId, "Known oversized input must preserve FloorId.");
            Equal(beforeDirty, fixture.Element.Dirty, "Known oversized input must preserve dirty state.");
        }

        private static void KnownReadOnlyOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture();
            var targets = new ReadOnlyProbeCollection(
                fixture.Element,
                reportedCount: MaximumTargets + 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;

            Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Equal(0, targets.ObservedEntries, "Known oversized IReadOnlyCollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Read-only known-count rejection must be atomic.");
        }

        private static void KnownNonGenericOversizeFailsBeforeEnumeration()
        {
            var fixture = NewFixture();
            var targets = new NonGenericProbeCollection(
                fixture.Element,
                reportedCount: MaximumTargets + 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;

            Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Equal(0, targets.ObservedEntries, "Known oversized non-generic ICollection input must fail before enumeration.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Non-generic known-count rejection must be atomic.");
        }

        private static void SharedMutationEntryPointsRejectKnownOversizeBeforeEnumeration()
        {
            AssertEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets), "Assign");
            AssertEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.AssignBottomLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignBottomLevel");
            AssertEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.AssignTopLevel(fixture.Project, fixture.TargetFloor.Id, targets), "AssignTopLevel");
            AssertEntryPointRejectsBeforeEnumeration((fixture, targets) =>
                ProjectFloorService.ClearVerticalLevels(fixture.Project, targets), "ClearVerticalLevels");
        }

        private static void AssertEntryPointRejectsBeforeEnumeration(
            Action<Fixture, ProbeCollection> action,
            string label)
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: MaximumTargets + 1,
                yieldedCount: MaximumTargets + 1,
                failIfEnumerated: true);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;
            var beforeBottom = fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey];
            var beforeTop = fixture.Element.Properties[ProjectFloorService.TopLevelIdKey];

            Capture<InvalidOperationException>(() => action(fixture, targets));

            Equal(0, targets.ObservedEntries, label + " must share the known-count fail-fast path.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, label + " oversized rejection must not mutate project version.");
            Equal(beforeFloor, fixture.Element.FloorId, label + " oversized rejection must preserve FloorId.");
            Equal(beforeBottom, fixture.Element.Properties[ProjectFloorService.BottomLevelIdKey], label + " oversized rejection must preserve Bottom Level.");
            Equal(beforeTop, fixture.Element.Properties[ProjectFloorService.TopLevelIdKey], label + " oversized rejection must preserve Top Level.");
        }

        private static void DishonestCountStopsAtFirstDisallowedEntry()
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: 1,
                yieldedCount: MaximumTargets + 2,
                failIfEnumerated: false);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets));

            Contains("10000 element limit", error.Message, "Dishonest Count must not bypass the streaming Floor target bound.");
            Equal(MaximumTargets + 1, targets.ObservedEntries, "Streaming enforcement must stop at entry 10001 and not consume entry 10002.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Streaming-bound rejection must occur before project mutation.");
            Equal(beforeFloor, fixture.Element.FloorId, "Streaming-bound rejection must preserve target FloorId.");
        }

        private static void ExactBoundaryRemainsAcceptedAndDeduplicated()
        {
            var fixture = NewFixture();
            var targets = new ProbeCollection(
                fixture.Element,
                reportedCount: MaximumTargets,
                yieldedCount: MaximumTargets,
                failIfEnumerated: false);
            var beforeVersion = fixture.Project.ChangeVersion;

            var changed = ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, targets);

            Equal(1, changed, "Repeated references at the exact boundary must remain deduplicated by semantic id.");
            Equal(MaximumTargets, targets.ObservedEntries, "Exactly 10000 targets must remain accepted and fully consumed.");
            Equal(fixture.TargetFloor.Id, fixture.Element.FloorId, "Accepted boundary input must perform the ordinary Floor assignment.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Accepted boundary input must preserve one-touch mutation semantics.");
        }

        private static void ForeignTargetStillFailsWithoutMutation()
        {
            var fixture = NewFixture();
            var foreign = new ProjectElement("FOREIGN", ElementCategory.Beam, string.Empty, fixture.SourceFloor.Id, string.Empty);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, new[] { foreign }));

            Contains("does not belong to the project instance", error.Message, "Bounded ingestion must preserve the existing ownership failure.");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Foreign-target rejection must remain atomic.");
            Equal(beforeFloor, fixture.Element.FloorId, "Foreign-target rejection must not mutate owned elements.");
        }

        private static Fixture NewFixture()
        {
            var project = new ProjectState("floor-target-bound", "Floor target bound");
            var sourceFloor = new FloorDefinition("F1", "Floor 1", 0d);
            var targetFloor = new FloorDefinition("F2", "Floor 2", 3d);
            project.Floors.Add(sourceFloor);
            project.Floors.Add(targetFloor);
            project.ActiveFloorId = sourceFloor.Id;

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, sourceFloor.Id, string.Empty);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = sourceFloor.Id;
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0";
            element.Properties[ProjectFloorService.TopLevelIdKey] = targetFloor.Id;
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "0";
            project.Elements.Add(element);
            return new Fixture(project, sourceFloor, targetFloor, element);
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
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class Fixture
        {
            internal Fixture(ProjectState project, FloorDefinition sourceFloor, FloorDefinition targetFloor, ProjectElement element)
            {
                Project = project;
                SourceFloor = sourceFloor;
                TargetFloor = targetFloor;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal FloorDefinition SourceFloor { get; }
            internal FloorDefinition TargetFloor { get; }
            internal ProjectElement Element { get; }
        }

        private sealed class ProbeCollection : ICollection<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly int _reportedCount;
            private readonly int _yieldedCount;
            private readonly bool _failIfEnumerated;

            internal ProbeCollection(ProjectElement element, int reportedCount, int yieldedCount, bool failIfEnumerated)
            {
                _element = element;
                _reportedCount = reportedCount;
                _yieldedCount = yieldedCount;
                _failIfEnumerated = failIfEnumerated;
            }

            internal int ObservedEntries { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                if (_failIfEnumerated) throw new Exception("Known oversized collection must not be enumerated.");
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

        private sealed class ReadOnlyProbeCollection : IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly int _reportedCount;
            private readonly bool _failIfEnumerated;

            internal ReadOnlyProbeCollection(ProjectElement element, int reportedCount, bool failIfEnumerated)
            {
                _element = element;
                _reportedCount = reportedCount;
                _failIfEnumerated = failIfEnumerated;
            }

            internal int ObservedEntries { get; private set; }
            public int Count => _reportedCount;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                if (_failIfEnumerated) throw new Exception("Known oversized read-only collection must not be enumerated.");
                ObservedEntries++;
                yield return _element;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericProbeCollection : IEnumerable<ProjectElement>, ICollection
        {
            private readonly ProjectElement _element;
            private readonly int _reportedCount;
            private readonly bool _failIfEnumerated;

            internal NonGenericProbeCollection(ProjectElement element, int reportedCount, bool failIfEnumerated)
            {
                _element = element;
                _reportedCount = reportedCount;
                _failIfEnumerated = failIfEnumerated;
            }

            internal int ObservedEntries { get; private set; }
            public int Count => _reportedCount;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                if (_failIfEnumerated) throw new Exception("Known oversized non-generic collection must not be enumerated.");
                ObservedEntries++;
                yield return _element;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
