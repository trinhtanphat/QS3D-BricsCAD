using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewKindParsingSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericKindsFailAtParseBoundary();
        }

        private static void NumericKindsFailAtParseBoundary()
        {
            var project = new ProjectState("P-REVIEW-KIND", "Review kind parsing");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));
            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM", "F", "Z");
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            project.Elements.Add(element);

            var snapshot = new PreviewReviewSnapshotService().Create(
                "Kind parsing",
                new QuantityRulePreviewService().PreviewProject(project));
            var path = Path.Combine(Path.GetTempPath(), "qs3d-preview-review-kind-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var xml = File.ReadAllText(path);
                if (!xml.Contains("kind=\"QuantityRule\""))
                    throw new Exception("Expected symbolic preview review kind was not serialized.");

                AssertKindRejectedAtParseBoundary(store, path, xml, "0");
                AssertKindRejectedAtParseBoundary(store, path, xml, "2");
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void AssertKindRejectedAtParseBoundary(PreviewReviewSnapshotStore store, string path, string originalXml, string kind)
        {
            File.WriteAllText(path, originalXml.Replace("kind=\"QuantityRule\"", "kind=\"" + kind + "\""));
            try
            {
                store.Load(path);
            }
            catch (InvalidDataException ex)
            {
                if (!string.Equals(ex.Message, "Invalid preview review kind.", StringComparison.Ordinal))
                    throw new Exception("Preview review kind was rejected after the parse boundary instead of at it: " + ex.Message);
                return;
            }
            throw new Exception("Numeric preview review kind was accepted: " + kind + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
