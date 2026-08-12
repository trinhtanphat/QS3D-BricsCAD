using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCreateNullPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("ZONE-NULL-CREATE", "Zone null create");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(null!);

            var zoneCount = project.Zones.Count;
            var activeZoneId = project.ActiveZoneId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                ProjectZoneService.Create(project, "Z2", "Zone 2");
                throw new InvalidOperationException("Create must reject a project zone collection containing a null entry.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project zone collection contains a null zone.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Create must fail closed with the canonical null-zone integrity error.", ex);
            }

            if (project.Zones.Count != zoneCount)
                throw new InvalidOperationException("Rejected zone creation must not change the zone collection.");
            if (!string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected zone creation must not change the active zone.");
            if (project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Rejected zone creation must not advance project ChangeVersion.");
            if (project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected zone creation must not change UpdatedUtc.");
        }
    }
}
