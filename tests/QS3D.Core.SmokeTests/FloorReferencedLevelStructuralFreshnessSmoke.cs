using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorReferencedLevelStructuralFreshnessSmoke
    {
        internal static void Run()
        {
            StableLazyBottomAssignmentStillWorks();
            ReplacedTopLevelFailsBottomAssignmentClosed();
            ReplacedBottomLevelFailsTopAssignmentClosed();
            RemovedTopLevelFailsBottomAssignmentClosed();
        }

        private static void StableLazyBottomAssignmentStillWorks()
        {
            var project = BuildBottomAssignmentProject(3d, out var element, out _, out _);
            var changed = ProjectFloorService.AssignBottomLevel(project, "B", StableTargets(element));
            if (changed != 1 ||
                !element.Properties.TryGetValue(ProjectFloorService.BottomLevelIdKey, out var bottomId) ||
                !string.Equals(bottomId, "B", StringComparison.Ordinal) ||
                !element.Properties.TryGetValue(ProjectFloorService.BottomLevelOffsetKey, out var offset) ||
                !string.Equals(offset, "0", StringComparison.Ordinal))
                throw new InvalidOperationException("Stable lazy Bottom Level assignment changed unexpectedly.");
        }

        private static void ReplacedTopLevelFailsBottomAssignmentClosed()
        {
            var project = BuildBottomAssignmentProject(-1d, out var element, out _, out var top);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                ProjectFloorService.AssignBottomLevel(project, "B", ReplaceFloorThenYield(project, top, 3d, element)));
            if (project.ChangeVersion != version + 1L)
                throw new InvalidOperationException("Direct Top Level replacement did not advance ProjectState.ChangeVersion exactly once.");
            AssertNoBottomMutation(element);
        }

        private static void ReplacedBottomLevelFailsTopAssignmentClosed()
        {
            var project = BuildTopAssignmentProject(4d, out var element, out var bottom, out _);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                ProjectFloorService.AssignTopLevel(project, "T", ReplaceFloorThenYield(project, bottom, 0d, element)));
            if (project.ChangeVersion != version + 1L)
                throw new InvalidOperationException("Direct Bottom Level replacement did not advance ProjectState.ChangeVersion exactly once.");
            if (element.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey) ||
                element.Properties.ContainsKey(ProjectFloorService.TopLevelOffsetKey))
                throw new InvalidOperationException("Top Level assignment mutated the element before rejecting Bottom Level replacement.");
        }

        private static void RemovedTopLevelFailsBottomAssignmentClosed()
        {
            var project = BuildBottomAssignmentProject(3d, out var element, out _, out var top);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                ProjectFloorService.AssignBottomLevel(project, "B", RemoveFloorThenYield(project, top, element)));
            if (project.ChangeVersion != version + 1L)
                throw new InvalidOperationException("Direct Top Level removal did not advance ProjectState.ChangeVersion exactly once.");
            AssertNoBottomMutation(element);
        }

        private static ProjectState BuildBottomAssignmentProject(
            double topElevation,
            out ProjectElement element,
            out FloorDefinition bottom,
            out FloorDefinition top)
        {
            var project = new ProjectState("P-FLOOR-REF-TOP-FRESHNESS", "Floor referenced Top Level freshness");
            bottom = new FloorDefinition("B", "Bottom", 0d);
            top = new FloorDefinition("T", "Top", topElevation);
            project.Floors.Add(bottom);
            project.Floors.Add(top);
            element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties[ProjectFloorService.TopLevelIdKey] = top.Id;
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState BuildTopAssignmentProject(
            double bottomElevation,
            out ProjectElement element,
            out FloorDefinition bottom,
            out FloorDefinition top)
        {
            var project = new ProjectState("P-FLOOR-REF-BOTTOM-FRESHNESS", "Floor referenced Bottom Level freshness");
            bottom = new FloorDefinition("B", "Bottom", bottomElevation);
            top = new FloorDefinition("T", "Top", 3d);
            project.Floors.Add(bottom);
            project.Floors.Add(top);
            element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = bottom.Id;
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> StableTargets(ProjectElement element)
        {
            yield return element;
        }

        private static IEnumerable<ProjectElement> ReplaceFloorThenYield(
            ProjectState project,
            FloorDefinition original,
            double replacementElevation,
            ProjectElement element)
        {
            var index = project.Floors.IndexOf(original);
            if (index < 0) throw new InvalidOperationException("Expected referenced Floor in project.");
            project.Floors[index] = new FloorDefinition(original.Id, original.Name, replacementElevation);
            yield return element;
        }

        private static IEnumerable<ProjectElement> RemoveFloorThenYield(
            ProjectState project,
            FloorDefinition original,
            ProjectElement element)
        {
            if (!project.Floors.Remove(original))
                throw new InvalidOperationException("Expected referenced Floor removal to succeed.");
            yield return element;
        }

        private static void AssertNoBottomMutation(ProjectElement element)
        {
            if (element.Properties.ContainsKey(ProjectFloorService.BottomLevelIdKey) ||
                element.Properties.ContainsKey(ProjectFloorService.BottomLevelOffsetKey))
                throw new InvalidOperationException("Bottom Level assignment mutated the element before rejecting referenced Top Level drift.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}