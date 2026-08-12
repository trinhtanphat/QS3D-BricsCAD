using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorMutationInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyInputAssignsFloor();
            MutatingLazyInputFailsBeforeFloorAssignment();
            MutatingEmptyInputFailsBeforeNoOp();
            MutatingBottomLevelInputUsesSharedGuard();
        }

        private static void StableLazyInputAssignsFloor()
        {
            var project = CreateProject("P-FLOOR-FRESH-1", out var floor, out var element);
            element.MarkClean(ElementDirtyFlags.All);

            Equal(1, ProjectFloorService.Assign(project, floor.Id, LazyElement(element)));
            Equal(floor.Id, element.FloorId);
            True((element.Dirty & ElementDirtyFlags.Relations) != 0);
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
        }

        private static void MutatingLazyInputFailsBeforeFloorAssignment()
        {
            var project = CreateProject("P-FLOOR-FRESH-2", out var floor, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFloorService.Assign(project, floor.Id, TouchThenYield(project, element)),
                "Project changed while Floor mutation targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(string.Empty, element.FloorId);
            Equal(ElementDirtyFlags.None, element.Dirty);
        }

        private static void MutatingEmptyInputFailsBeforeNoOp()
        {
            var project = CreateProject("P-FLOOR-FRESH-3", out var floor, out _);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFloorService.Assign(project, floor.Id, TouchThenStop(project)),
                "Project changed while Floor mutation targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static void MutatingBottomLevelInputUsesSharedGuard()
        {
            var project = CreateProject("P-FLOOR-FRESH-4", out var floor, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFloorService.AssignBottomLevel(project, floor.Id, TouchThenYield(project, element)),
                "Project changed while Floor mutation targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            False(element.Properties.ContainsKey(ProjectFloorService.BottomLevelIdKey));
            False(element.Properties.ContainsKey(ProjectFloorService.BottomLevelOffsetKey));
            Equal(ElementDirtyFlags.None, element.Dirty);
        }

        private static ProjectState CreateProject(string id, out FloorDefinition floor, out ProjectElement element)
        {
            var project = new ProjectState(id, "Floor mutation freshness");
            floor = new FloorDefinition("FLOOR-1", "Floor 1", 0d);
            element = new ProjectElement("E-1", ElementCategory.Room);
            project.Floors.Add(floor);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> LazyElement(ProjectElement element)
        {
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenYield(ProjectState project, ProjectElement element)
        {
            project.Touch();
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
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
