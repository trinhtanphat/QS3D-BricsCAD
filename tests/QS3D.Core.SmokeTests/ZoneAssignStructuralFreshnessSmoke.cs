using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneAssignStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RemovedElementDuringLazyEnumerationFailsClosed();
            RemovedTargetZoneDuringLazyEnumerationFailsClosed();
        }

        private static void RemovedElementDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-ZONE-STRUCT-1", out var zone, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, YieldThenRemoveElement(project, element)),
                "Element no longer belongs to the project after Zone assignment target enumeration");

            Equal(beforeVersion, project.ChangeVersion, "removed-element project revision");
            False(project.Elements.Contains(element), "removed-element external removal");
            Equal(string.Empty, element.ZoneId, "removed-element ZoneId");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-element dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-element timestamp");
        }

        private static void RemovedTargetZoneDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-ZONE-STRUCT-2", out var zone, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, YieldThenRemoveZone(project, zone, element)),
                "Target Zone no longer belongs to the project after assignment target enumeration");

            Equal(beforeVersion, project.ChangeVersion, "removed-zone project revision");
            False(project.Zones.Contains(zone), "removed-zone external removal");
            Equal(string.Empty, element.ZoneId, "removed-zone ZoneId");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-zone dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-zone timestamp");
        }

        private static ProjectState CreateProject(string id, out ZoneDefinition zone, out ProjectElement element)
        {
            var project = new ProjectState(id, "Zone structural freshness");
            zone = new ZoneDefinition("ZONE-STRUCT", "Structural Zone");
            element = new ProjectElement("E-ZONE-STRUCT", ElementCategory.Beam);
            project.Zones.Add(zone);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> YieldThenRemoveElement(ProjectState project, ProjectElement element)
        {
            yield return element;
            project.Elements.Remove(element);
        }

        private static IEnumerable<ProjectElement> YieldThenRemoveZone(ProjectState project, ZoneDefinition zone, ProjectElement element)
        {
            yield return element;
            project.Zones.Remove(zone);
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("ZoneAssignStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ZoneAssignStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("ZoneAssignStructuralFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("ZoneAssignStructuralFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
