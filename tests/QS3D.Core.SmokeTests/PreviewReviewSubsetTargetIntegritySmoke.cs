using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewSubsetTargetIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ValidSubsetRoundTrips();
            OutsideSubsetTargetFailsClosedBeforeFingerprintValidation();
        }

        private static void ValidSubsetRoundTrips()
        {
            var snapshot = CreateSubsetSnapshot();
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                Equal("Subset", loaded.Scope);
                Equal(1, loaded.TargetElementIds.Count);
                Equal("B1", loaded.TargetElementIds[0]);
                True(loaded.Entries.All(x => string.Equals(x.ElementId, "B1", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void OutsideSubsetTargetFailsClosedBeforeFingerprintValidation()
        {
            var snapshot = CreateSubsetSnapshot();
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);

                var document = XDocument.Load(path);
                var entry = document.Root?.Element("entries")?.Elements("entry").FirstOrDefault()
                    ?? throw new Exception("Expected a serialized subset review entry.");
                entry.SetAttributeValue("elementId", "B2");
                document.Save(path, SaveOptions.DisableFormatting);

                try
                {
                    store.Load(path);
                }
                catch (InvalidDataException ex)
                {
                    if (ex.InnerException is InvalidOperationException inner &&
                        inner.Message.IndexOf("outside the reviewed target set", StringComparison.OrdinalIgnoreCase) >= 0)
                        return;

                    throw new Exception("Expected subset target semantic rejection before fingerprint validation, got: " + ex.Message, ex);
                }

                throw new Exception("Expected invalid subset review row to fail closed.");
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static PreviewReviewSnapshot CreateSubsetSnapshot()
        {
            var project = new ProjectState("P-REVIEW-SUBSET-INTEGRITY", "Review subset integrity");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));

            var beam = new ProjectElement("B1", ElementCategory.Beam, "FAM", "F", "Z");
            beam.Properties["LengthM"] = "6";
            beam.Properties["WidthM"] = "0.3";
            beam.Properties["HeightM"] = "0.5";
            beam.SourceHandles.Add("ABC123");
            project.Elements.Add(beam);

            var preview = new RegenerationPreviewService().PreviewSubset(project, new[] { "B1" });
            var snapshot = new PreviewReviewSnapshotService().Create("Subset integrity", preview);
            if (snapshot.Entries.Count == 0) throw new Exception("Expected subset review entries.");
            return snapshot;
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "qs3d-preview-review-subset-integrity-" + Guid.NewGuid().ToString("N") + ".xml");

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
