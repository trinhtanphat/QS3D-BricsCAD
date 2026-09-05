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
            RejectsNullZoneAtCatalogBoundaryWithoutMutation();
            PreservesValidCreate();
        }

        private static void RejectsNullZoneAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("ZONE-NULL-CREATE", "Zone null create");
            var zone = new ZoneDefinition("Z1", "Zone 1");
            project.Zones.Add(zone);

            var zoneCount = project.Zones.Count;
            var activeZoneId = project.ActiveZoneId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                project.Zones.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Zone admission failed for the wrong parameter.", ex);
                if (project.Zones.Count != zoneCount)
                    throw new InvalidOperationException("Rejected null-Zone admission must not change the Zone collection.");
                if (!string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rejected null-Zone admission must not change the active Zone.");
                if (project.ChangeVersion != changeVersion)
                    throw new InvalidOperationException("Rejected null-Zone admission must not advance project ChangeVersion.");
                if (project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected null-Zone admission must not change UpdatedUtc.");
                if (!ReferenceEquals(project.FindZone("Z1"), zone))
                    throw new InvalidOperationException("Rejected null-Zone admission must preserve existing Zone lookup state.");
                return;
            }

            throw new InvalidOperationException("Zone catalog must reject null entries before Zone creation preflight can observe malformed state.");
        }

        private static void PreservesValidCreate()
        {
            var project = new ProjectState("ZONE-CREATE-OK", "Zone create ok");
            var created = ProjectZoneService.Create(project, "Z1", "Zone 1");
            if (!ReferenceEquals(project.FindZone("Z1"), created))
                throw new InvalidOperationException("Valid Zone creation must remain supported.");
        }
    }
}
