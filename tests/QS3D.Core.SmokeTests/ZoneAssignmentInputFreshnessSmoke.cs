using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneAssignmentInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyInputAssignsZone();
            MutatingLazyInputFailsBeforeAssignment();
            MutatingEmptyInputFailsBeforeNoOp();
        }

        private static void StableLazyInputAssignsZone()
        {
            var project = CreateProject("P-ZONE-FRESH-1", out var zone, out var element);
            element.MarkClean(ElementDirtyFlags.All);

            Equal(1, ProjectZoneService.Assign(project, zone.Id, LazyElement(element)));
            Equal(zone.Id, element.ZoneId);
            True((element.Dirty & ElementDirtyFlags.Relations) != 0);
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
        }

        private static void MutatingLazyInputFailsBeforeAssignment()
        {
            var project = CreateProject("P-ZONE-FRESH-2", out var zone, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, TouchThenYield(project, element)),
                "Project changed while Zone assignment targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(string.Empty, element.ZoneId);
            Equal(ElementDirtyFlags.None, element.Dirty);
        }

        private static void MutatingEmptyInputFailsBeforeNoOp()
        {
            var project = CreateProject("P-ZONE-FRESH-3", out var zone, out _);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, TouchThenStop(project)),
                "Project changed while Zone assignment targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static ProjectState CreateProject(string id, out ZoneDefinition zone, out ProjectElement element)
        {
            var project = new ProjectState(id, "Zone assignment freshness");
            zone = new ZoneDefinition("ZONE-1", "Zone 1");
            element = new ProjectElement("E-1", ElementCategory.Room);
            project.Zones.Add(zone);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> LazyElement(ProjectElement element)
        {
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenYield(ProjectState project, ProjectElement element)
        {
            project.Touch();
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
