using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthBaselineSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NewResolvedAndPersistentIssuesAreClassified();
            DuplicateIssuesAreStable();
            CrossProjectDiffFailsClosed();
            SemanticCaptureIsReadOnly();
        }

        private static void NewResolvedAndPersistentIssuesAreClassified()
        {
            var project = Project("P1");
            var service = new ModelHealthBaselineService();
            var before = service.Capture(project, new[]
            {
                new ModelHealthIssue("OLD_ERROR", HealthSeverity.Error, "old", "E1"),
                new ModelHealthIssue("KEEP", HealthSeverity.Warning, "same", "E1")
            });
            var after = service.Capture(project, new[]
            {
                new ModelHealthIssue("KEEP", HealthSeverity.Warning, "same", "E1"),
                new ModelHealthIssue("NEW_ERROR", HealthSeverity.Error, "new", "E2")
            });

            var diff = service.Compare(before, after);
            Equal(1, diff.NewIssues.Count);
            Equal("NEW_ERROR", diff.NewIssues[0].Code);
            Equal(1, diff.ResolvedIssues.Count);
            Equal("OLD_ERROR", diff.ResolvedIssues[0].Code);
            Equal(1, diff.PersistentIssues.Count);
            True(diff.HasRegressions);
            True(diff.HasImprovements);
            Equal(1, diff.NewErrorCount);
            Equal(1, diff.ResolvedErrorCount);
        }

        private static void DuplicateIssuesAreStable()
        {
            var project = Project("P2");
            var service = new ModelHealthBaselineService();
            var baseline = service.Capture(project, new[]
            {
                new ModelHealthIssue("A", HealthSeverity.Warning, "same", "E1"),
                new ModelHealthIssue("a", HealthSeverity.Warning, "same", "e1"),
                new ModelHealthIssue("B", HealthSeverity.Info, "info", "E2")
            });
            Equal(2, baseline.Issues.Count);
            Equal(1, baseline.WarningCount);
            Equal(1, baseline.InfoCount);
        }

        private static void CrossProjectDiffFailsClosed()
        {
            var service = new ModelHealthBaselineService();
            var left = service.Capture(Project("A"), Array.Empty<ModelHealthIssue>());
            var right = service.Capture(Project("B"), Array.Empty<ModelHealthIssue>());
            Throws<InvalidOperationException>(() => service.Compare(left, right));
        }

        private static void SemanticCaptureIsReadOnly()
        {
            var project = Project("P3");
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, "F", "Z");
            project.Elements.Add(element);
            var updated = project.UpdatedUtc;
            var dirty = element.Dirty;

            var baseline = new ModelHealthBaselineService().CaptureSemantic(project);
            True(baseline.Issues.Any());
            Equal(updated, project.UpdatedUtc);
            Equal(dirty, element.Dirty);
        }

        private static ProjectState Project(string id)
        {
            var project = new ProjectState(id, "Health Baseline");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
