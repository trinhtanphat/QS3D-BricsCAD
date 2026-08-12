using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyInputRenumbers();
            MutatingLazyInputFailsBeforeNamingMutation();
            MutatingEmptyInputFailsBeforeEmptyValidation();
        }

        private static void StableLazyInputRenumbers()
        {
            var project = new ProjectState("P-GRID-FRESH-1", "Stable Grid naming input");
            var grid = new ProjectElement("GRID-1", ElementCategory.Grid);
            project.Elements.Add(grid);

            var assignments = GridNamingService.Renumber(project, LazyId(grid.Id));

            Equal(1, assignments.Count);
            Equal(grid.Id, assignments[0].ElementId);
            Equal("1", assignments[0].Label);
            Equal("1", grid.Properties[GridNamingService.GridLabelKey]);
            Equal("1", grid.Properties[GridNamingService.GridSequenceIndexKey]);
        }

        private static void MutatingLazyInputFailsBeforeNamingMutation()
        {
            var project = new ProjectState("P-GRID-FRESH-2", "Mutating Grid naming input");
            var grid = new ProjectElement("GRID-STALE", ElementCategory.Grid);
            project.Elements.Add(grid);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => GridNamingService.Renumber(project, TouchThenYield(project, grid.Id)),
                "Project changed while Grid renumber targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            False(grid.Properties.ContainsKey(GridNamingService.GridLabelKey));
            False(grid.Properties.ContainsKey(GridNamingService.GridSequenceIndexKey));
        }

        private static void MutatingEmptyInputFailsBeforeEmptyValidation()
        {
            var project = new ProjectState("P-GRID-FRESH-3", "Mutating empty Grid naming input");
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => GridNamingService.Renumber(project, TouchThenStop(project)),
                "Project changed while Grid renumber targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static IEnumerable<string> LazyId(string id)
        {
            yield return id;
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project, string id)
        {
            project.Touch();
            yield return id;
        }

        private static IEnumerable<string> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
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
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
