using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneGlobalDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsUnrelatedDuplicateAcrossTargetOperations();
            PreservesValidTargetOperations();
        }

        private static void RejectsUnrelatedDuplicateAcrossTargetOperations()
        {
            var project = new ProjectState("ZONE-GLOBAL-DUP", "Zone global duplicate");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1 duplicate"));
            project.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));
            project.ActiveZoneId = "Z1";
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, "Z1");
            project.Elements.Add(element);

            AssertRejectedWithoutMutation(project, element, () => ProjectZoneService.Update(project, "Z2", "Zone 2 renamed"));
            AssertRejectedWithoutMutation(project, element, () => ProjectZoneService.SetActive(project, "Z2"));
            AssertRejectedWithoutMutation(project, element, () => ProjectZoneService.Assign(project, "Z2", new[] { element }));
            AssertRejectedWithoutMutation(project, element, () => ProjectZoneService.Delete(project, "Z2"));
            AssertRejectedWithoutMutation(project, element, () => ProjectZoneService.ReferenceCount(project, "Z2"));
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, ProjectElement element, Action action)
        {
            var zoneCount = project.Zones.Count;
            var targetName = project.Zones[2].Name;
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
                if (!string.Equals(ex.Message, "Project contains duplicate zone id: z1.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Zone target operation returned an unexpected duplicate-integrity error.", ex);
                if (project.Zones.Count != zoneCount ||
                    !string.Equals(project.Zones[2].Name, targetName, StringComparison.Ordinal) ||
                    !string.Equals(project.ActiveZoneId, activeZoneId, StringComparison.Ordinal) ||
                    !string.Equals(element.ZoneId, elementZoneId, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected Zone target operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Zone target operation must reject an unrelated duplicate Zone identity.");
        }

        private static void PreservesValidTargetOperations()
        {
            var project = new ProjectState("ZONE-GLOBAL-VALID", "Zone global valid");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));
            project.ActiveZoneId = "Z1";
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, "Z1");
            project.Elements.Add(element);

            ProjectZoneService.Update(project, "Z2", "Zone 2 renamed");
            ProjectZoneService.SetActive(project, "Z2");
            if (ProjectZoneService.Assign(project, "Z2", new[] { element }) != 1)
                throw new InvalidOperationException("Valid Zone assignment must preserve its mutation result.");
            if (ProjectZoneService.ReferenceCount(project, "Z2") != 1)
                throw new InvalidOperationException("Valid Zone reference count must preserve its result.");
            if (!string.Equals(project.Zones[1].Name, "Zone 2 renamed", StringComparison.Ordinal) ||
                !string.Equals(project.ActiveZoneId, "Z2", StringComparison.Ordinal) ||
                !string.Equals(element.ZoneId, "Z2", StringComparison.Ordinal))
                throw new InvalidOperationException("Valid Zone target operations must preserve their existing semantics.");
        }
    }
}
