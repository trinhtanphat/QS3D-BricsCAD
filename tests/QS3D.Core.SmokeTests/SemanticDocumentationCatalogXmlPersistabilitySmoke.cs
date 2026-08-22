using System;
using System.IO;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            InvalidRequiredTextFailsBeforeProjectMutation();
            InvalidOptionalTextFailsBeforeProjectMutation();
            SupplementaryUnicodeRoundTripsThroughStoreAndQsdb();
        }

        private static void InvalidRequiredTextFailsBeforeProjectMutation()
        {
            var project = new ProjectState("DOC-CATALOG-XML-REQ", "Documentation catalog XML required text");
            var store = new SemanticDocumentationCatalogStore();
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeHasPayload = project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var beforePayload);

            Throws<InvalidOperationException>(() => store.Save(
                project,
                new[] { new SemanticViewDefinition("VIEW-1", "View \uD800") },
                Array.Empty<SemanticSheetDefinition>()));
            RequireUnchanged(project, beforeVersion, beforeUpdatedUtc, beforeHasPayload, beforePayload, "invalid view name");

            Throws<InvalidOperationException>(() => store.Save(
                project,
                Array.Empty<SemanticViewDefinition>(),
                new[] { Sheet("SHEET-\uD800", "A-01", "Sheet") }));
            RequireUnchanged(project, beforeVersion, beforeUpdatedUtc, beforeHasPayload, beforePayload, "invalid sheet id");

            Throws<InvalidOperationException>(() => store.Save(
                project,
                Array.Empty<SemanticViewDefinition>(),
                new[] { Sheet("SHEET-1", "A-\uD800", "Sheet") }));
            RequireUnchanged(project, beforeVersion, beforeUpdatedUtc, beforeHasPayload, beforePayload, "invalid sheet number");

            Throws<InvalidOperationException>(() => store.Save(
                project,
                Array.Empty<SemanticViewDefinition>(),
                new[] { Sheet("SHEET-1", "A-01", "Sheet \uD800") }));
            RequireUnchanged(project, beforeVersion, beforeUpdatedUtc, beforeHasPayload, beforePayload, "invalid sheet name");
        }

        private static void InvalidOptionalTextFailsBeforeProjectMutation()
        {
            var project = new ProjectState("DOC-CATALOG-XML-OPT", "Documentation catalog XML optional text");
            var store = new SemanticDocumentationCatalogStore();
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeHasPayload = project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var beforePayload);

            Throws<InvalidOperationException>(() => store.Save(
                project,
                Array.Empty<SemanticViewDefinition>(),
                new[] { Sheet("SHEET-1", "A-01", "Sheet", "Title \uD800") }));

            RequireUnchanged(project, beforeVersion, beforeUpdatedUtc, beforeHasPayload, beforePayload, "invalid title block");
        }

        private static void SupplementaryUnicodeRoundTripsThroughStoreAndQsdb()
        {
            const string compass = "\U0001F9ED";
            var viewId = "VIEW-" + compass;
            var viewName = "View " + compass;
            var sheetId = "SHEET-" + compass;
            var sheetNumber = "A-" + compass;
            var sheetName = "Sheet " + compass;
            var titleBlockName = "Title " + compass;
            var project = new ProjectState("DOC-CATALOG-XML-ROUNDTRIP", "Documentation catalog Unicode roundtrip");
            var store = new SemanticDocumentationCatalogStore();
            var view = new SemanticViewDefinition(viewId, viewName);
            var placement = new SemanticSheetPlacementDefinition(viewId, 10d, 20d, 100d, 50d);
            var sheet = new SemanticSheetDefinition(sheetId, sheetNumber, sheetName, 841d, 594d, new[] { placement }, titleBlockName);

            store.Save(project, new[] { view }, new[] { sheet });
            var catalog = store.Load(project);
            AssertCatalog(catalog, viewId, viewName, sheetId, sheetNumber, sheetName, titleBlockName);

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-doc-catalog-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var qsdb = new QsdbProjectStore();
                qsdb.SaveNew(project, path);
                var loadedProject = qsdb.Load(path);
                var loadedCatalog = store.Load(loadedProject);
                AssertCatalog(loadedCatalog, viewId, viewName, sheetId, sheetNumber, sheetName, titleBlockName);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static SemanticSheetDefinition Sheet(string id, string number, string name, string? titleBlockName = null) =>
            new SemanticSheetDefinition(id, number, name, 841d, 594d, Array.Empty<SemanticSheetPlacementDefinition>(), titleBlockName);

        private static void AssertCatalog(
            SemanticDocumentationCatalog catalog,
            string viewId,
            string viewName,
            string sheetId,
            string sheetNumber,
            string sheetName,
            string titleBlockName)
        {
            Require(catalog.Views.Count == 1, "Documentation catalog view count changed across round-trip.");
            Require(catalog.Sheets.Count == 1, "Documentation catalog sheet count changed across round-trip.");
            Require(catalog.Views[0].Id == viewId, "Supplementary-Unicode view id changed across round-trip.");
            Require(catalog.Views[0].Name == viewName, "Supplementary-Unicode view name changed across round-trip.");
            Require(catalog.Sheets[0].Id == sheetId, "Supplementary-Unicode sheet id changed across round-trip.");
            Require(catalog.Sheets[0].Number == sheetNumber, "Supplementary-Unicode sheet number changed across round-trip.");
            Require(catalog.Sheets[0].Name == sheetName, "Supplementary-Unicode sheet name changed across round-trip.");
            Require(catalog.Sheets[0].TitleBlockName == titleBlockName, "Supplementary-Unicode title block changed across round-trip.");
            Require(catalog.Sheets[0].Placements.Count == 1 && catalog.Sheets[0].Placements[0].ViewId == viewId, "Supplementary-Unicode placement view id changed across round-trip.");
        }

        private static void RequireUnchanged(
            ProjectState project,
            long beforeVersion,
            DateTime beforeUpdatedUtc,
            bool beforeHasPayload,
            string? beforePayload,
            string label)
        {
            Require(project.ChangeVersion == beforeVersion, label + " changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, label + " changed project timestamp.");
            var afterHasPayload = project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var afterPayload);
            Require(afterHasPayload == beforeHasPayload, label + " changed documentation metadata presence.");
            Require(!afterHasPayload || string.Equals(afterPayload, beforePayload, StringComparison.Ordinal), label + " changed documentation metadata payload.");
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
