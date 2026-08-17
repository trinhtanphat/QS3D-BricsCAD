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
            CallerZoneIdsFailClosedWithoutMutation();
            ZoneNamesStillNormalize();
            StoredZoneReferencesFailClosedWithoutMutation();
            CorruptStoredReferenceFailsBeforeTargetEnumeration();
            CanonicalCaseInsensitiveAssignmentStillWorks();
        }

        private static void CallerZoneIdsFailClosedWithoutMutation()
        {
            var project = NewProject("caller");
            var zone = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var before = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, " zone-a", "Changed"));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, "zone-a "));
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(project, "\tzone-a"));
            Throws<ArgumentException>(() => ProjectZoneService.Delete(project, " zone-a "));

            Equal("Zone A", zone.Name, "padded caller name state");
            Equal(zone.Id, project.ActiveZoneId, "padded caller active state");
            Equal(before, project.ChangeVersion, "padded caller project version");
        }

        private static void ZoneNamesStillNormalize()
        {
            var project = NewProject("names");
            var zone = ProjectZoneService.Create(project, "zone-a", "  Khu A  ");
            Equal("Khu A", zone.Name, "create name normalization");

            ProjectZoneService.Update(project, "ZONE-A", "  Khu B  ");
            Equal("Khu B", zone.Name, "update name normalization");
        }

        private static void StoredZoneReferencesFailClosedWithoutMutation()
        {
            var project = NewProject("stored");
            var zoneA = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var zoneB = ProjectZoneService.Create(project, "zone-b", "Zone B");
            ProjectZoneService.SetActive(project, zoneB.Id);

            var element = NewElement("E1");
            project.Elements.Add(element);
            Equal(1, ProjectZoneService.Assign(project, zoneA.Id, new[] { element }), "initial assignment count");

            element.ZoneId = " ZONE-A ";
            var beforeReference = project.ChangeVersion;
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(project, zoneA.Id));
            Equal(" ZONE-A ", element.ZoneId, "padded element ZoneId preserved");
            Equal(beforeReference, project.ChangeVersion, "padded element reference version");

            project.ActiveZoneId = " ZONE-B ";
            var beforeDelete = project.ChangeVersion;
            Throws<ArgumentException>(() => ProjectZoneService.Delete(project, zoneA.Id));
            Equal(" ZONE-B ", project.ActiveZoneId, "padded ActiveZoneId preserved");
            Equal(beforeDelete, project.ChangeVersion, "padded ActiveZoneId delete version");
        }

        private static void CorruptStoredReferenceFailsBeforeTargetEnumeration()
        {
            var project = NewProject("enumeration");
            var zone = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var element = NewElement("E1");
            element.ZoneId = " zone-a ";
            project.Elements.Add(element);
            var before = project.ChangeVersion;
            var targets = new ThrowIfEnumerated();

            Throws<ArgumentException>(() => ProjectZoneService.Assign(project, zone.Id, targets));

            Equal(false, targets.Enumerated, "target enumerable untouched");
            Equal(" zone-a ", element.ZoneId, "corrupt stored reference preserved");
            Equal(before, project.ChangeVersion, "fail-before-enumeration version");
        }

        private static void CanonicalCaseInsensitiveAssignmentStillWorks()
        {
            var project = NewProject("canonical");
            var zoneA = ProjectZoneService.Create(project, "zone-a", "Zone A");
            var zoneB = ProjectZoneService.Create(project, "zone-b", "Zone B");
            ProjectZoneService.SetActive(project, zoneB.Id);
            var element = NewElement("Element-A");
            project.Elements.Add(element);

            Equal(1, ProjectZoneService.Assign(project, "ZONE-A", new[] { element }), "case-insensitive assign");
            Equal(zoneA.Id, element.ZoneId, "canonical stored ZoneId");
            Equal(1, ProjectZoneService.ReferenceCount(project, "ZONE-A"), "case-insensitive reference count");
            Equal(0, ProjectZoneService.Assign(project, "zone-A", new[] { element }), "canonical no-op assignment");
        }

        private static ProjectState NewProject(string suffix)
        {
            return new ProjectState("P-ZONE-CANONICAL-" + suffix, "Zone canonicality " + suffix);
        }

        private static ProjectElement NewElement(string id)
        {
            return new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
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

        private sealed class ThrowIfEnumerated : IEnumerable<ProjectElement>
        {
            public bool Enumerated { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Target enumeration should not occur for corrupt stored Zone references.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
