using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewKindInvariantSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ValidKindSpecificSnapshotsStillRoundTrip();
            QuantityRuleRegenerationCounterForgeryFailsClosed();
            QuantityRuleFieldForgeryFailsClosed();
            RegenerationCategoryForgeryFailsClosed();
            RegenerationProvenanceForgeryFailsClosed();
        }

        private static void ValidKindSpecificSnapshotsStillRoundTrip()
        {
            var service = new PreviewReviewSnapshotService();
            RoundTrip(service.Create("Quantity review", new QuantityRulePreviewService().PreviewProject(RuleFixture())));
            RoundTrip(service.Create("Regeneration review", new RegenerationPreviewService().PreviewSubset(RegenFixture(), new[] { "B1" })));
        }

        private static void QuantityRuleRegenerationCounterForgeryFailsClosed()
        {
            AssertForgedSnapshotRejected(
                new PreviewReviewSnapshotService().Create("Quantity review", new QuantityRulePreviewService().PreviewProject(RuleFixture())),
                document => document.Root!.SetAttributeValue("regeneratedElementCount", "1"));
        }

        private static void QuantityRuleFieldForgeryFailsClosed()
        {
            AssertForgedSnapshotRejected(
                new PreviewReviewSnapshotService().Create("Quantity review", new QuantityRulePreviewService().PreviewProject(RuleFixture())),
                document => FirstEntry(document).SetAttributeValue("field", "Property:WidthM"));
        }

        private static void RegenerationCategoryForgeryFailsClosed()
        {
            AssertForgedSnapshotRejected(
                new PreviewReviewSnapshotService().Create("Regeneration review", new RegenerationPreviewService().PreviewSubset(RegenFixture(), new[] { "B1" })),
                document => FirstEntry(document).SetAttributeValue("category", "Beam"));
        }

        private static void RegenerationProvenanceForgeryFailsClosed()
        {
            AssertForgedSnapshotRejected(
                new PreviewReviewSnapshotService().Create("Regeneration review", new RegenerationPreviewService().PreviewSubset(RegenFixture(), new[] { "B1" })),
                document => FirstEntry(document).SetAttributeValue("beforeProvenance", "QuantityRule:forged"));
        }

        private static void AssertForgedSnapshotRejected(PreviewReviewSnapshot snapshot, Action<XDocument> mutate)
        {
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var document = XDocument.Load(path);
                mutate(document);
                RewriteFingerprint(document);
                document.Save(path, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(path));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void RoundTrip(PreviewReviewSnapshot snapshot)
        {
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                Equal(snapshot.Kind, loaded.Kind);
                Equal(snapshot.Fingerprint, loaded.Fingerprint);
                True(new PreviewReviewSnapshotService().Verify(loaded));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static XElement FirstEntry(XDocument document)
        {
            return document.Root?.Element("entries")?.Elements("entry").FirstOrDefault()
                ?? throw new Exception("Expected serialized preview review entry was not found.");
        }

        private static void RewriteFingerprint(XDocument document)
        {
            var root = document.Root ?? throw new Exception("Preview review document has no root.");
            var sb = new StringBuilder(4096);
            Part(sb, Attribute(root, "format"));
            Part(sb, Attribute(root, "formatVersion"));
            Part(sb, Attribute(root, "name"));
            Part(sb, Attribute(root, "projectId"));
            Part(sb, Attribute(root, "kind"));
            Part(sb, Attribute(root, "sourceChangeVersion"));
            Part(sb, Attribute(root, "scope"));
            Part(sb, Attribute(root, "changedElementCount"));
            Part(sb, Attribute(root, "regeneratedElementCount"));
            Part(sb, Attribute(root, "newHealthIssueCount"));
            Part(sb, Attribute(root, "newHealthErrorCount"));
            Part(sb, Attribute(root, "resolvedHealthIssueCount"));
            Part(sb, Attribute(root, "omittedHandleFieldCount"));

            foreach (var target in root.Element("targets")!.Elements("target")
                         .Select(x => Attribute(x, "id"))
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                Part(sb, target);

            foreach (var entry in root.Element("entries")!.Elements("entry")
                         .OrderBy(x => Attribute(x, "elementId"), StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => Attribute(x, "field"), StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => Attribute(x, "change"), StringComparer.Ordinal)
                         .ThenBy(x => Attribute(x, "before"), StringComparer.Ordinal)
                         .ThenBy(x => Attribute(x, "after"), StringComparer.Ordinal))
            {
                Part(sb, Attribute(entry, "elementId"));
                Part(sb, Attribute(entry, "category"));
                Part(sb, Attribute(entry, "change"));
                Part(sb, Attribute(entry, "field"));
                Part(sb, Attribute(entry, "before"));
                Part(sb, Attribute(entry, "after"));
                Part(sb, Attribute(entry, "beforeProvenance"));
                Part(sb, Attribute(entry, "afterProvenance"));
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                root.SetAttributeValue("fingerprint", BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant());
            }
        }

        private static string Attribute(XElement element, string name)
        {
            return element.Attribute(name)?.Value ?? throw new Exception("Missing serialized preview review attribute: " + name + ".");
        }

        private static void Part(StringBuilder sb, string value)
        {
            sb.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
        }

        private static ProjectState RuleFixture()
        {
            var project = new ProjectState("P-REVIEW-KIND-RULE", "Rule review");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));
            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM", "F", "Z");
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState RegenFixture()
        {
            var project = new ProjectState("P-REVIEW-KIND-REGEN", "Regen review");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));
            var beam = new ProjectElement("B1", ElementCategory.Beam, "FAM", "F", "Z");
            beam.Properties["LengthM"] = "6";
            beam.Properties["WidthM"] = "0.3";
            beam.Properties["HeightM"] = "0.5";
            beam.SourceHandles.Add("ABC123");
            project.Elements.Add(beam);
            return project;
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "qs3d-preview-review-kind-" + Guid.NewGuid().ToString("N") + ".xml");

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

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
