using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbProjectSchemaRegressionSmoke
    {
        private const string Timestamp = "2026-08-11T00:00:00.0000000Z";

        internal static void Run()
        {
            ValidCurrentProjectLoads();
            LegacyV1StillMigrates();
            RejectsNonCanonicalRootName();
            RejectsForeignNamespace();
            RejectsUnknownRootAttribute();
            RejectsUnknownChild();
            RejectsDuplicateNestedContainer();
            RejectsNamespacedAttribute();
            RejectsMixedTextContent();
        }

        private static void ValidCurrentProjectLoads()
        {
            var project = Load(Current());
            Equal("P", project.ProjectId);
            Equal("Project", project.Name);
            Equal(ProjectState.CurrentSchemaVersion, project.SchemaVersion);
            Equal(0L, project.ChangeVersion);
        }

        private static void LegacyV1StillMigrates()
        {
            var project = Load("<qs3d schema='1' projectId='P1' name='Legacy'><zones/><floors/><families/><elements/></qs3d>");
            Equal("P1", project.ProjectId);
            Equal(ProjectState.CurrentSchemaVersion, project.SchemaVersion);
            Equal("1", project.Metadata["QS3D.SchemaMigratedFrom"]);
        }

        private static void RejectsNonCanonicalRootName() => Reject(Current("Qs3D"));

        private static void RejectsForeignNamespace() => Reject(
            "<qs3d xmlns='urn:qs3d:future' schema='3' projectId='P' name='Project' updatedUtc='" + Timestamp + "' changeVersion='0'><metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>");

        private static void RejectsUnknownRootAttribute() => Reject(
            Current(extraAttributes: " future='1'"));

        private static void RejectsUnknownChild() => Reject(
            Current(extraContent: "<future/>"));

        private static void RejectsDuplicateNestedContainer() => Reject(
            Current(elementsContent:
                "<element id='E' category='Wall' familyId='' floorId='' zoneId='' drawingFingerprint='' dirty='0' updatedUtc='" + Timestamp + "'><properties/><properties/></element>"));

        private static void RejectsNamespacedAttribute() => Reject(
            Current(extraAttributes: " xmlns:f='urn:future' f:value='1'"));

        private static void RejectsMixedTextContent() => Reject(
            Current(extraContent: "future"));

        private static string Current(
            string rootName = "qs3d",
            string extraAttributes = "",
            string extraContent = "",
            string elementsContent = "")
        {
            return "<" + rootName + " schema='3' projectId='P' name='Project' updatedUtc='" + Timestamp + "' changeVersion='0'" + extraAttributes + ">" +
                   "<metadata/><zones/><floors/><families/><rules/><elements>" + elementsContent + "</elements><audit/>" + extraContent +
                   "</" + rootName + ">";
        }

        private static ProjectState Load(string xml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-project-schema-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(path, xml);
                return new QsdbProjectStore().Load(path);
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
            if (!failed) throw new Exception("Malformed or forward-unknown QSDB XML must fail closed.");
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

    internal static class QsdbProjectSchemaRegressionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbProjectSchemaRegressionSmoke.Run();
    }
}
