using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorMutationTargetCountNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunRejectsBeforeAdditionalTraversal();
            HonestCountedAndStreamingInputsRemainSupported();
        }

        private static void KnownCountOverrunRejectsBeforeAdditionalTraversal()
        {
            var fixture = NewFixture();
            var source = new OverrunTargets(fixture.Element);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeFloor = fixture.Element.FloorId;

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(fixture.Project, fixture.TargetFloor.Id, source));

            Contains("known count", error.Message, "known Count overrun must fail closed at the first N+1 observation");
            Equal(2, source.MoveNextCalls, "first N+1 successful MoveNext must reject without terminal traversal");
            Equal(1, source.CurrentReads, "entry beyond advertised Count must never expose Current");
            Equal(beforeVersion, fixture.Project.ChangeVersion, "rejection must not mutate project version");
            Equal(beforeFloor, fixture.Element.FloorId, "rejection must not mutate element floor");
        }

        private static void HonestCountedAndStreamingInputsRemainSupported()
        {
            var counted = NewFixture();
            Equal(1, ProjectFloorService.Assign(counted.Project, counted.TargetFloor.Id, new[] { counted.Element }), "honest counted assignment");

            var streaming = NewFixture();
            Equal(1, ProjectFloorService.Assign(streaming.Project, streaming.TargetFloor.Id, Stream(streaming.Element)), "streaming assignment");
        }

        private static IEnumerable<ProjectElement> Stream(ProjectElement element)
        {
            yield return element;
        }

        private static Fixture NewFixture()
        {
            var project = new ProjectState("floor-target-no-overread", "Floor target no-overread");
            var sourceFloor = new FloorDefinition("F1", "Floor 1", 0d);
            var targetFloor = new FloorDefinition("F2", "Floor 2", 3d);
            project.Floors.Add(sourceFloor);
            project.Floors.Add(targetFloor);
            project.ActiveFloorId = sourceFloor.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, sourceFloor.Id, string.Empty);
            project.Elements.Add(element);
            return new Fixture(project, targetFloor, element);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + ". Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + ": expected=" + expected + ", actual=" + actual + ".");
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

        private sealed class OverrunTargets : IEnumerable<ProjectElement>, IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement _element;
            internal OverrunTargets(ProjectElement element) { _element = element; }
            public int Count => 1;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly OverrunTargets _owner;
                private int _index = -1;
                internal Enumerator(OverrunTargets owner) { _owner = owner; }
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < 2;
                }
                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._element;
                    }
                }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
