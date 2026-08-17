using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCanonicalReferenceSmoke
    {
        public static void Run()
        {
            CanonicalCaseInsensitiveReferencesRemainSupported();
            PaddedPublicZoneIdsFailBeforeMutation();
            PaddedStoredZoneReferenceFailsBeforeMutation();
            PaddedProjectElementIdFailsBeforeMutation();
            PaddedActiveZoneReferenceFailsBeforeMutation();
        }

        private static void CanonicalCaseInsensitiveReferencesRemainSupported()
        {
            var fixture = NewFixture("canonical");
            SetPrivateField(fixture.Element, "_zoneId", "ZONE-SOURCE");
            Equal(1, ProjectZoneService.ReferenceCount(fixture.Project, "ZONE-SOURCE"),
                "Canonical case-insensitive Zone reference was not resolved.");

            var beforeVersion = fixture.Project.ChangeVersion;
            var changed = ProjectZoneService.Assign(fixture.Project, "ZONE-TARGET", new[] { fixture.Element });
            Equal(1, changed, "Canonical case-insensitive Zone assignment did not report one change.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion,
                "Canonical Zone assignment did not touch the project exactly once.");
            Equal(fixture.TargetZone.Id, fixture.Element.ZoneId,
                "Canonical Zone assignment did not store the canonical target id.");
        }

        private static void PaddedPublicZoneIdsFailBeforeMutation()
        {
            var fixture = NewFixture("public-id");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var beforeCount = fixture.Project.Zones.Count;
            var beforeActive = fixture.Project.ActiveZoneId;
            var beforeZoneId = fixture.Element.ZoneId;

            Throws<ArgumentException>(() => ProjectZoneService.Create(fixture.Project, " zone-new ", "New zone"));
            Throws<ArgumentException>(() => ProjectZoneService.Update(fixture.Project, " zone-source ", "Renamed"));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(fixture.Project, " zone-source "));
            Throws<ArgumentException>(() => ProjectZoneService.Assign(fixture.Project, " zone-target ", new[] { fixture.Element }));
            Throws<ArgumentException>(() => ProjectZoneService.Delete(fixture.Project, " zone-source "));
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(fixture.Project, " zone-source "));

            Equal(beforeVersion, fixture.Project.ChangeVersion, "Rejected padded Zone id changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Rejected padded Zone id changed project timestamp.");
            Equal(beforeCount, fixture.Project.Zones.Count, "Rejected padded Zone id changed Zone collection.");
            Equal(beforeActive, fixture.Project.ActiveZoneId, "Rejected padded Zone id changed active Zone.");
            Equal(beforeZoneId, fixture.Element.ZoneId, "Rejected padded Zone id changed element Zone reference.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Rejected padded Zone id dirtied the element.");
        }

        private static void PaddedStoredZoneReferenceFailsBeforeMutation()
        {
            foreach (var malformed in new[] { " zone-source", "zone-source ", " zone-source ", "\tzone-source\t" })
            {
                var fixture = NewFixture("stored");
                SetPrivateField(fixture.Element, "_zoneId", malformed);
                var beforeVersion = fixture.Project.ChangeVersion;
                var beforeUtc = fixture.Project.UpdatedUtc;

                Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(fixture.Project, fixture.SourceZone.Id));
                Throws<InvalidOperationException>(() => ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, new[] { fixture.Element }));
                Throws<InvalidOperationException>(() => ProjectZoneService.Delete(fixture.Project, fixture.SourceZone.Id));

                Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed stored ZoneId changed project version.");
                Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed stored ZoneId changed project timestamp.");
                Equal(malformed, fixture.Element.ZoneId, "Rejected malformed stored ZoneId was normalized or overwritten.");
                Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Rejected malformed stored ZoneId dirtied the element.");
            }
        }

        private static void PaddedProjectElementIdFailsBeforeMutation()
        {
            var fixture = NewFixture("element-id");
            SetPrivateField(fixture.Element, "<Id>k__BackingField", " element-1 ");
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(fixture.Project, fixture.SourceZone.Id));

            Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed element id changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed element id changed project timestamp.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Malformed element id dirtied the element.");
        }

        private static void PaddedActiveZoneReferenceFailsBeforeMutation()
        {
            var fixture = NewFixture("active");
            SetPrivateField(fixture.Project, "_activeZoneId", " zone-source ");
            SetPrivateField(fixture.Element, "_zoneId", string.Empty);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(fixture.Project, fixture.SourceZone.Id));

            Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed ActiveZoneId changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed ActiveZoneId changed project timestamp.");
            True(fixture.Project.Zones.Contains(fixture.SourceZone), "Malformed ActiveZoneId allowed Zone deletion.");
        }

        private static Fixture NewFixture(string suffix)
        {
            var project = new ProjectState("zone-canonical-" + suffix, "Zone canonical " + suffix);
            var sourceZone = new ZoneDefinition("zone-source", "Source zone");
            var targetZone = new ZoneDefinition("zone-target", "Target zone");
            project.Zones.Add(sourceZone);
            project.Zones.Add(targetZone);
            project.ActiveZoneId = targetZone.Id;
            var element = new ProjectElement("element-1", ElementCategory.Beam, string.Empty, string.Empty, sourceZone.Id);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Fixture(project, sourceZone, targetZone, element);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception(instance.GetType().Name + "." + fieldName + " field was not found.");
            field.SetValue(instance, value);
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

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ZoneDefinition sourceZone, ZoneDefinition targetZone, ProjectElement element)
            {
                Project = project;
                SourceZone = sourceZone;
                TargetZone = targetZone;
                Element = element;
            }

            public ProjectState Project { get; }
            public ZoneDefinition SourceZone { get; }
            public ZoneDefinition TargetZone { get; }
            public ProjectElement Element { get; }
        }
    }
}
