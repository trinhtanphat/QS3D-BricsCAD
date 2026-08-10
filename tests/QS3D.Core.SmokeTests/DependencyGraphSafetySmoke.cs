using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphSafetySmoke
    {
        internal static void Run()
        {
            DuplicateRebuildFailsClosedAndPreservesPreviousGraph();
            MarkChangedRejectsDuplicateProjectIdsBeforeMutation();
        }

        private static void DuplicateRebuildFailsClosedAndPreservesPreviousGraph()
        {
            var wall = new ProjectElement("W", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var opening = new ProjectElement("O", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            opening.DependsOn.Add("W");

            var graph = new DependencyGraph();
            graph.Rebuild(new[] { wall, opening });
            var before = graph.GetDependentsTransitive("W");
            if (before.Count != 1 || !string.Equals(before[0], "O", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Expected baseline dependency graph before duplicate rebuild test.");

            var first = new ProjectElement("DUP", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("dup", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var threw = false;
            try
            {
                graph.Rebuild(new[] { first, second });
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("duplicate semantic element id", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!threw) throw new Exception("DependencyGraph.Rebuild must reject duplicate semantic IDs case-insensitively.");
            var after = graph.GetDependentsTransitive("W");
            if (after.Count != 1 || !string.Equals(after[0], "O", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Rejected dependency rebuild must preserve the previous valid graph instead of leaving a partial graph.");
        }

        private static void MarkChangedRejectsDuplicateProjectIdsBeforeMutation()
        {
            var project = new ProjectState("dependency-duplicate", "Dependency Duplicate");
            var first = new ProjectElement("DUP", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("dup", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var threw = false;
            try
            {
                new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>())
                    .MarkChanged(project, "DUP", ElementDirtyFlags.Geometry);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("duplicate semantic element id", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!threw) throw new Exception("Regeneration MarkChanged must fail closed when semantic IDs are ambiguous.");
            if (first.Dirty != ElementDirtyFlags.None || second.Dirty != ElementDirtyFlags.None)
                throw new Exception("Rejected ambiguous MarkChanged mutated semantic dirty state.");
        }
    }

    internal static class DependencyGraphSafetySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphSafetySmoke.Run();
    }
}
