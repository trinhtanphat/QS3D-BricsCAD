using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneSemanticCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalIdsRemainCaseInsensitiveAndNamesStillNormalize();
            PaddedCallerIdsRejectBeforeMutation();
            PaddedAssignZoneIdRejectsBeforeTargetEnumeration();
        }

        private static void CanonicalIdsRemainCaseInsensitiveAndNamesStillNormalize()
        {
            var project = new ProjectState("P-ZONE-CANONICAL-OK", "Zone canonical controls");
            var zoneA = ProjectZoneService.Create(project, "zone-a", "  Zone A  ");
            var zoneB = ProjectZoneService.Create(project, "zone-b", "Zone B");

            Equal("zone-a", zoneA.Id, "canonical create id");
            Equal("Zone A", zoneA.Name, "create name normalization");

            var updated = ProjectZoneService.Update(project, "ZONE-A", "  Zone A Updated  ");
            Equal(zoneA, updated, "case-insensitive update identity");
            Equal("Zone A Updated", zoneA.Name, "update name normalization");

            ProjectZoneService.SetActive(project, "ZONE-B");
            Equal(zoneB.Id, project.ActiveZoneId, "case-insensitive active canonicalization");

            var element = new ProjectElement("E-ZONE-1", ElementCategory.Wall);
            project.Elements.Add(element);
            Equal(1, ProjectZoneService.Assign(project, "ZONE-A", new[] { element }), "case-insensitive assign");
            Equal(zoneA.Id, element.ZoneId, "assigned canonical zone id");
            Equal(1, ProjectZoneService.ReferenceCount(project, "ZONE-A"), "case-insensitive reference count");
        }

        private static void PaddedCallerIdsRejectBeforeMutation()
        {
            var project = new ProjectState("P-ZONE-CANONICAL-REJECT", "Zone canonical rejects");
            var zoneA = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var zoneB = ProjectZoneService.Create(project, "zone-b", "Zone B");
            ProjectZoneService.SetActive(project, zoneB.Id);

            var element = new ProjectElement("E-ZONE-REJECT", ElementCategory.Wall);
            project.Elements.Add(element);
            ProjectZoneService.Assign(project, zoneA.Id, new[] { element });
            element.MarkClean(ElementDirtyFlags.All);

            var beforeVersion = project.ChangeVersion;
            var beforeName = zoneA.Name;
            var beforeActive = project.ActiveZoneId;
            var beforeZoneId = element.ZoneId;
            var beforeDirty = element.Dirty;
            var beforeZoneCount = project.Zones.Count;

            foreach (var padded in new[] { " zone-a", "zone-a ", " zone-a ", "\tzone-a\t" })
            {
                Throws<ArgumentException>(() => ProjectZoneService.Update(project, padded, "Changed"));
                Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, padded));
                Throws<ArgumentException>(() => ProjectZoneService.Delete(project, padded));
                Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(project, padded));
            }

            Throws<ArgumentException>(() => ProjectZoneService.Create(project, " zone-c ", "Zone C"));

            Equal(beforeVersion, project.ChangeVersion, "rejected caller version");
            Equal(beforeName, zoneA.Name, "rejected caller name");
            Equal(beforeActive, project.ActiveZoneId, "rejected caller active zone");
            Equal(beforeZoneId, element.ZoneId, "rejected caller element zone");
            Equal(beforeDirty, element.Dirty, "rejected caller dirty state");
            Equal(beforeZoneCount, project.Zones.Count, "rejected caller zone count");
        }

        private static void PaddedAssignZoneIdRejectsBeforeTargetEnumeration()
        {
            var project = new ProjectState("P-ZONE-CANONICAL-ENUM", "Zone canonical enumeration");
            ProjectZoneService.Create(project, "zone-a", "Zone A");
            var source = new ExplodingEnumerable();
            var beforeVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectZoneService.Assign(project, " zone-a ", source));

            Equal(0, source.EnumerationRequests, "padded assign enumeration requests");
            Equal(beforeVersion, project.ChangeVersion, "padded assign version");
        }

        private sealed class ExplodingEnumerable : IEnumerable<ProjectElement>
        {
            internal int EnumerationRequests { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationRequests++;
                throw new InvalidOperationException("Target enumeration must not begin for a noncanonical Zone id.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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