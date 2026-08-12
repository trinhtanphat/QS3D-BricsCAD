using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationSubsetTargetBoundSmoke
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
            var engine = Engine();
            var error = Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, ThreeThenSentinel()));
            Contains(error.Message, "cannot exceed project element count of 2");
        }

        private static void ExactProjectCardinalityRemainsAccepted()
        {
            var project = ProjectWithElements("E1", "E2");
            var engine = Engine();
            var regenerated = engine.RegenerateDirtySubset(project, new[] { "E2", "E1" });
            Equal(0, regenerated);
        }

        private static void DuplicateValidationKeepsPrecedence()
        {
            var project = ProjectWithElements("E1");
            var engine = Engine();
            var error = Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { "E1", "E1" }));
            Contains(error.Message, "Duplicate regeneration target id: E1");
        }

        private static ProjectState ProjectWithElements(params string[] ids)
        {
            var project = new ProjectState("regen-subset-bound", "Regeneration Subset Bound");
            foreach (var id in ids)
            {
                var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }
            return project;
        }

        private static RegenerationEngine Engine() =>
            new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>());

        private static IEnumerable<string> ThreeThenSentinel()
        {
            yield return "E1";
            yield return "E2";
            yield return "E3";
            throw new InvalidOperationException("Target enumeration continued beyond the project cardinality bound.");
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

    internal static class RegenerationSubsetTargetBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationSubsetTargetBoundSmoke.Run();
    }
}
