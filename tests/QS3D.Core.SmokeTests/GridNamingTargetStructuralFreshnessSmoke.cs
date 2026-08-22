using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingTargetStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableTargetStillRenumbers();
            SameIdReplacementFailsBeforeMutation();
            UnrelatedReplacementDoesNotRetargetGrid();
        }

        private static void StableTargetStillRenumbers()
        {
            var project = new ProjectState("P-GRID-STRUCT-1", "Stable Grid target");
            var grid = new ProjectElement("GRID-STABLE", ElementCategory.Grid);
            project.Elements.Add(grid);
            var beforeVersion = project.ChangeVersion;

            var assignments = GridNamingService.Renumber(project, Yield(grid.Id));

            Equal(1, assignments.Count);
            Equal(grid.Id, assignments[0].ElementId);
            Equal("1", grid.Properties[GridNamingService.GridLabelKey]);
            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static void SameIdReplacementFailsBeforeMutation()
        {
            var project = new ProjectState("P-GRID-STRUCT-2", "Replaced Grid target");
            var original = new ProjectElement("GRID-REPLACED", ElementCategory.Grid);
            var replacement = new ProjectElement(original.Id, ElementCategory.Grid);
            project.Elements.Add(original);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => GridNamingService.Renumber(project, ReplaceThenYield(project, replacement, 0)),
                "Grid renumber target changed while Grid IDs were being enumerated");

            Equal(beforeVersion, project.ChangeVersion);
            False(original.Properties.ContainsKey(GridNamingService.GridLabelKey));
            False(original.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
            False(replacement.Properties.ContainsKey(GridNamingService.GridLabelKey));
            False(replacement.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
        }

        private static void UnrelatedReplacementDoesNotRetargetGrid()
        {
            var project = new ProjectState("P-GRID-STRUCT-3", "Unrelated structural change");
            var grid = new ProjectElement("GRID-TARGET", ElementCategory.Grid);
            var unrelated = new ProjectElement("OTHER", ElementCategory.CustomQuantity);
            var unrelatedReplacement = new ProjectElement(unrelated.Id, ElementCategory.CustomQuantity);
            project.Elements.Add(grid);
            project.Elements.Add(unrelated);

            var assignments = GridNamingService.Renumber(
                project,
                ReplaceThenYield(project, unrelatedReplacement, 1, grid.Id));

            Equal(1, assignments.Count);
            Equal(grid.Id, assignments[0].ElementId);
            Equal("1", grid.Properties[GridNamingService.GridLabelKey]);
            False(unrelatedReplacement.Properties.ContainsKey(GridNamingService.GridLabelKey));
        }

        private static IEnumerable<string> Yield(string id)
        {
            yield return id;
        }

        private static IEnumerable<string> ReplaceThenYield(
            ProjectState project,
            ProjectElement replacement,
            int index,
            string? yieldedId = null)
        {
            project.Elements[index] = replacement;
            yield return yieldedId ?? replacement.Id;
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException(
                    "Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
