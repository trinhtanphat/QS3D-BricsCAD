using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthMulticoreSmoke
    {
        internal static void Run()
        {
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
            var service = new ComprehensiveModelHealthService();
            var expected = InvokeSequential(service, project, liveSourceHandles);
            if (expected.Count == 0)
                throw new InvalidOperationException("Sequential comprehensive health oracle unexpectedly produced no diagnostics.");
            RequireCode(expected, "HEALTH_PROVIDER_FAILED");
            RequireCode(expected, "ORPHAN_HANDLE");

            for (var iteration = 0; iteration < 32; iteration++)
            {
                var actual = service.Inspect(project, liveSourceHandles, null);
                AssertEquivalent(expected, actual, iteration);
            }
        }

        private static IReadOnlyList<ModelHealthIssue> InvokeSequential(
            ComprehensiveModelHealthService service,
            ProjectState project,
            ISet<string> liveSourceHandles)
        {
            var method = typeof(ComprehensiveModelHealthService).GetMethod(
                "InspectSequential",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Comprehensive health sequential parity oracle is unavailable.");

            try
            {
                var result = method.Invoke(service, new object?[] { project, liveSourceHandles, null });
                if (result is IReadOnlyList<ModelHealthIssue> issues) return issues;
                throw new InvalidOperationException("Comprehensive health sequential parity oracle returned an unexpected result.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
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
