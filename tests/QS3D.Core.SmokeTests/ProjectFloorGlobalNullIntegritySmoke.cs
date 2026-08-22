using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorGlobalNullIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullFloorAcrossTargetOperations();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullFloorAcrossTargetOperations()
        {
            var project = new ProjectState("FLOOR-GLOBAL-NULL", "Floor global null");
            var source = new FloorDefinition("F1", "Level 1", 0d);
            var target = new FloorDefinition("F2", "Level 2", 3d);
            project.Floors.Add(source);
            project.Floors.Add(target);
            project.Floors.Add(null!);
            project.ActiveFloorId = source.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, source.Id, string.Empty);
            project.Elements.Add(element);

            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.Update(project, target.Id, "Level 2 renamed", 3.5d));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.SetActive(project, target.Id));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.Assign(project, target.Id, new[] { element }));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.AssignBottomLevel(project, target.Id, new[] { element }));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.AssignTopLevel(project, target.Id, new[] { element }));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.Delete(project, target.Id));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFloorService.ReferenceCount(project, target.Id));
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, FloorDefinition target, ProjectElement element, Action action)
        {
            var floorCount = project.Floors.Count;
            var targetName = target.Name;
            var targetElevation = target.ElevationM;
            var activeFloorId = project.ActiveFloorId;
            var elementFloorId = element.FloorId;
            var propertyCount = element.Properties.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project floor collection contains a null floor.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Floor target operation returned an unexpected null-integrity error.", ex);
                if (project.Floors.Count != floorCount ||
                    !string.Equals(target.Name, targetName, StringComparison.Ordinal) ||
                    target.ElevationM != targetElevation ||
                    !string.Equals(project.ActiveFloorId, activeFloorId, StringComparison.Ordinal) ||
                    !string.Equals(element.FloorId, elementFloorId, StringComparison.Ordinal) ||
                    element.Properties.Count != propertyCount ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected Floor target operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Floor target operation must reject a null Floor collection entry.");
        }

        private static void PreservesValidTargetOperations()
        {
            var project = new ProjectState("FLOOR-GLOBAL-NULL-VALID", "Floor global null valid");
            var source = new FloorDefinition("F1", "Level 1", 0d);
            var target = new FloorDefinition("F2", "Level 2", 3d);
            project.Floors.Add(source);
            project.Floors.Add(target);
            project.ActiveFloorId = source.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, source.Id, string.Empty);
            project.Elements.Add(element);

            ProjectFloorService.Update(project, target.Id, "Level 2 renamed", 3.5d);
            ProjectFloorService.SetActive(project, target.Id);
            if (ProjectFloorService.Assign(project, target.Id, new[] { element }) != 1)
                throw new InvalidOperationException("Valid Floor assignment must preserve its mutation result.");
            if (ProjectFloorService.ReferenceCount(project, target.Id) != 1)
                throw new InvalidOperationException("Valid Floor reference count must preserve its result.");
            if (!string.Equals(target.Name, "Level 2 renamed", StringComparison.Ordinal) ||
                target.ElevationM != 3.5d ||
                !string.Equals(project.ActiveFloorId, target.Id, StringComparison.Ordinal) ||
                !string.Equals(element.FloorId, target.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid Floor target operations changed behavior after null-integrity hardening.");
        }
    }
}
