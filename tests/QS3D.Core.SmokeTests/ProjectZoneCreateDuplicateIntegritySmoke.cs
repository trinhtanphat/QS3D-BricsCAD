using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCreateDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsExistingDuplicateIdsWithoutMutation();
            PreservesValidCreate();
        }

        private static void RejectsExistingDuplicateIdsWithoutMutation()
        {
            var project = new ProjectState("ZONE-DUP-CREATE", "Zone duplicate create");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1 duplicate"));
            project.ActiveZoneId = "Z1";

            var zoneCount = project.Zones.Count;
            var activeZoneId = project.ActiveZoneId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                ProjectZoneService.Create(project, "Z2", "Zone 2");
                throw new InvalidOperationException("Create must reject pre-existing duplicate Zone ids.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project contains duplicate zone id: z1.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Create must fail closed with the canonical duplicate-Zone integrity error.", ex);
            }

            if (project.Zones.Count != zoneCount)
                throw new InvalidOperationException("Rejected Zone creation must not change the Zone collection.");
            if (!string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected Zone creation must not change ActiveZoneId.");
            if (project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Rejected Zone creation must not advance ChangeVersion.");
            if (project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected Zone creation must not change UpdatedUtc.");
        }

        private static void PreservesValidCreate()
        {
            var project = new ProjectState("ZONE-VALID-CREATE", "Zone valid create");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.ActiveZoneId = "Z1";
            var changeVersion = project.ChangeVersion;

            var created = ProjectZoneService.Create(project, "Z2", "Zone 2");
            if (!string.Equals(created.Id, "Z2", StringComparison.Ordinal) || project.Zones.Count != 2)
                throw new InvalidOperationException("Valid Zone creation must preserve the existing Create contract.");
            if (project.ChangeVersion != checked(changeVersion + 2L))
                throw new InvalidOperationException("Valid Zone creation must advance ChangeVersion once for the semantic service touch and once for the catalog structural add.");
            if (!string.Equals(project.ActiveZoneId, "Z1", StringComparison.Ordinal))
                throw new InvalidOperationException("Creating a second Zone must preserve the existing active Zone.");
        }
    }
}
