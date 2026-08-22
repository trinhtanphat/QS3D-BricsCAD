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
            RejectsNullZoneAcrossTargetOperations();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullZoneAcrossTargetOperations()
        {
            var project = new ProjectState("ZONE-GLOBAL-NULL", "Zone global null");
            var source = new ZoneDefinition("Z1", "Zone 1");
            var target = new ZoneDefinition("Z2", "Zone 2");
            project.Zones.Add(source);
            project.Zones.Add(target);
            project.Zones.Add(null!);
            project.ActiveZoneId = source.Id;
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, source.Id);
            project.Elements.Add(element);

            AssertRejectedWithoutMutation(project, target, element, () => ProjectZoneService.Update(project, target.Id, "Zone 2 renamed"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectZoneService.SetActive(project, target.Id));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectZoneService.Assign(project, target.Id, new[] { element }));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectZoneService.Delete(project, target.Id));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectZoneService.ReferenceCount(project, target.Id));
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, ZoneDefinition target, ProjectElement element, Action action)
        {
            var zoneCount = project.Zones.Count;
            var targetName = target.Name;
            var activeZoneId = project.ActiveZoneId;
            var elementZoneId = element.ZoneId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project zone collection contains a null zone.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Zone target operation returned an unexpected null-integrity error.", ex);
                if (project.Zones.Count != zoneCount ||
                    !string.Equals(target.Name, targetName, StringComparison.Ordinal) ||
                    !string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal) ||
                    !string.Equals(element.ZoneId, elementZoneId, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected Zone target operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Zone target operation must reject a null Zone collection entry.");
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
