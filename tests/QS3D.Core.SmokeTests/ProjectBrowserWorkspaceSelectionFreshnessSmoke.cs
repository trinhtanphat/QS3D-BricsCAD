using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceSelectionFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SemanticMutationDuringSelectionEnumerationFailsFreshness();
            StructuralReplacementDuringSelectionEnumerationFailsFreshness();
            StableSelectionRemainsPresentationOnly();
        }

        private static void SemanticMutationDuringSelectionEnumerationFailsFreshness()
        {
            var project = BuildProject(out _);
            var state = State();
            var beforeVersion = project.ChangeVersion;

            IEnumerable<string> Selection()
            {
                project.Touch();
                yield return "B-001";
            }

            ThrowsContaining(
                () => ProjectBrowserWorkspaceCoordinator.ApplySelection(project, state, Selection(), "B-001"),
                "Project changed while Project Browser selection ids were being enumerated");
            Equal(beforeVersion + 1, project.ChangeVersion, "caller mutation revision");
        }

        private static void StructuralReplacementDuringSelectionEnumerationFailsFreshness()
        {
            var project = BuildProject(out var original);
            var state = State();
            var beforeVersion = project.ChangeVersion;
            var index = project.Elements.IndexOf(original);

            IEnumerable<string> Selection()
            {
                project.Elements[index] = Beam("B-001");
                yield return "B-001";
            }

            ThrowsContaining(
                () => ProjectBrowserWorkspaceCoordinator.ApplySelection(project, state, Selection(), "B-001"),
                "Project element structure changed while Project Browser selection ids were being enumerated");
            Equal(beforeVersion, project.ChangeVersion, "direct replacement revision");
            if (ReferenceEquals(project.Elements[index], original))
                throw new InvalidOperationException("ProjectBrowserWorkspaceSelectionFreshnessSmoke replacement fixture did not change element ownership.");
        }

        private static void StableSelectionRemainsPresentationOnly()
        {
            var project = BuildProject(out _);
            var state = State();
            var beforeVersion = project.ChangeVersion;

            var updated = ProjectBrowserWorkspaceCoordinator.ApplySelection(
                project,
                state,
                new[] { "B-001" },
                "B-001");

            Equal(beforeVersion, project.ChangeVersion, "stable presentation-only revision");
            Equal(1, updated.SelectedElementIds.Count, "stable selected count");
            Equal("B-001", updated.SelectedElementIds[0], "stable selected id");
            Equal("B-001", updated.PrimaryElementId, "stable primary id");
        }

        private static ProjectState BuildProject(out ProjectElement selected)
        {
            var project = new ProjectState("P-BROWSER-SELECTION-FRESHNESS", "Browser selection freshness");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            selected = Beam("B-001");
            project.Elements.Add(selected);
            project.Elements.Add(Beam("B-002"));
            return project;
        }

        private static ProjectElement Beam(string id) =>
            new ProjectElement(id, ElementCategory.Beam, string.Empty, "F-02", "Z-A");

        private static ProjectBrowserWorkspaceState State() =>
            new ProjectBrowserWorkspaceState(ProjectBrowserGrouping.FloorThenCategory);

        private static void ThrowsContaining(Action action, string expected)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected Project Browser selection freshness error.", ex);
            }

            throw new InvalidOperationException("Expected Project Browser selection freshness rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
