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
            InvalidProjectPayloadFailsAtCaptureBoundary();
            ValidUnicodeIsPreservedExactly();
        }

        private static void InvalidRevisionIdFailsAtCaptureBoundary()
        {
            var service = new RevisionService();
            var project = new ProjectState("revision-xml-integrity", "Revision XML Integrity");
            Throws<ArgumentException>(() => service.Capture(project, "REV-\u0001-A"));
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

            Throws<InvalidOperationException>(() => service.Capture(ProjectWithElement(invalidIdElement), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.FamilyId = "F-\u0001-1"), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.FloorId = "L-\u0001-1"), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.ZoneId = "Z-\u0001-1"), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.Properties["P-\u0001-1"] = "ok"), "REV-XML"));
            Throws<InvalidOperationException>(() => service.Capture(ProjectWithMutation(x => x.SetProperty("Note", "bad-\u0001-value")), "REV-XML"));
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
