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
            var project = new ProjectState("FLOOR-NULL-CREATE", "Floor null create");
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 0d));
            project.Floors.Add(null!);

            var floorCount = project.Floors.Count;
            var activeFloorId = project.ActiveFloorId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                ProjectFloorService.Create(project, "L2", "Level 2", 3d);
                throw new InvalidOperationException("Create must reject a project floor collection containing a null entry.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project floor collection contains a null floor.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Create must fail closed with the canonical null-floor integrity error.", ex);
            }

            if (project.Floors.Count != floorCount)
                throw new InvalidOperationException("Rejected floor creation must not change the floor collection.");
            if (!string.Equals(project.ActiveFloorId, activeFloorId, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected floor creation must not change the active floor.");
            if (project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Rejected floor creation must not advance project ChangeVersion.");
            if (project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected floor creation must not change UpdatedUtc.");
        }
    }
}
