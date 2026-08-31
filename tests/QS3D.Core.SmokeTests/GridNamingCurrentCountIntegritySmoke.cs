using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingCurrentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentInducedCountDriftWinsBeforeValueValidation();
            StableCountedCurrentSucceeds();
        }

        private static void CurrentInducedCountDriftWinsBeforeValueValidation()
        {
            var fixture = Fixture("grid-current-count-drift");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUpdatedUtc = fixture.Project.UpdatedUtc;
            var source = new CurrentDriftCollection(null!);

            try
            {
                GridNamingService.Renumber(fixture.Project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Grid renumber target source known Count changed during traversal.", ex.Message);
                Equal(beforeVersion, fixture.Project.ChangeVersion);
                Equal(beforeUpdatedUtc, fixture.Project.UpdatedUtc);
                Equal("KEEP", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
                Equal("77", fixture.Grid.Properties[GridNamingService.GridSequenceIndexKey]);
                Equal(1, source.CurrentReads);
                return;
            }

            throw new Exception("Expected Current-induced Grid renumber Count drift to fail before value validation/staging.");
        }

        private static void StableCountedCurrentSucceeds()
        {
            var fixture = Fixture("grid-current-count-stable");
            var source = new CurrentDriftCollection(fixture.Grid.Id, driftOnCurrent: false);
            var plan = GridNamingService.Renumber(fixture.Project, source);

            Equal(1, plan.Count);
            Equal(fixture.Grid.Id, plan[0].ElementId);
            Equal("1", fixture.Grid.Properties[GridNamingService.GridLabelKey]);
            Equal(1, source.CurrentReads);
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

        private sealed class CurrentDriftCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly bool _driftOnCurrent;
            private int _count = 1;

            public CurrentDriftCollection(string value, bool driftOnCurrent = true)
            {
                _value = value;
                _driftOnCurrent = driftOnCurrent;
            }

            public int Count => _count;
            public int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly CurrentDriftCollection _owner;
                private bool _moved;

                public Enumerator(CurrentDriftCollection owner)
                {
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        if (!_moved) throw new InvalidOperationException("Enumerator is not positioned.");
                        _owner.CurrentReads++;
                        if (_owner._driftOnCurrent)
                            _owner._count = 2;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("GridNamingCurrentCountIntegritySmoke expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
