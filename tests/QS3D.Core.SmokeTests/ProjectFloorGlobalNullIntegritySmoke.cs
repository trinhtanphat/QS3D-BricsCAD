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
            RejectsNullFloorAtCatalogBoundaryWithoutMutation();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullFloorAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("FLOOR-GLOBAL-NULL", "Floor global null");
            var source = new FloorDefinition("F1", "Level 1", 0d);
            var target = new FloorDefinition("F2", "Level 2", 3d);
            project.Floors.Add(source);
            project.Floors.Add(target);
            project.ActiveFloorId = source.Id;

            var floorCount = project.Floors.Count;
            var activeFloorId = project.ActiveFloorId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                project.Floors.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Floor admission failed for the wrong parameter.", ex);
                if (project.Floors.Count != floorCount ||
                    !string.Equals(project.ActiveFloorId, activeFloorId, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc ||
                    !ReferenceEquals(project.FindFloor(source.Id), source) ||
                    !ReferenceEquals(project.FindFloor(target.Id), target))
                    throw new InvalidOperationException("Rejected null-Floor admission mutated project state.");
                return;
            }

            throw new InvalidOperationException("Floor catalog must reject null entries at the admission boundary.");
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
