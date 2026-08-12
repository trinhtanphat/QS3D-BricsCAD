using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbDrawingFingerprintCanonicalitySmoke
    {
        internal static void Run()
        {
            RejectsPaddedProjectDrawingFingerprint();
            RejectsPaddedElementDrawingFingerprint();
            AcceptsCanonicalDrawingFingerprints();
        }

        private static void RejectsPaddedProjectDrawingFingerprint()
        {
            WithSavedProject((store, path) =>
            {
                var document = XDocument.Load(path);
                document.Root!.SetAttributeValue("drawingFingerprint", " DWG-ROOT ");
                document.Save(path, SaveOptions.DisableFormatting);

                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static void RejectsPaddedElementDrawingFingerprint()
        {
            WithSavedProject((store, path) =>
            {
                var document = XDocument.Load(path);
                var element = document.Root!.Element("elements")!.Element("element")!;
                element.SetAttributeValue("drawingFingerprint", " DWG-ELEMENT ");
                document.Save(path, SaveOptions.DisableFormatting);

                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static void AcceptsCanonicalDrawingFingerprints()
        {
            WithSavedProject((store, path) =>
            {
                var loaded = store.Load(path);
                Require(loaded.DrawingFingerprint == "DWG-ROOT", "Canonical project drawing fingerprint did not round-trip.");
                Require(loaded.Elements.Count == 1, "Canonical project did not preserve its element.");
                Require(loaded.Elements[0].DrawingFingerprint == "DWG-ELEMENT", "Canonical element drawing fingerprint did not round-trip.");
            });
        }

        private static void WithSavedProject(Action<QsdbProjectStore, string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-fingerprint-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");

            try
            {
                var project = new ProjectState("qsdb-fingerprint", "QSDB fingerprint smoke")
                {
                    DrawingFingerprint = "DWG-ROOT"
                };
                project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam)
                {
                    DrawingFingerprint = "DWG-ELEMENT"
                });

                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                assertion(store, path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
