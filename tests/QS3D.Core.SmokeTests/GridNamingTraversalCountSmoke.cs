using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingTraversalCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountUnderEnumerationRejectsAtomically();
            KnownCountOverEnumerationRejectsAtomically();
            HonestCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void KnownCountUnderEnumerationRejectsAtomically()
        {
            var project = Project("grid-count-under");
            var grid = Grid(project, "G-A", "KEEP-A", "9");
            var source = new DishonestCountCollection(2, new[] { grid.Id });
            RejectMismatchAtomically(project, source, grid, "KEEP-A", "9");
        }

        private static void KnownCountOverEnumerationRejectsAtomically()
        {
            var project = Project("grid-count-over");
            var first = Grid(project, "G-A", "KEEP-A", "9");
            var second = Grid(project, "G-B", "KEEP-B", "10");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            try
            {
                GridNamingService.Renumber(project, new DishonestCountCollection(1, new[] { first.Id, second.Id }));
            }
            catch (InvalidOperationException ex)
            {
                Equal("Grid renumber target source known Count was exceeded during traversal.", ex.Message);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(beforeUpdatedUtc, project.UpdatedUtc);
                Equal("KEEP-A", first.Properties[GridNamingService.GridLabelKey]);
                Equal("9", first.Properties[GridNamingService.GridSequenceIndexKey]);
                Equal("KEEP-B", second.Properties[GridNamingService.GridLabelKey]);
                Equal("10", second.Properties[GridNamingService.GridSequenceIndexKey]);
                return;
            }

            throw new Exception("Expected Grid renumber over-enumeration mismatch to fail.");
        }

        private static void HonestCountedSourceRemainsAccepted()
        {
            var project = Project("grid-count-honest");
            var first = Grid(project, "G-A");
            var second = Grid(project, "G-B");
            var plan = GridNamingService.Renumber(project, new List<string> { first.Id, second.Id });
            Equal(2, plan.Count);
            Equal("1", first.Properties[GridNamingService.GridLabelKey]);
            Equal("2", second.Properties[GridNamingService.GridLabelKey]);
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var project = Project("grid-count-streaming");
            var grid = Grid(project, "G-A");
            var plan = GridNamingService.Renumber(project, new StreamingIds(grid.Id));
            Equal(1, plan.Count);
            Equal("1", grid.Properties[GridNamingService.GridLabelKey]);
        }

        private static void RejectMismatchAtomically(
            ProjectState project,
            IEnumerable<string> source,
            ProjectElement grid,
            string expectedLabel,
            string expectedSequence)
        {
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Grid renumber target source Count does not match the enumerated element count.", ex.Message);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(beforeUpdatedUtc, project.UpdatedUtc);
                Equal(expectedLabel, grid.Properties[GridNamingService.GridLabelKey]);
                Equal(expectedSequence, grid.Properties[GridNamingService.GridSequenceIndexKey]);
                return;
            }

            throw new Exception("Expected Grid renumber under-enumeration mismatch to fail.");
        }

        private sealed class DishonestCountCollection : ICollection<string>
        {
            private readonly IReadOnlyList<string> _items;

            public DishonestCountCollection(int advertisedCount, IReadOnlyList<string> items)
            {
                Count = advertisedCount;
                _items = items;
            }

            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _items.Count; index++)
                    yield return _items[index];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class StreamingIds : IEnumerable<string>
        {
            private readonly string _id;

            public StreamingIds(string id)
            {
                _id = id;
            }

            public IEnumerator<string> GetEnumerator()
            {
                yield return _id;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static ProjectState Project(string id) => new ProjectState(id, id);

        private static ProjectElement Grid(
            ProjectState project,
            string id,
            string? label = null,
            string? sequence = null)
        {
            var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            if (label != null) element.SetProperty(GridNamingService.GridLabelKey, label);
            if (sequence != null) element.SetProperty(GridNamingService.GridSequenceIndexKey, sequence);
            project.Elements.Add(element);
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("GridNamingTraversalCountSmoke expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
