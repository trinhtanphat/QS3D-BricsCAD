using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneGlobalNullIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullZoneAtCatalogBoundaryWithoutMutation();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullZoneAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("ZONE-GLOBAL-NULL", "Zone global null");
            var source = new ZoneDefinition("Z1", "Zone 1");
            var target = new ZoneDefinition("Z2", "Zone 2");
            project.Zones.Add(source);
            project.Zones.Add(target);
            project.ActiveZoneId = source.Id;

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
                if (project.Zones.Count != zoneCount ||
                    !string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc ||
                    !ReferenceEquals(project.FindZone(source.Id), source) ||
                    !ReferenceEquals(project.FindZone(target.Id), target))
                    throw new InvalidOperationException("Rejected null-Zone admission mutated project state.");
                return;
            }

            throw new InvalidOperationException("Zone catalog must reject null entries at the admission boundary.");
        }

        private static void PreservesValidTargetOperations()
        {
            var project = new ProjectState("ZONE-GLOBAL-NULL-VALID", "Zone global null valid");
            var source = new ZoneDefinition("Z1", "Zone 1");
            var target = new ZoneDefinition("Z2", "Zone 2");
            project.Zones.Add(source);
            project.Zones.Add(target);
            project.ActiveZoneId = source.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, source.Id);
            project.Elements.Add(element);

            ProjectZoneService.Update(project, target.Id, "Zone 2 renamed");
            ProjectZoneService.SetActive(project, target.Id);
            if (ProjectZoneService.Assign(project, target.Id, new[] { element }) != 1)
                throw new InvalidOperationException("Valid Zone assignment must preserve its mutation result.");
            if (ProjectZoneService.ReferenceCount(project, target.Id) != 1)
                throw new InvalidOperationException("Valid Zone reference count must preserve its result.");
            if (!string.Equals(target.Name, "Zone 2 renamed", StringComparison.Ordinal) ||
                !string.Equals(project.ActiveZoneId, target.Id, StringComparison.Ordinal) ||
                !string.Equals(element.ZoneId, target.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid Zone target operations changed behavior after null-integrity hardening.");
        }
    }
}
