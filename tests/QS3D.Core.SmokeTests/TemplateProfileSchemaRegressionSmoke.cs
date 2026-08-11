using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileSchemaRegressionSmoke
    {
        internal static void Run()
        {
            ValidMinimalTemplateLoads();
            RejectsForeignNamespace();
            RejectsUnknownRootAttribute();
            RejectsUnknownChild();
            RejectsDuplicateRootSingleton();
            RejectsDuplicateFamilyProperties();
        }

        private static void ValidMinimalTemplateLoads()
        {
            var profile = Load("<qs3dTemplate schema='1' id='T' name='Template'/>");
            Equal("T", profile.Id);
            Equal("Template", profile.Name);
        }

        private static void RejectsForeignNamespace() => Reject(
            "<qs3dTemplate xmlns='urn:qs3d:future' schema='1' id='T' name='Template'><families/></qs3dTemplate>");

        private static void RejectsUnknownRootAttribute() => Reject(
            "<qs3dTemplate schema='1' id='T' name='Template' future='1'><families/></qs3dTemplate>");

        private static void RejectsUnknownChild() => Reject(
            "<qs3dTemplate schema='1' id='T' name='Template'><families/><future/></qs3dTemplate>");

        private static void RejectsDuplicateRootSingleton() => Reject(
            "<qs3dTemplate schema='1' id='T' name='Template'><families/><families/></qs3dTemplate>");

        private static void RejectsDuplicateFamilyProperties() => Reject(
            "<qs3dTemplate schema='1' id='T' name='Template'><families><family id='F' name='Family' category='Beam'><properties/><properties/></family></families></qs3dTemplate>");

        private static TemplateProfile Load(string xml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-template-schema-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(path, xml);
                return new TemplateProfileStore().Load(path);
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
            if (!failed) throw new Exception("Malformed or forward-unknown template XML must fail closed.");
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

    internal static class TemplateProfileSchemaRegressionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateProfileSchemaRegressionSmoke.Run();
    }
}
