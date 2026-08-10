using System;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingSmoke
    {
        public static void Run()
        {
            NumericSequenceIsOrderedAndPadded();
            AlphabeticSequenceCrossesZDeterministically();
            ExistingExternalLabelBlocksWholeBatch();
            NonGridInputBlocksWholeBatch();
            UnrelatedDuplicateIdentityBlocksWholeBatch();
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
