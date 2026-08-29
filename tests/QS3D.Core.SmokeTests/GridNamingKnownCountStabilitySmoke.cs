using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GenericCountDriftFailsAtomically();
            ReadOnlyCountDriftFailsAtomically();
            NonGenericCountDriftFailsAtomically();
            NegativePostTraversalCountFailsAtomically();
            ConflictingPostTraversalCountsFailAtomically();
            PostTraversalCountProjectMutationFailsBeforeGridMutation();
            StableCountedInputSucceeds();
            PureStreamingInputSucceeds();
        }

        private static void GenericCountDriftFailsAtomically()
        {
            var fixture = Fixture("grid-count-drift-generic");
            RejectAtomically(fixture, new GenericDriftCollection(new[] { fixture.Grid.Id }, 1, 2), "Grid renumber target source known Count changed during traversal.");
        }

        private static void ReadOnlyCountDriftFailsAtomically()
        {
            var fixture = Fixture("grid-count-drift-readonly");
            RejectAtomically(fixture, new ReadOnlyDriftCollection(new[] { fixture.Grid.Id }, 1, 2), "Grid renumber target source known Count changed during traversal.");
        }

        private static void NonGenericCountDriftFailsAtomically()
        {
            var fixture = Fixture("grid-count-drift-nongeneric");
            RejectAtomically(fixture, new NonGenericDriftCollection(new[] { fixture.Grid.Id }, 1, 2), "Grid renumber target source known Count changed during traversal.");
        }

        private static void NegativePostTraversalCountFailsAtomically()
        {
            var fixture = Fixture("grid-count-negative-post");
            RejectAtomically(fixture, new GenericDriftCollection(new[] { fixture.Grid.Id }, 1, -1), "Grid renumber target source exposes an invalid negative known Count value after traversal.");
        }

        private static void ConflictingPostTraversalCountsFailAtomically()
        {
            var fixture = Fixture("grid-count-conflict-post");
            RejectAtomically(fixture, new ConflictingAfterTraversalCollection(new[] { fixture.Grid.Id }), "Grid renumber target source exposes conflicting known Count values after traversal.");
        }

        private static void PostTraversalCountProjectMutationFailsBeforeGridMutation()
        {
            var fixture = Fixture("grid-count-project-mutation");
            var source = new ProjectMutatingCountCollection(new[] { fixture.Grid.Id }, fixture.Project);
            try
            {
                GridNamingService.Renumber(fixture.Project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.", ex.Message);
                Equal("KEEP", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
                Equal("77", fixture.Grid.Properties[GridNamingService.GridSequenceIndexKey]);
                return;
            }

            throw new Exception("Expected post-traversal Count project mutation to fail before Grid mutation.");
        }

        private static void StableCountedInputSucceeds()
        {
            var fixture = Fixture("grid-count-stable");
            var plan = GridNamingService.Renumber(fixture.Project, new List<string> { fixture.Grid.Id });
            Equal(1, plan.Count);
            Equal("1", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
        }

        private static void PureStreamingInputSucceeds()
        {
            var fixture = Fixture("grid-count-streaming-stability");
            var plan = GridNamingService.Renumber(fixture.Project, new StreamingIds(fixture.Grid.Id));
            Equal(1, plan.Count);
            Equal("1", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
        }

        private static void RejectAtomically(FixtureState fixture, IEnumerable<string> source, string expectedMessage)
        {
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUpdatedUtc = fixture.Project.UpdatedUtc;
            try
            {
                GridNamingService.Renumber(fixture.Project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal(expectedMessage, ex.Message);
                Equal(beforeVersion, fixture.Project.ChangeVersion);
                Equal(beforeUpdatedUtc, fixture.Project.UpdatedUtc);
                Equal("KEEP", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
                Equal("77", fixture.Grid.Properties[GridNamingService.GridSequenceIndexKey]);
                return;
            }

            throw new Exception("Expected unstable Grid renumber Count evidence to fail.");
        }

        private sealed class FixtureState
        {
            public FixtureState(ProjectState project, ProjectElement grid)
            {
                Project = project;
                Grid = grid;
            }

            public ProjectState Project { get; }
            public ProjectElement Grid { get; }
        }

        private static FixtureState Fixture(string id)
        {
            var project = new ProjectState(id, id);
            var grid = new ProjectElement("G-1", ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            grid.SetProperty(GridNamingService.GridLabelKey, "KEEP");
            grid.SetProperty(GridNamingService.GridSequenceIndexKey, "77");
            project.Elements.Add(grid);
            return new FixtureState(project, grid);
        }

        private abstract class DriftEnumerableBase : IEnumerable<string>
        {
            private readonly IReadOnlyList<string> _items;
            protected bool Traversed;

            protected DriftEnumerableBase(IReadOnlyList<string> items)
            {
                _items = items;
            }

            public IEnumerator<string> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Count; i++)
                        yield return _items[i];
                }
                finally
                {
                    Traversed = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GenericDriftCollection : DriftEnumerableBase, ICollection<string>
        {
            private readonly int _before;
            private readonly int _after;

            public GenericDriftCollection(IReadOnlyList<string> items, int before, int after) : base(items)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
            public bool IsReadOnly => true;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection : DriftEnumerableBase, IReadOnlyCollection<string>
        {
            private readonly int _before;
            private readonly int _after;

            public ReadOnlyDriftCollection(IReadOnlyList<string> items, int before, int after) : base(items)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
        }

        private sealed class NonGenericDriftCollection : DriftEnumerableBase, ICollection
        {
            private readonly int _before;
            private readonly int _after;

            public NonGenericDriftCollection(IReadOnlyList<string> items, int before, int after) : base(items)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingAfterTraversalCollection : DriftEnumerableBase, ICollection<string>, IReadOnlyCollection<string>
        {
            public ConflictingAfterTraversalCollection(IReadOnlyList<string> items) : base(items) { }
            int ICollection<string>.Count => 1;
            int IReadOnlyCollection<string>.Count => Traversed ? 2 : 1;
            bool ICollection<string>.IsReadOnly => true;
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => false;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ProjectMutatingCountCollection : DriftEnumerableBase, ICollection<string>
        {
            private readonly ProjectState _project;
            private int _reads;

            public ProjectMutatingCountCollection(IReadOnlyList<string> items, ProjectState project) : base(items)
            {
                _project = project;
            }

            public int Count
            {
                get
                {
                    _reads++;
                    if (_reads > 1)
                        _project.Touch();
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class StreamingIds : IEnumerable<string>
        {
            private readonly string _id;
            public StreamingIds(string id) { _id = id; }
            public IEnumerator<string> GetEnumerator() { yield return _id; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("GridNamingKnownCountStabilitySmoke expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
