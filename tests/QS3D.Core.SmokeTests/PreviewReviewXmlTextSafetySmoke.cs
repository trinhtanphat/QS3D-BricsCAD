using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewXmlTextSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidReviewNameFailsBeforeSnapshot();
            InvalidRuleProvenanceFailsBeforeSnapshot();
            XmlValidWhitespaceRoundTripsExactly();
        }

        private static void InvalidReviewNameFailsBeforeSnapshot()
        {
            var preview = new QuantityRulePreviewService().PreviewProject(CreateProject("cost", "1"));
            ExpectInvalidOperation(
                () => new PreviewReviewSnapshotService().Create("Review\u0001Name", preview),
                "invalid in XML");
        }

        private static void InvalidRuleProvenanceFailsBeforeSnapshot()
        {
            var preview = new QuantityRulePreviewService().PreviewProject(CreateProject("cost-rule", "1"));
            if (preview.Elements.Count != 1 || preview.Elements[0].Changes.Count != 1 ||
                !string.Equals(preview.Elements[0].Changes[0].AfterProvenance, "cost-rule@1", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected real Quantity Rule provenance before corruption.");

            var change = preview.Elements[0].Changes[0];
            var provenanceField = typeof(QuantityRulePreviewChange).GetField("<AfterProvenance>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("QuantityRulePreviewChange AfterProvenance backing field changed; update corruption regression intentionally.");
            provenanceField.SetValue(change, "cost\u0001rule@1");
            if (preview.Elements.Count != 1 || preview.Elements[0].Changes.Count != 1 ||
                preview.Elements[0].Changes[0].AfterProvenance.IndexOf('\u0001') < 0)
                throw new InvalidOperationException("Expected invalid XML character to reach Preview Review provenance input.");

            ExpectInvalidOperation(
                () => new PreviewReviewSnapshotService().Create("Provenance review", preview),
                "invalid in XML");
        }

        private static void XmlValidWhitespaceRoundTripsExactly()
        {
            const string name = "Review\tName";
            const string ruleId = "cost\tline";
            var snapshot = new PreviewReviewSnapshotService().Create(
                name,
                new QuantityRulePreviewService().PreviewProject(CreateProject(ruleId, "1")));
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                if (!string.Equals(loaded.Name, name, StringComparison.Ordinal))
                    throw new InvalidOperationException("Preview Review changed XML-valid tab content in the review name.");
                if (loaded.Entries.Count != 1 ||
                    !string.Equals(loaded.Entries[0].AfterProvenance, ruleId + "@1", StringComparison.Ordinal))
                    throw new InvalidOperationException("Preview Review changed XML-valid tab content in rule provenance.");
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static ProjectState CreateProject(string ruleId, string version)
        {
            var project = new ProjectState("preview-review-xml-text", "Preview Review XML text");
            project.QuantityRules.Add(new QuantityRule(ruleId, ElementCategory.Beam, "Cost", "LengthM*Rate", version));
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            project.Elements.Add(element);
            return project;
        }

        private static string TempPath() =>
            Path.Combine(Path.GetTempPath(), "qs3d-preview-review-xml-text-" + Guid.NewGuid().ToString("N") + ".xml");

        private static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(
                    "Preview Review rejected XML-invalid text for an unexpected reason: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException("Expected Preview Review XML-invalid text to fail closed.");
        }
    }
}
