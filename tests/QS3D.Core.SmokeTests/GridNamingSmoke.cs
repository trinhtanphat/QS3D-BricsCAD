using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingSmoke
    {
        private const int MaxGridBatch = 2000;

        public static void Run()
        {
            NumericSequenceIsOrderedAndPadded();
            AlphabeticSequenceCrossesZDeterministically();
            ExistingExternalLabelBlocksWholeBatch();
            NonGridInputBlocksWholeBatch();
            UnrelatedDuplicateIdentityBlocksWholeBatch();
            OversizedCountedSourcesRejectBeforeEnumeration();
            ConflictingKnownCountContractsRejectBeforeEnumeration();
            NegativeKnownCountContractsRejectBeforeEnumeration();
            CountSideEffectRejectsBeforeEnumeration();
            ExactCapacityCountedSourceRemainsAccepted();
            GridNamingBoundedEnumerationSmoke.Run();
        }

        private static void NumericSequenceIsOrderedAndPadded()
        {
            var project = Project();
            var a = Grid(project, "G-A");
            var b = Grid(project, "G-B");
            var plan = GridNamingService.Renumber(project, new[] { b.Id, a.Id }, new GridNamingOptions
            {
                Sequence = GridLabelSequence.Numeric,
                Prefix = "X-",
                StartIndex = 3,
                NumericPadding = 2
            });

            Equal(2, plan.Count);
            Equal("X-03", b.Properties[GridNamingService.GridLabelKey]);
            Equal("3", b.Properties[GridNamingService.GridSequenceIndexKey]);
            Equal("X-04", a.Properties[GridNamingService.GridLabelKey]);
            Equal("4", a.Properties[GridNamingService.GridSequenceIndexKey]);
        }

        private static void AlphabeticSequenceCrossesZDeterministically()
        {
            var project = Project();
            var a = Grid(project, "G-1");
            var b = Grid(project, "G-2");
            var c = Grid(project, "G-3");
            var plan = GridNamingService.Renumber(project, new[] { a.Id, b.Id, c.Id }, new GridNamingOptions
            {
                Sequence = GridLabelSequence.Alphabetic,
                Prefix = "A-",
                Suffix = "-REF",
                StartIndex = 25
            });

            Equal("A-Y-REF", plan[0].Label);
            Equal("A-Z-REF", plan[1].Label);
            Equal("A-AA-REF", plan[2].Label);
        }

        private static void ExistingExternalLabelBlocksWholeBatch()
        {
            var project = Project();
            var external = Grid(project, "G-X");
            external.SetProperty(GridNamingService.GridLabelKey, "2");
            var a = Grid(project, "G-A");
            var b = Grid(project, "G-B");
            a.SetProperty(GridNamingService.GridLabelKey, "OLD-A");
            b.SetProperty(GridNamingService.GridLabelKey, "OLD-B");

            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, new[] { a.Id, b.Id }, new GridNamingOptions
            {
                StartIndex = 1
            }));

            Equal("OLD-A", a.Properties[GridNamingService.GridLabelKey]);
            Equal("OLD-B", b.Properties[GridNamingService.GridLabelKey]);
            True(!a.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
            True(!b.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
        }

        private static void NonGridInputBlocksWholeBatch()
        {
            var project = Project();
            var grid = Grid(project, "G-A");
            grid.SetProperty(GridNamingService.GridLabelKey, "OLD");
            var wall = new ProjectElement("W-A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(wall);

            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, new[] { grid.Id, wall.Id }, new GridNamingOptions()));
            Equal("OLD", grid.Properties[GridNamingService.GridLabelKey]);
            True(!grid.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
        }

        private static void UnrelatedDuplicateIdentityBlocksWholeBatch()
        {
            var project = Project();
            var grid = Grid(project, "G-A");
            grid.SetProperty(GridNamingService.GridLabelKey, "OLD");
            project.Elements.Add(new ProjectElement("DUP", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("dup", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, new[] { grid.Id }, new GridNamingOptions()));
            Equal("OLD", grid.Properties[GridNamingService.GridLabelKey]);
            True(!grid.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
        }

        private static void OversizedCountedSourcesRejectBeforeEnumeration()
        {
            var generic = new OversizedGenericCollection();
            CapacityRejectedBeforeEnumeration(generic, () => generic.EnumeratorRequested);

            var readOnly = new OversizedReadOnlyCollection();
            CapacityRejectedBeforeEnumeration(readOnly, () => readOnly.EnumeratorRequested);

            var nonGeneric = new OversizedNonGenericCollection();
            CapacityRejectedBeforeEnumeration(nonGeneric, () => nonGeneric.EnumeratorRequested);
        }

        private static void ConflictingKnownCountContractsRejectBeforeEnumeration()
        {
            var project = Project();
            var source = new ConflictingKnownCountCollection();
            var beforeVersion = project.ChangeVersion;
            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("A Grid renumber batch supports at most 2000 elements.", ex.Message);
                True(source.GenericCountRead);
                True(source.ReadOnlyCountRead);
                True(source.NonGenericCountRead);
                True(!source.EnumeratorRequested);
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected conflicting known-count Grid source to fail before enumeration.");
        }

        private static void NegativeKnownCountContractsRejectBeforeEnumeration()
        {
            var generic = new NegativeGenericCollection();
            NegativeKnownCountRejectedBeforeEnumeration(generic, () => generic.EnumeratorRequested);

            var readOnly = new NegativeReadOnlyCollection();
            NegativeKnownCountRejectedBeforeEnumeration(readOnly, () => readOnly.EnumeratorRequested);

            var nonGeneric = new NegativeNonGenericCollection();
            NegativeKnownCountRejectedBeforeEnumeration(nonGeneric, () => nonGeneric.EnumeratorRequested);
        }

        private static void NegativeKnownCountRejectedBeforeEnumeration(IEnumerable<string> source, Func<bool> enumeratorRequested)
        {
            var project = Project();
            var grid = Grid(project, "G-NEG");
            grid.SetProperty(GridNamingService.GridLabelKey, "KEEP");
            grid.SetProperty(GridNamingService.GridSequenceIndexKey, "9");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Grid renumber target source exposes an invalid negative known count.", ex.Message);
                True(!enumeratorRequested());
                Equal(beforeVersion, project.ChangeVersion);
                Equal(beforeUpdatedUtc, project.UpdatedUtc);
                Equal("KEEP", grid.Properties[GridNamingService.GridLabelKey]);
                Equal("9", grid.Properties[GridNamingService.GridSequenceIndexKey]);
                return;
            }

            throw new Exception("Expected negative known-count Grid source to fail before enumeration.");
        }

        private static void CapacityRejectedBeforeEnumeration(IEnumerable<string> source, Func<bool> enumeratorRequested)
        {
            var project = Project();
            var beforeVersion = project.ChangeVersion;
            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("A Grid renumber batch supports at most 2000 elements.", ex.Message);
                True(!enumeratorRequested());
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected counted Grid renumber capacity rejection.");
        }

        private static void CountSideEffectRejectsBeforeEnumeration()
        {
            var project = Project();
            var source = new VersionMutatingCountCollection(project);
            var beforeVersion = project.ChangeVersion;
            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.", ex.Message);
                True(source.CountRead);
                True(!source.EnumeratorRequested);
                Equal(beforeVersion + 1, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected Grid renumber version-drift rejection after Count access.");
        }

        private static void ExactCapacityCountedSourceRemainsAccepted()
        {
            var project = Project();
            var ids = new List<string>(MaxGridBatch);
            for (var index = 1; index <= MaxGridBatch; index++)
            {
                var id = "G-LIMIT-" + index;
                Grid(project, id);
                ids.Add(id);
            }

            var beforeVersion = project.ChangeVersion;
            var plan = GridNamingService.Renumber(project, ids);
            Equal(MaxGridBatch, plan.Count);
            Equal(beforeVersion + 1, project.ChangeVersion);
            Equal("1", plan[0].Label);
            Equal(MaxGridBatch.ToString(), plan[MaxGridBatch - 1].Label);
        }

        private sealed class OversizedGenericCollection : ICollection<string>
        {
            public int Count => MaxGridBatch + 1;
            public bool IsReadOnly => true;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Oversized generic Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class OversizedReadOnlyCollection : IReadOnlyCollection<string>
        {
            public int Count => MaxGridBatch + 1;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Oversized read-only Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class OversizedNonGenericCollection : IEnumerable<string>, ICollection
        {
            public int Count => MaxGridBatch + 1;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Oversized non-generic Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class NegativeGenericCollection : ICollection<string>
        {
            public int Count => -1;
            public bool IsReadOnly => true;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Negative-count generic Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class NegativeReadOnlyCollection : IReadOnlyCollection<string>
        {
            public int Count => -1;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Negative-count read-only Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeNonGenericCollection : IEnumerable<string>, ICollection
        {
            public int Count => -1;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Negative-count non-generic Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingKnownCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            public int Count
            {
                get
                {
                    GenericCountRead = true;
                    return 1;
                }
            }

            int IReadOnlyCollection<string>.Count
            {
                get
                {
                    ReadOnlyCountRead = true;
                    return 2;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountRead = true;
                    return MaxGridBatch + 1;
                }
            }

            public bool GenericCountRead { get; private set; }
            public bool ReadOnlyCountRead { get; private set; }
            public bool NonGenericCountRead { get; private set; }
            public bool EnumeratorRequested { get; private set; }
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Conflicting known-count Grid source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class VersionMutatingCountCollection : ICollection<string>
        {
            private readonly ProjectState _project;

            public VersionMutatingCountCollection(ProjectState project)
            {
                _project = project;
            }

            public int Count
            {
                get
                {
                    CountRead = true;
                    _project.Touch();
                    return 1;
                }
            }

            public bool CountRead { get; private set; }
            public bool EnumeratorRequested { get; private set; }
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Version-mutating Count source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private static ProjectState Project() => new ProjectState("grid-naming", "Grid Naming");

        private static ProjectElement Grid(ProjectState project, string id)
        {
            var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
