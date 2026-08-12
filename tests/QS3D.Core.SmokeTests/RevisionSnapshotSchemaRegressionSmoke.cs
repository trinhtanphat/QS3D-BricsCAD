using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotSchemaRegressionSmoke
    {
        private const string CanonicalCreatedUtc = "2026-08-11T00:00:00.0000000Z";

        internal static void Run()
        {
            ValidMinimalRevisionLoads();
            RejectsForeignNamespace();
            RejectsUnknownRootAttribute();
            RejectsUnknownChild();
            RejectsDuplicateElementsContainer();
            RejectsDuplicateNestedContainer();
            RejectsMixedTextContent();
        }

        private static void ValidMinimalRevisionLoads()
        {
            var snapshot = Load(Root("<elements/>"));
            Equal("R", snapshot.Id);
            Equal(0, snapshot.Elements.Count);
        }

        private static void RejectsForeignNamespace() => Reject(
            "<qs3dRevision xmlns='urn:qs3d:future' id='R' createdUtc='" + CanonicalCreatedUtc + "'><elements/></qs3dRevision>");

        private static void RejectsUnknownRootAttribute() => Reject(
            "<qs3dRevision id='R' createdUtc='" + CanonicalCreatedUtc + "' future='1'><elements/></qs3dRevision>");

        private static void RejectsUnknownChild() => Reject(
            Root("<future/><elements/>"));

        private static void RejectsDuplicateElementsContainer() => Reject(
            Root("<elements/><elements/>"));

        private static void RejectsDuplicateNestedContainer() => Reject(
            Root("<elements><element id='E' category='Beam' familyId='' floorId='' zoneId=''>" +
                 "<properties/><properties/><quantities/><sourceHandles/><dependencies/>" +
                 "</element></elements>"));

        private static void RejectsMixedTextContent() => Reject(
            Root("future<elements/>"));

        private static string Root(string content) =>
            "<qs3dRevision id='R' createdUtc='" + CanonicalCreatedUtc + "'>" + content + "</qs3dRevision>";

        private static RevisionSnapshot Load(string xml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-revision-schema-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(path, xml);
                return new RevisionSnapshotStore().Load(path);
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void Reject(string xml)
        {
            var failed = false;
            try { Load(xml); }
            catch (InvalidDataException) { failed = true; }
            if (!failed) throw new Exception("Malformed or forward-unknown revision XML must fail closed.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }

    internal static class RevisionSnapshotSchemaRegressionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionSnapshotSchemaRegressionSmoke.Run();
    }
}
