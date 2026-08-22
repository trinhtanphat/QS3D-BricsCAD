using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityReviewIsImmutableAndRoundTrips();
            RegenerationReviewKeepsSubsetScope();
            TamperedReviewFailsClosed();
            HandleFieldInjectionFailsClosed();
        }

        private static void QuantityReviewIsImmutableAndRoundTrips()
        {
            var project = RuleFixture();
            var preview = new QuantityRulePreviewService().PreviewProject(project);
            var service = new PreviewReviewSnapshotService();
            var snapshot = service.Create("Cost review", preview);

            Equal(PreviewReviewKind.QuantityRule, snapshot.Kind);
            Equal("Project", snapshot.Scope);
            Equal(project.ProjectId, snapshot.ProjectId);
            Equal(project.ChangeVersion, snapshot.SourceChangeVersion);
            Equal(1, snapshot.ChangedElementCount);
            Equal(1, snapshot.Entries.Count);
            Equal("Quantity:Cost", snapshot.Entries[0].Field);
            Equal("6", snapshot.Entries[0].After);
            Equal(64, snapshot.Fingerprint.Length);
            True(service.Verify(snapshot));
            True(snapshot.Entries.All(x => x.Field.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0));
            True(!project.FindElement("E1")!.Quantities.ContainsKey("Cost"));

            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                Equal(snapshot.Fingerprint, loaded.Fingerprint);
                Equal(snapshot.Name, loaded.Name);
                Equal(snapshot.Entries.Count, loaded.Entries.Count);
                Equal(snapshot.Entries[0].AfterProvenance, loaded.Entries[0].AfterProvenance);
                True(service.Verify(loaded));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void RegenerationReviewKeepsSubsetScope()
        {
            var project = RegenFixture();
            var preview = new RegenerationPreviewService().PreviewSubset(project, new[] { "B1" });
            var snapshot = new PreviewReviewSnapshotService().Create("Beam regeneration", preview);

            Equal(PreviewReviewKind.Regeneration, snapshot.Kind);
            Equal("Subset", snapshot.Scope);
            True(snapshot.IsSubset);
            Equal(1, snapshot.TargetElementIds.Count);
            Equal("B1", snapshot.TargetElementIds[0]);
            True(snapshot.RegeneratedElementCount >= 1);
            True(snapshot.Entries.Any(x => x.ElementId == "B1" && x.Field == "Quantity:NetVolumeM3"));
            True(snapshot.Entries.All(x => x.Field.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0));
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void TamperedReviewFailsClosed()
        {
            var snapshot = new PreviewReviewSnapshotService().Create("Cost review", new QuantityRulePreviewService().PreviewProject(RuleFixture()));
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var xml = File.ReadAllText(path);
                if (!xml.Contains("after=\"6\"")) throw new Exception("Expected serialized quantity was not found.");
                File.WriteAllText(path, xml.Replace("after=\"6\"", "after=\"7\""));
                Throws<InvalidDataException>(() => store.Load(path));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void HandleFieldInjectionFailsClosed()
        {
            var snapshot = new PreviewReviewSnapshotService().Create("Cost review", new QuantityRulePreviewService().PreviewProject(RuleFixture()));
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var xml = File.ReadAllText(path);
                if (!xml.Contains("field=\"Quantity:Cost\"")) throw new Exception("Expected serialized field was not found.");
                File.WriteAllText(path, xml.Replace("field=\"Quantity:Cost\"", "field=\"SourceHandles\""));
                Throws<InvalidDataException>(() => store.Load(path));
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static ProjectState RuleFixture()
        {
            var project = new ProjectState("P-REVIEW-RULE", "Rule review");
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
            var project = new ProjectState("P-REVIEW-REGEN", "Regen review");
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

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "qs3d-preview-review-" + Guid.NewGuid().ToString("N") + ".xml");

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
