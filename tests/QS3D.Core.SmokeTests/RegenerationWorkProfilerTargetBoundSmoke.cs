using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfilerTargetBoundSmoke
    {
        internal static void Run()
        {
            StopsAtProjectCardinality();
            ExactProjectCardinalityRemainsAccepted();
            DuplicateValidationKeepsPrecedence();
        }

        private static void StopsAtProjectCardinality()
        {
            var project = ProjectWithElements("E1", "E2");
            var profiler = new RegenerationWorkProfiler();
            var error = Throws<ArgumentException>(() => profiler.ProfileSubset(project, ThreeThenSentinel()));
            Contains(error.Message, "cannot exceed project element count of 2");
        }

        private static void ExactProjectCardinalityRemainsAccepted()
        {
            var project = ProjectWithElements("E1", "E2");
            var profile = new RegenerationWorkProfiler().ProfileSubset(project, new[] { "E2", "E1" });
            Equal(RegenerationWorkScope.Subset, profile.Scope);
            Equal(2, profile.TargetElementIds.Count);
            Equal("E1", profile.TargetElementIds[0]);
            Equal("E2", profile.TargetElementIds[1]);
            Equal(2, profile.ProjectElementCount);
        }

        private static void DuplicateValidationKeepsPrecedence()
        {
            var project = ProjectWithElements("E1");
            var profiler = new RegenerationWorkProfiler();
            var error = Throws<ArgumentException>(() => profiler.ProfileSubset(project, new[] { "E1", "E1" }));
            Contains(error.Message, "Duplicate regeneration target id: E1");
        }

        private static ProjectState ProjectWithElements(params string[] ids)
        {
            var project = new ProjectState("regen-profile-bound", "Regeneration Profile Bound");
            foreach (var id in ids)
            {
                var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }
            return project;
        }

        private static IEnumerable<string> ThreeThenSentinel()
        {
            yield return "E1";
            yield return "E2";
            yield return "E3";
            throw new InvalidOperationException("Profile target enumeration continued beyond the project cardinality bound.");
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RegenerationWorkProfilerTargetBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationWorkProfilerTargetBoundSmoke.Run();
    }
}
