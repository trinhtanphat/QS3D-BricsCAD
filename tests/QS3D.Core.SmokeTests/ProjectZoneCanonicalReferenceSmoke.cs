using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCanonicalReferenceSmoke
    {
        public static void Run()
        {
            CanonicalCaseInsensitiveReferenceStillWorks();
            PaddedZoneIdentityParametersFailBeforeMutation();
            PaddedStoredZoneReferenceFailsBeforeMutation();
            PaddedProjectElementIdFailsBeforeMutation();
            PaddedActiveZoneReferenceFailsDeleteBeforeMutation();
        }

        private static void CanonicalCaseInsensitiveReferenceStillWorks()
        {
            var fixture = NewFixture("canonical");
            SetRawZoneId(fixture.Element, "ZONE-SOURCE");
            Equal(1, ProjectZoneService.ReferenceCount(fixture.Project, "zone-source"), "Canonical case-insensitive Zone reference was not resolved.");
            var beforeVersion = fixture.Project.ChangeVersion;
            var changed = ProjectZoneService.Assign(fixture.Project, "ZONE-TARGET", new[] { fixture.Element });
            Equal(1, changed, "Canonical case-insensitive Zone assignment did not report one change.");
            Equal(beforeVersion + 1L, fixture.Project.ChangeVersion, "Canonical Zone assignment did not touch the project exactly once.");
            Equal(fixture.TargetZone.Id, fixture.Element.ZoneId, "Canonical Zone assignment did not store the canonical target id.");
        }

        private static void PaddedZoneIdentityParametersFailBeforeMutation()
        {
            var fixture = NewFixture("identity-parameter");
            fixture.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            var beforeZoneCount = fixture.Project.Zones.Count;
            var beforeActiveZoneId = fixture.Project.ActiveZoneId;
            var beforeElementZoneId = fixture.Element.ZoneId;
            Throws<ArgumentException>(() => ProjectZoneService.Create(fixture.Project, " zone-new ", "New zone"));
            Throws<ArgumentException>(() => ProjectZoneService.Update(fixture.Project, " zone-source ", "Renamed"));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(fixture.Project, " zone-source "));
            Throws<ArgumentException>(() => ProjectZoneService.Assign(fixture.Project, " zone-target ", new[] { fixture.Element }));
            Throws<ArgumentException>(() => ProjectZoneService.Delete(fixture.Project, " zone-source "));
            Throws<ArgumentException>(() => ProjectZoneService.ReferenceCount(fixture.Project, " zone-source "));
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Rejected padded Zone identity parameter changed project version.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Rejected padded Zone identity parameter changed project timestamp.");
            Equal(beforeZoneCount, fixture.Project.Zones.Count, "Rejected padded Zone identity parameter changed Zone collection.");
            Equal(beforeActiveZoneId, fixture.Project.ActiveZoneId, "Rejected padded Zone identity parameter changed active Zone.");
            Equal(beforeElementZoneId, fixture.Element.ZoneId, "Rejected padded Zone identity parameter changed element Zone reference.");
            Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Rejected padded Zone identity parameter dirtied the element.");
        }

        private static void PaddedStoredZoneReferenceFailsBeforeMutation()
        {
            foreach (var malformed in new[] { " zone-source", "zone-source ", " zone-source ", "\tzone-source" })
            {
                var fixture = NewFixture("stored-ref");
                SetRawZoneId(fixture.Element, malformed);
                fixture.Element.MarkClean(ElementDirtyFlags.All);
                var beforeVersion = fixture.Project.ChangeVersion;
                var beforeUtc = fixture.Project.UpdatedUtc;
                Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(fixture.Project, fixture.SourceZone.Id));
                Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed stored ZoneId changed project version during ReferenceCount.");
                Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed stored ZoneId changed project timestamp during ReferenceCount.");
                Throws<InvalidOperationException>(() => ProjectZoneService.Assign(fixture.Project, fixture.TargetZone.Id, new[] { fixture.Element }));
                Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed stored ZoneId changed project version during Assign.");
                Equal(malformed, RawZoneId(fixture.Element), "Rejected malformed stored ZoneId was normalized or overwritten.");
                Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Rejected malformed stored ZoneId dirtied the element.");
                Throws<InvalidOperationException>(() => ProjectZoneService.Delete(fixture.Project, fixture.SourceZone.Id));
                Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed stored ZoneId changed project version during Delete.");
                True(fixture.Project.Zones.Contains(fixture.SourceZone), "Malformed stored ZoneId allowed referenced Zone deletion.");
            }
        }

        private static void PaddedProjectElementIdFailsBeforeMutation()
        {
            foreach (var malformed in new[] { " element-1", "element-1 ", " element-1 ", "\telement-1" })
            {
                var fixture = NewFixture("element-id");
                SetRawElementId(fixture.Element, malformed);
                fixture.Element.MarkClean(ElementDirtyFlags.All);
                var beforeVersion = fixture.Project.ChangeVersion;
                var beforeUtc = fixture.Project.UpdatedUtc;
                Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(fixture.Project, fixture.SourceZone.Id));
                Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed project element id changed project version.");
                Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed project element id changed project timestamp.");
                Equal(ElementDirtyFlags.None, fixture.Element.Dirty, "Malformed project element id dirtied the element.");
            }
        }

        private static void PaddedActiveZoneReferenceFailsDeleteBeforeMutation()
        {
            var fixture = NewFixture("active-ref");
            SetRawActiveZoneId(fixture.Project, " zone-source ");
            SetRawZoneId(fixture.Element, string.Empty);
            fixture.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUtc = fixture.Project.UpdatedUtc;
            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(fixture.Project, fixture.SourceZone.Id));
            Equal(beforeVersion, fixture.Project.ChangeVersion, "Malformed ActiveZoneId changed project version during Delete.");
            Equal(beforeUtc, fixture.Project.UpdatedUtc, "Malformed ActiveZoneId changed project timestamp during Delete.");
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

        private static void SetRawZoneId(ProjectElement element, string value)
        {
            var field = typeof(ProjectElement).GetField("_zoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement._zoneId field was not found.");
            field.SetValue(element, value);
        }

        private static string RawZoneId(ProjectElement element)
        {
            var field = typeof(ProjectElement).GetField("_zoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement._zoneId field was not found.");
            return field.GetValue(element) as string ?? throw new Exception("ProjectElement._zoneId was null or not a string.");
        }

        private static void SetRawElementId(ProjectElement element, string value)
        {
            var field = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement.Id backing field was not found.");
            field.SetValue(element, value);
        }

        private static void SetRawActiveZoneId(ProjectState project, string value)
        {
            var field = typeof(ProjectState).GetField("_activeZoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectState._activeZoneId field was not found.");
            field.SetValue(project, value);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, ZoneDefinition sourceZone, ZoneDefinition targetZone, ProjectElement element)
            {
                Project = project; SourceZone = sourceZone; TargetZone = targetZone; Element = element;
            }
            public ProjectState Project { get; }
            public ZoneDefinition SourceZone { get; }
            public ZoneDefinition TargetZone { get; }
            public ProjectElement Element { get; }
        }
    }
}
