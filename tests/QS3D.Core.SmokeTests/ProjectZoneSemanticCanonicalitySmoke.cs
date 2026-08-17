using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneSemanticCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CallerZoneIdsMustBeCanonical();
            StoredZoneReferencesMustBeCanonicalAndAtomic();
            ActiveZoneStateMustBeCanonicalAndAtomic();
            OrdinaryZoneWorkflowRemainsCompatible();
        }

        private static void CallerZoneIdsMustBeCanonical()
        {
            var project = NewProject("caller-zone-id");
            var initialVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectZoneService.Create(project, " Z1", "Zone 1"));
            Throws<ArgumentException>(() => ProjectZoneService.Create(project, "Z1 ", "Zone 1"));
            Throws<ArgumentException>(() => ProjectZoneService.Create(project, "\tZ1", "Zone 1"));
            Equal(0, project.Zones.Count, "padded create zone count");
            Equal(initialVersion, project.ChangeVersion, "padded create version");

            var zone = ProjectZoneService.Create(project, "Z1", "  Zone 1  ");
            Equal("Zone 1", zone.Name, "zone name trimming remains supported");
            var afterCreate = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, " Z1 ", "Changed"));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, "Z1\t"));
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(project, " Z1"));
            Throws<ArgumentException>(() => ProjectZoneService.Delete(project, "Z1 "));
            Equal("Zone 1", zone.Name, "padded API id zone name");
            Equal(afterCreate, project.ChangeVersion, "padded API id version");
        }

        private static void StoredZoneReferencesMustBeCanonicalAndAtomic()
        {
            var project = NewProject("stored-zone-ref");
            var zone1 = ProjectZoneService.Create(project, "Z1", "Zone 1");
            var zone2 = ProjectZoneService.Create(project, "Z2", "Zone 2");
            ProjectZoneService.SetActive(project, zone2.Id);
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, zone1.Id);
            project.Elements.Add(element);

            element.ZoneId = " Z1 ";
            var before = project.ChangeVersion;
            var dirtyBefore = element.Dirty;

            Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(project, zone1.Id));
            Equal(before, project.ChangeVersion, "padded stored reference count version");

            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, zone2.Id, new[] { element }));
            Equal(" Z1 ", element.ZoneId, "padded stored reference assign state");
            Equal(dirtyBefore, element.Dirty, "padded stored reference dirty state");
            Equal(before, project.ChangeVersion, "padded stored reference assign version");

            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, zone1.Id));
            Equal(2, project.Zones.Count, "padded stored reference delete zone count");
            Equal(before, project.ChangeVersion, "padded stored reference delete version");
        }

        private static void ActiveZoneStateMustBeCanonicalAndAtomic()
        {
            var project = NewProject("active-zone-ref");
            var zone1 = ProjectZoneService.Create(project, "Z1", "Zone 1");
            var zone2 = ProjectZoneService.Create(project, "Z2", "Zone 2");
            project.ActiveZoneId = " Z1 ";
            var before = project.ChangeVersion;

            Throws<InvalidOperationException>(() => ProjectZoneService.SetActive(project, zone2.Id));
            Equal(" Z1 ", project.ActiveZoneId, "padded active state retained after rejected SetActive");
            Equal(before, project.ChangeVersion, "padded active SetActive version");

            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, zone1.Id));
            Equal(" Z1 ", project.ActiveZoneId, "padded active state retained after rejected Delete");
            Equal(2, project.Zones.Count, "padded active delete zone count");
            Equal(before, project.ChangeVersion, "padded active Delete version");
        }

        private static void OrdinaryZoneWorkflowRemainsCompatible()
        {
            var project = NewProject("ordinary-zone");
            var zone1 = ProjectZoneService.Create(project, "Zone-A", " Zone A ");
            var zone2 = ProjectZoneService.Create(project, "zone-b", "Zone B");
            ProjectZoneService.SetActive(project, "ZONE-B");
            Equal(zone2.Id, project.ActiveZoneId, "case-insensitive SetActive");

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            Equal(1, ProjectZoneService.Assign(project, "ZONE-A", new[] { element }), "ordinary assign count");
            Equal(zone1.Id, element.ZoneId, "ordinary assign zone id");
            Equal(1, ProjectZoneService.ReferenceCount(project, "zone-a"), "ordinary reference count");
            Equal(0, ProjectZoneService.Assign(project, zone1.Id, new[] { element }), "canonical assign no-op");

            ProjectZoneService.Update(project, "ZONE-A", "  Zone A renamed  ");
            Equal("Zone A renamed", zone1.Name, "ordinary name trimming");
        }

        private static ProjectState NewProject(string suffix)
        {
            return new ProjectState("P-ZONE-CANON-" + suffix, "Zone canonicality smoke");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectZoneSemanticCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "ProjectZoneSemanticCanonicalitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
