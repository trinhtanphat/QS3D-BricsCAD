using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewChangeDomainIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ValidProducerChangesRoundTrip();
            UnsupportedPersistedChangesFailClosed();
        }

        private static void ValidProducerChangesRoundTrip()
        {
            AssertProducerChangeRoundTrips(CreateAddedProject(), "Added");
            AssertProducerChangeRoundTrips(CreateChangedProject(), "Changed");
            AssertProducerChangeRoundTrips(CreateRemovedProject(), "Removed");
        }

        private static void AssertProducerChangeRoundTrips(ProjectState project, string expectedChange)
        {
            var preview = new QuantityRulePreviewService().PreviewProject(project);
            var snapshot = new PreviewReviewSnapshotService().Create("Change domain " + expectedChange, preview);
            if (snapshot.Entries.Count != 1 || !string.Equals(snapshot.Entries[0].Change, expectedChange, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected producer Preview Review change '" + expectedChange + "'.");

            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                if (loaded.Entries.Count != 1 || !string.Equals(loaded.Entries[0].Change, expectedChange, StringComparison.Ordinal))
                    throw new InvalidOperationException("Preview Review change '" + expectedChange + "' did not round-trip.");
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static void UnsupportedPersistedChangesFailClosed()
        {
            AssertPersistedChangeRejected("Renamed", "change is not supported");
            AssertPersistedChangeRejected("added", "change is not supported");
            AssertPersistedChangeRejected(" Added ", "change");
        }

        private static void AssertPersistedChangeRejected(string change, string expectedMessage)
        {
            var snapshot = new PreviewReviewSnapshotService().Create(
                "Change domain invalid",
                new QuantityRulePreviewService().PreviewProject(CreateAddedProject()));
            var path = TempPath();
            try
            {
                var store = new PreviewReviewSnapshotStore();
                store.Save(snapshot, path);
                var document = XDocument.Load(path);
                var entry = document.Root?.Element("entries")?.Elements("entry").SingleOrDefault()
                    ?? throw new InvalidOperationException("Expected one persisted Preview Review entry.");
                entry.SetAttributeValue("change", change);
                document.Save(path, SaveOptions.DisableFormatting);
                ExpectInvalidData(() => store.Load(path), expectedMessage);
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
            }
        }

        private static ProjectState CreateAddedProject()
        {
            var project = BaseProject("preview-change-added");
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));
            project.Elements.Add(BaseElement());
            return project;
        }

        private static ProjectState CreateChangedProject()
        {
            var project = BaseProject("preview-change-changed");
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));
            var element = BaseElement();
            element.Quantities["Cost"] = 1d;
            element.Properties["Rule:Cost"] = "cost@1";
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState CreateRemovedProject()
        {
            var project = BaseProject("preview-change-removed");
            var element = BaseElement();
            element.Quantities["Cost"] = 6d;
            element.Properties["Rule:Cost"] = "retired-cost@1";
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState BaseProject(string id) => new ProjectState(id, "Preview Review change domain");

        private static ProjectElement BaseElement()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            return element;
        }

        private static string TempPath() =>
            Path.Combine(Path.GetTempPath(), "qs3d-preview-review-change-" + Guid.NewGuid().ToString("N") + ".xml");

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

        private static void ExpectInvalidData(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(
                    "Preview Review rejected invalid change for an unexpected reason: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException("Expected invalid Preview Review change to fail closed.");
        }
    }
}
