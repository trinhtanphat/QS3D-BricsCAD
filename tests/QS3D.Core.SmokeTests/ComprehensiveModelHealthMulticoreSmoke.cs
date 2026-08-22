using System;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthMulticoreSmoke
    {
        internal static void Run()
        {
            ThrowsArgumentOutOfRange(0);
            ThrowsArgumentOutOfRange(5);

            var project = NewProject();
            project.Elements.Add(null!);

            var opening = new ProjectElement("opening-1", ElementCategory.WallOpening, string.Empty, "floor-0", "zone-1");
            opening.SourceHandles.Add("AB12");
            project.Elements.Add(opening);

            var grid = new ProjectElement("grid-1", ElementCategory.Grid, string.Empty, "floor-0", "zone-1");
            grid.Properties["GridLabel"] = " A ";
            grid.Properties["GridSequenceIndex"] = "01";
            grid.Properties["GeneratedGridAnnotationHandles"] = "A;A";
            project.Elements.Add(grid);

            var curtain = new ProjectElement("curtain-1", ElementCategory.GlassWall, string.Empty, "floor-0", "zone-1");
            curtain.Properties["GeneratedCurtainFrameHandles"] = "B;B";
            curtain.Properties["GeneratedCurtainFrameCount"] = "2";
            curtain.Properties["GeneratedCurtainFrameColumns"] = "1";
            curtain.Properties["GeneratedCurtainFrameRows"] = "1";
            curtain.Properties["GeneratedRebarHandles"] = "C;C";
            curtain.Properties["GeneratedRebarCount"] = "2";
            project.Elements.Add(curtain);

            var liveSourceHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var singleWorker = new ComprehensiveModelHealthService(1);
            var multiWorker = new ComprehensiveModelHealthService(4);
            var expected = singleWorker.Inspect(project, liveSourceHandles, null);
            if (expected.Count == 0)
                throw new InvalidOperationException("Single-worker comprehensive health oracle unexpectedly produced no diagnostics.");
            RequireCode(expected, "HEALTH_PROVIDER_FAILED");
            RequireCode(expected, "ORPHAN_HANDLE");

            for (var iteration = 0; iteration < 32; iteration++)
            {
                var actual = multiWorker.Inspect(project, liveSourceHandles, null);
                AssertEquivalent(expected, actual, iteration);
            }
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Multicore diagnostics");
            project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Floor 0", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";
            return project;
        }

        private static void ThrowsArgumentOutOfRange(int maxDegreeOfParallelism)
        {
            try
            {
                _ = new ComprehensiveModelHealthService(maxDegreeOfParallelism);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            throw new InvalidOperationException("Expected bounded comprehensive health parallelism validation failure.");
        }

        private static void RequireCode(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new InvalidOperationException("Expected comprehensive health diagnostic code was not produced: " + code + ".");
        }

        private static void AssertEquivalent(
            IReadOnlyList<ModelHealthIssue> expected,
            IReadOnlyList<ModelHealthIssue> actual,
            int iteration)
        {
            if (expected.Count != actual.Count)
                throw new InvalidOperationException(
                    "Comprehensive health multicore parity count mismatch at iteration " + iteration +
                    ": expected " + expected.Count + ", actual " + actual.Count + ".");

            for (var index = 0; index < expected.Count; index++)
            {
                var left = expected[index];
                var right = actual[index];
                if (string.Equals(left.Code, right.Code, StringComparison.Ordinal) &&
                    left.Severity == right.Severity &&
                    string.Equals(left.Message, right.Message, StringComparison.Ordinal) &&
                    string.Equals(left.ElementId, right.ElementId, StringComparison.Ordinal))
                    continue;

                throw new InvalidOperationException(
                    "Comprehensive health multicore parity mismatch at iteration " + iteration +
                    ", issue " + index + ". Expected " + Describe(left) + ", actual " + Describe(right) + ".");
            }
        }

        private static string Describe(ModelHealthIssue issue) =>
            "[" + issue.Code + "," + issue.Severity + "," + issue.ElementId + "] " + issue.Message;
    }
}
