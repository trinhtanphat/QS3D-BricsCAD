using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingHealthSmoke
    {
        public static void Run()
        {
            HealthyGeneratedLabelsRemainClean();
            DuplicateLabelsAreErrorsOnBothOwners();
            InvalidSequenceAndEmptyLabelAreReported();
            ComprehensiveHealthIncludesGridNamingIssues();
        }

        private static void HealthyGeneratedLabelsRemainClean()
        {
            var project = Project();
            var a = Grid(project, "G-A");
            var b = Grid(project, "G-B");
            GridNamingService.Renumber(project, new[] { a.Id, b.Id }, new GridNamingOptions
            {
                Sequence = GridLabelSequence.Alphabetic,
                StartIndex = 1
            });

            var issues = new GridNamingHealthService().Inspect(project);
            Equal(0, issues.Count);
        }

        private static void DuplicateLabelsAreErrorsOnBothOwners()
        {
            var project = Project();
            var a = Grid(project, "G-A");
            var b = Grid(project, "G-B");
            a.SetProperty(GridNamingService.GridLabelKey, "A");
            b.SetProperty(GridNamingService.GridLabelKey, "a");

            var issues = new GridNamingHealthService().Inspect(project)
                .Where(x => x.Code == "GRID_LABEL_DUPLICATE")
                .ToList();
            Equal(2, issues.Count);
            True(issues.All(x => x.Severity == HealthSeverity.Error));
            True(issues.Any(x => x.ElementId == a.Id));
            True(issues.Any(x => x.ElementId == b.Id));
        }

        private static void InvalidSequenceAndEmptyLabelAreReported()
        {
            var project = Project();
            var grid = Grid(project, "G-A");
            grid.SetProperty(GridNamingService.GridLabelKey, "   ");
            grid.SetProperty(GridNamingService.GridSequenceIndexKey, "0");

            var issues = new GridNamingHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "GRID_LABEL_EMPTY" && x.Severity == HealthSeverity.Warning));
            True(issues.Any(x => x.Code == "GRID_SEQUENCE_INVALID" && x.Severity == HealthSeverity.Error));
        }

        private static void ComprehensiveHealthIncludesGridNamingIssues()
        {
            var project = Project();
            var a = Grid(project, "G-A");
            var b = Grid(project, "G-B");
            a.SetProperty(GridNamingService.GridLabelKey, "1");
            b.SetProperty(GridNamingService.GridLabelKey, "1");

            var issues = new ComprehensiveModelHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "GRID_LABEL_DUPLICATE" && x.ElementId == a.Id));
            True(issues.Any(x => x.Code == "GRID_LABEL_DUPLICATE" && x.ElementId == b.Id));
        }

        private static ProjectState Project()
        {
            return new ProjectState("grid-health", "Grid Health");
        }

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
    }
}
