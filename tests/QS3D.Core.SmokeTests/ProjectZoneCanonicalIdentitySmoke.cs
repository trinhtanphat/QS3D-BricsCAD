using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCanonicalIdentitySmoke
    {
        public static void Run()
        {
            CreateRejectsPaddedIdentityBeforeMutation();
            LookupEntryPointsRejectPaddedIdentityBeforeMutation();
            AssignRejectsPaddedIdentityBeforeTargetEnumeration();
            CanonicalCaseInsensitiveIdentityStillWorks();
            ZoneNameWhitespaceNormalizationRemainsCompatible();
        }

        private static void CreateRejectsPaddedIdentityBeforeMutation()
        {
            foreach (var id in new[] { " zone-new", "zone-new ", " zone-new ", "\tzone-new" })
            {
                var project = NewProject("create");
                var beforeVersion = project.ChangeVersion;
                var beforeCount = project.Zones.Count;
                Throws<ArgumentException>(() => ProjectZoneService.Create(project, id, "New Zone"));
                Equal(beforeVersion, project.ChangeVersion, "Rejected Create changed project version.");
                Equal(beforeCount, project.Zones.Count, "Rejected Create changed Zone catalog.");
            }
        }

        private static void LookupEntryPointsRejectPaddedIdentityBeforeMutation()
        {
            var project = NewProject("lookup");
            var target = new ZoneDefinition("zone-target", "Target");
            project.Zones.Add(target);
            var beforeVersion = project.ChangeVersion;
            var beforeName = target.Name;
            var beforeActive = project.ActiveZoneId;

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, " zone-target ", "Renamed"));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, " zone-target "));
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(project, " zone-target "));
            Throws<ArgumentException>(() => ProjectZoneService.Delete(project, " zone-target "));

            Equal(beforeVersion, project.ChangeVersion, "Rejected lookup entry point changed project version.");
            Equal(beforeName, target.Name, "Rejected Update changed Zone name.");
            Equal(beforeActive, project.ActiveZoneId, "Rejected SetActive changed ActiveZoneId.");
            Equal(2, project.Zones.Count, "Rejected Delete changed Zone catalog.");
        }

        private static void AssignRejectsPaddedIdentityBeforeTargetEnumeration()
        {
            var project = NewProject("assign");
            var target = new ZoneDefinition("zone-target", "Target");
            project.Zones.Add(target);
            var element = new ProjectElement("element-1", ElementCategory.Beam, string.Empty, string.Empty, "zone-source");
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var source = new ThrowOnEnumeration(element);

            Throws<ArgumentException>(() => ProjectZoneService.Assign(project, " zone-target ", source));

            Equal(0, source.EnumerationAttempts, "Padded Zone id was validated after target enumeration started.");
            Equal(beforeVersion, project.ChangeVersion, "Rejected Assign changed project version.");
            Equal("zone-source", element.ZoneId, "Rejected Assign changed element ZoneId.");
            Equal(ElementDirtyFlags.None, element.Dirty, "Rejected Assign dirtied the element.");
        }

        private static void CanonicalCaseInsensitiveIdentityStillWorks()
        {
            var project = NewProject("case");
            var target = new ZoneDefinition("ZONE-TARGET", "Target");
            project.Zones.Add(target);
            var element = new ProjectElement("element-1", ElementCategory.Beam, string.Empty, string.Empty, "zone-source");
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var changed = ProjectZoneService.Assign(project, "zone-target", new[] { element });

            Equal(1, changed, "Canonical case-insensitive Zone lookup did not assign the element.");
            Equal(beforeVersion + 1L, project.ChangeVersion, "Canonical assignment did not commit exactly once.");
            Equal("ZONE-TARGET", element.ZoneId, "Canonical assignment did not preserve catalog Zone identity.");
            Equal(1, ProjectZoneService.ReferenceCount(project, "zone-target"), "Canonical case-insensitive ReferenceCount failed.");
        }

        private static void ZoneNameWhitespaceNormalizationRemainsCompatible()
        {
            var project = NewProject("name");
            var created = ProjectZoneService.Create(project, "zone-new", "  New Zone  ");
            Equal("New Zone", created.Name, "Zone name whitespace normalization changed while hardening IDs.");
        }

        private static ProjectState NewProject(string suffix)
        {
            var project = new ProjectState("zone-canonical-" + suffix, "Zone canonical " + suffix);
            var source = new ZoneDefinition("zone-source", "Source");
            project.Zones.Add(source);
            project.ActiveZoneId = source.Id;
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ThrowOnEnumeration : System.Collections.Generic.IEnumerable<ProjectElement>
        {
            private readonly ProjectElement _element;
            public ThrowOnEnumeration(ProjectElement element) { _element = element; }
            public int EnumerationAttempts { get; private set; }
            public System.Collections.Generic.IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Target source must not be enumerated before Zone id validation.");
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
