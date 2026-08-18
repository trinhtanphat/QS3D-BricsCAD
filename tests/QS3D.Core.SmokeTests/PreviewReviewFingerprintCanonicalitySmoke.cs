using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewFingerprintCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalFingerprintRoundTrips();
            UppercaseFingerprintFailsClosed();
            MixedCaseFingerprintFailsClosed();
        }

        private static void CanonicalFingerprintRoundTrips()
        {
            var snapshot = Snapshot();
            if (snapshot.Fingerprint != snapshot.Fingerprint.ToLowerInvariant())
                throw new Exception("Preview review fingerprints must be emitted in lowercase canonical form.");

            WithPersistedSnapshot(snapshot, (store, path) =>
            {
                var loaded = store.Load(path);
                if (!string.Equals(snapshot.Fingerprint, loaded.Fingerprint, StringComparison.Ordinal))
                    throw new Exception("Canonical preview review fingerprint did not round-trip exactly.");
                if (!new PreviewReviewSnapshotService().Verify(loaded))
                    throw new Exception("Canonical preview review snapshot did not verify.");
            });
        }

        private static void UppercaseFingerprintFailsClosed()
        {
            AssertPersistedFingerprintRejected(value => value.ToUpperInvariant());
        }

        private static void MixedCaseFingerprintFailsClosed()
        {
            AssertPersistedFingerprintRejected(value =>
            {
                var chars = value.ToCharArray();
                for (var i = 0; i < chars.Length; i++)
                {
                    if (chars[i] >= 'a' && chars[i] <= 'f')
                    {
                        chars[i] = char.ToUpperInvariant(chars[i]);
                        break;
                    }
                }
                return new string(chars);
            });
        }

        private static void AssertPersistedFingerprintRejected(Func<string, string> mutate)
        {
            var snapshot = Snapshot();
            WithPersistedSnapshot(snapshot, (store, path) =>
            {
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("Expected preview review root.");
                var canonical = root.Attribute("fingerprint")?.Value ?? throw new Exception("Expected preview review fingerprint.");
                var mutated = mutate(canonical);
                if (string.Equals(canonical, mutated, StringComparison.Ordinal))
                    throw new Exception("Fingerprint mutation did not change canonical text.");
                root.SetAttributeValue("fingerprint", mutated);
                document.Save(path, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static PreviewReviewSnapshot Snapshot()
        {
            var project = new ProjectState("P-REVIEW-FINGERPRINT", "Fingerprint review");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));
            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM", "F", "Z");
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            project.Elements.Add(element);
            var preview = new QuantityRulePreviewService().PreviewProject(project);
            return new PreviewReviewSnapshotService().Create("Fingerprint review", preview);
        }

        private static void WithPersistedSnapshot(PreviewReviewSnapshot snapshot, Action<PreviewReviewSnapshotStore, string> assertion)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-preview-review-fingerprint-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                assertion(store, path);
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
