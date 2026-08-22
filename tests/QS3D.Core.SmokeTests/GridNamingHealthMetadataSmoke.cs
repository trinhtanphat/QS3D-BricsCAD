using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingHealthMetadataSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LabelWithoutSequenceIsReported();
            MissingPairRemainsUnflagged();
            CompletePairRemainsHealthy();
            SequenceWithoutLabelRemainsReported();
        }

        private static void LabelWithoutSequenceIsReported()
        {
            var grid = Grid("GRID-LABEL-ONLY");
            grid.Properties[GridNamingService.GridLabelKey] = "A";

            var issues = Inspect(grid);
            var issue = Find(issues, "GRID_LABEL_WITHOUT_SEQUENCE", grid.Id);
            Assert(issue != null, "A Grid label without GridSequenceIndex must be reported.");
            Assert(issue!.Severity == HealthSeverity.Error, "Grid label without sequence metadata must fail health diagnostics as an error.");
        }

        private static void MissingPairRemainsUnflagged()
        {
            var grid = Grid("GRID-UNNAMED");
            var issues = Inspect(grid);
            Assert(Find(issues, "GRID_LABEL_WITHOUT_SEQUENCE", grid.Id) == null, "An unnamed Grid with neither naming property must not be flagged as label-only metadata.");
        }

        private static void CompletePairRemainsHealthy()
        {
            var grid = Grid("GRID-COMPLETE");
            grid.Properties[GridNamingService.GridLabelKey] = "B";
            grid.Properties[GridNamingService.GridSequenceIndexKey] = "2";

            var issues = Inspect(grid);
            Assert(Find(issues, "GRID_LABEL_WITHOUT_SEQUENCE", grid.Id) == null, "A complete Grid naming pair must not be flagged as missing sequence metadata.");
            Assert(issues.Count == 0, "A canonical unique Grid naming pair should remain healthy.");
        }

        private static void SequenceWithoutLabelRemainsReported()
        {
            var grid = Grid("GRID-SEQUENCE-ONLY");
            grid.Properties[GridNamingService.GridSequenceIndexKey] = "3";

            var issues = Inspect(grid);
            var issue = Find(issues, "GRID_SEQUENCE_WITHOUT_LABEL", grid.Id);
            Assert(issue != null, "Existing sequence-without-label diagnostics must remain active.");
            Assert(issue!.Severity == HealthSeverity.Warning, "Existing sequence-without-label severity changed unexpectedly.");
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectElement grid)
        {
            var project = new ProjectState("grid-health-smoke", "Grid health smoke");
            project.Elements.Add(grid);
            return new GridNamingHealthService().Inspect(project);
        }

        private static ProjectElement Grid(string id) => new ProjectElement(id, ElementCategory.Grid);

        private static ModelHealthIssue? Find(IReadOnlyList<ModelHealthIssue> issues, string code, string elementId)
        {
            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                if (string.Equals(issue.Code, code, StringComparison.Ordinal) &&
                    string.Equals(issue.ElementId, elementId, StringComparison.Ordinal))
                    return issue;
            }
            return null;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
