using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPublicationSchemaValidationSmoke
    {
        internal static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-publication-schema-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("publication-schema-project", "Publication schema validation"), path);
                InvokePublicationGate(path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new InvalidOperationException("QSDB fixture has no root element.");
                root.SetAttributeValue("unsupportedPublicationShape", "must-fail");
                document.Save(path, SaveOptions.DisableFormatting);

                ThrowsInvalidData(() => InvokePublicationGate(path));
                ThrowsInvalidData(() => store.Load(path));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
                SafeDelete(path + ".tmp");
            }
        }

        private static void InvokePublicationGate(string path)
        {
            var method = typeof(QsdbProjectStore).GetMethod("ValidateSerializedFile", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("QSDB publication validation gate was not found.");
            try
            {
                method.Invoke(null, new object[] { path });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ThrowsInvalidData(Action action)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("Expected InvalidDataException.");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
