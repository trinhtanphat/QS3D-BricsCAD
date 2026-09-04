using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorCreateNullPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullFloorAtCatalogBoundaryWithoutMutation();
            PreservesValidCreate();
        }

        private static void RejectsNullFloorAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("FLOOR-NULL-CREATE", "Floor null create");
            var floor = new FloorDefinition("L1", "Level 1", 0d);
            project.Floors.Add(floor);

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
                if (project.Floors.Count != floorCount)
                    throw new InvalidOperationException("Rejected null-Floor admission must not change the Floor collection.");
                if (!string.Equals(project.ActiveFloorId, activeFloorId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rejected null-Floor admission must not change the active Floor.");
                if (project.ChangeVersion != changeVersion)
                    throw new InvalidOperationException("Rejected null-Floor admission must not advance project ChangeVersion.");
                if (project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected null-Floor admission must not change UpdatedUtc.");
                if (!ReferenceEquals(project.FindFloor("L1"), floor))
                    throw new InvalidOperationException("Rejected null-Floor admission must preserve existing Floor lookup state.");
                return;
            }

            throw new InvalidOperationException("Floor catalog must reject null entries before Floor creation preflight can observe malformed state.");
        }

        private static void PreservesValidCreate()
        {
            var project = new ProjectState("FLOOR-CREATE-OK", "Floor create ok");
            var created = ProjectFloorService.Create(project, "L1", "Level 1", 0d);
            if (!ReferenceEquals(project.FindFloor("L1"), created))
                throw new InvalidOperationException("Valid Floor creation must remain supported.");
        }
    }
}
