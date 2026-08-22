using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionCaptureXmlTextIntegritySmoke
    {
        internal static void Run()
        {
            InvalidRevisionIdFailsAtCaptureBoundary();
            InvalidPropertyValueFailsAtMutationBoundary();
            InvalidProjectPayloadFailsAtCaptureBoundary();
            ValidUnicodeIsPreservedExactly();
        }

        private static void InvalidRevisionIdFailsAtCaptureBoundary()
        {
            var service = new RevisionService();
            var project = new ProjectState("revision-xml-integrity", "Revision XML Integrity");
            Throws<ArgumentException>(() => service.Capture(project, "REV-\u0001-A"));
        }

        private static void InvalidPropertyValueFailsAtMutationBoundary()
        {
            var element = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            var updatedUtc = element.UpdatedUtc;
            var dirty = element.Dirty;

            Throws<ArgumentException>(() => element.SetProperty("Note", "bad-\u0001-value"));

            if (element.Properties.ContainsKey("Note"))
                throw new Exception("Invalid XML property value was retained after SetProperty rejection.");
            Equal(updatedUtc, element.UpdatedUtc);
            Equal(dirty, element.Dirty);
        }

        private static void InvalidProjectPayloadFailsAtCaptureBoundary()
        {
            var service = new RevisionService();

            var invalidIdElement = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            Equal("E-1", invalidIdElement.Id);
            var idField = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement Id backing field was not found.");
            idField.SetValue(invalidIdElement, "E-\u0001-1");
            Equal("E-\u0001-1", invalidIdElement.Id);

            var invalidFamilyElement = new ProjectElement("E-1", ElementCategory.ArchitecturalWall) { FamilyId = "F-1" };
            Equal("F-1", invalidFamilyElement.FamilyId);
            SetRawRelation(invalidFamilyElement, "_familyId", "F-\u0001-1");
            Equal("F-\u0001-1", invalidFamilyElement.FamilyId);

            var invalidFloorElement = new ProjectElement("E-1", ElementCategory.ArchitecturalWall) { FloorId = "L-1" };
            Equal("L-1", invalidFloorElement.FloorId);
            SetRawRelation(invalidFloorElement, "_floorId", "L-\u0001-1");
            Equal("L-\u0001-1", invalidFloorElement.FloorId);

            var invalidZoneElement = new ProjectElement("E-1", ElementCategory.ArchitecturalWall) { ZoneId = "Z-1" };
            Equal("Z-1", invalidZoneElement.ZoneId);
            SetRawRelation(invalidZoneElement, "_zoneId", "Z-\u0001-1");
            Equal("Z-\u0001-1", invalidZoneElement.ZoneId);

            Throws<InvalidOperationException>(() => service.Capture(ProjectWithElement(invalidIdElement), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithElement(invalidFamilyElement), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithElement(invalidFloorElement), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithElement(invalidZoneElement), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.Properties["P-\u0001-1"] = "ok"), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.Quantities["Q-\u0001-1"] = 1d), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.SourceHandles.Add("H-\u0001-1")), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.DependsOn.Add("D-\u0001-1")), "REV-XML"));
        }

        private static void ValidUnicodeIsPreservedExactly()
        {
            const string revisionId = "REV-\u0110-\uD83D\uDE80";
            const string propertyName = "T\u00EAn";
            const string propertyValue = "\u0110\u00E0 N\u1EB5ng \uD83D\uDE80";
            var project = ProjectWithMutation(x => x.SetProperty(propertyName, propertyValue));

            var snapshot = new RevisionService().Capture(project, revisionId);

            Equal(revisionId, snapshot.Id);
            Equal(1, snapshot.Elements.Count);
            Equal(propertyValue, snapshot.Elements[0].Properties[propertyName]);
        }

        private static ProjectState ProjectWithMutation(Action<ProjectElement> mutate)
        {
            var element = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            mutate(element);
            return ProjectWithElement(element);
        }

        private static ProjectState ProjectWithElement(ProjectElement element)
        {
            var project = new ProjectState("revision-xml-integrity", "Revision XML Integrity");
            project.Elements.Add(element);
            return project;
        }

        private static void SetRawRelation(ProjectElement element, string fieldName, string value)
        {
            var field = typeof(ProjectElement).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement relation field " + fieldName + " was not found.");
            Equal(typeof(string), field.FieldType);
            field.SetValue(element, value);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RevisionCaptureXmlTextIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionCaptureXmlTextIntegritySmoke.Run();
    }
}
