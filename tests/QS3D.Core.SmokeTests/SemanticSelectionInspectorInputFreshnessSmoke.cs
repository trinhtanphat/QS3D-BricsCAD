using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionInspectorInputFreshnessSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("P-SELECTION-FRESHNESS", "Selection freshness smoke");
            project.Elements.Add(new ProjectElement("E-1", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("E-2", ElementCategory.ArchitecturalWall));
            project.Touch();

            var stable = SemanticSelectionInspector.Inspect(project, new[] { "E-2", "E-1" });
            Equal(2, stable.Count, "stable count");
            Equal("E-1", stable.ElementIds[0], "stable first id");
            Equal("E-2", stable.ElementIds[1], "stable second id");

            var before = project.ChangeVersion;
            try
            {
                SemanticSelectionInspector.Inspect(project, MutatingIds(project));
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("changed while materializing semantic selection ids", StringComparison.Ordinal) < 0)
                    throw new Exception("SemanticSelectionInspectorInputFreshnessSmoke: unexpected freshness error: " + ex.Message);
                Equal(before + 1L, project.ChangeVersion, "mutating enumerable version");
                return;
            }

            throw new Exception("SemanticSelectionInspectorInputFreshnessSmoke: re-entrant project mutation was accepted.");
        }

        private static IEnumerable<string> MutatingIds(ProjectState project)
        {
            yield return "E-1";
            project.Touch();
            yield return "E-2";
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("SemanticSelectionInspectorInputFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class SemanticSelectionInspectorInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticSelectionInspectorInputFreshnessSmoke.Run();
    }
}
