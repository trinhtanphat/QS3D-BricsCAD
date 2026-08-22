using System;
using System.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogEditorSmoke
    {
        public static void Run()
        {
            UpsertIsIdempotentAndPersists();
            ContentOnlyUpsertDoesNotReportPlacementRewrite();
            ReferencedViewRemovalFailsWithoutCascade();
            CascadedViewRemovalDropsOnlyItsPlacements();
            ViewIdentityReplacementCanRewriteSheets();
            InvalidUpsertDoesNotMutateCatalog();
        }

        private static void UpsertIsIdempotentAndPersists()
        {
            var project = BuildProject();
            var editor = new SemanticDocumentationCatalogEditor();
            var view = View("V-1", "Model 1");
            var first = editor.UpsertView(project, view);
            Equal(true, first.Changed);
            Equal(1, first.ViewCount);

            var version = project.ChangeVersion;
            var second = editor.UpsertView(project, view);
            Equal(false, second.Changed);
            Equal(version, project.ChangeVersion);

            var sheet = Sheet("S-1", "A-01", "Sheet 1", Placement("V-1", 10, 10, 100, 80));
            var sheetResult = editor.UpsertSheet(project, sheet);
            Equal(true, sheetResult.Changed);
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(1, catalog.Views.Count);
            Equal(1, catalog.Sheets.Count);
            Equal("V-1", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void ContentOnlyUpsertDoesNotReportPlacementRewrite()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var result = editor.UpsertView(project, View("V-1", "Model 1 renamed"));
            Equal(true, result.Changed);
            Equal(0, result.RewrittenPlacementCount);

            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal("Model 1 renamed", catalog.Views.Single(x => x.Id == "V-1").Name);
            Equal("V-1", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void ReferencedViewRemovalFailsWithoutCascade()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var metadata = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var version = project.ChangeVersion;

            MustFail(() => editor.RemoveView(project, "V-1"));
            Equal(version, project.ChangeVersion);
            Equal(metadata, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(2, catalog.Views.Count);
            Equal(2, catalog.Sheets[0].Placements.Count);
        }

        private static void CascadedViewRemovalDropsOnlyItsPlacements()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var result = editor.RemoveView(project, "v-1", true);
            Equal(true, result.Changed);
            Equal(1, result.RewrittenPlacementCount);
            Equal(1, result.ViewCount);

            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(false, catalog.Views.Any(x => string.Equals(x.Id, "V-1", StringComparison.OrdinalIgnoreCase)));
            Equal(1, catalog.Sheets[0].Placements.Count);
            Equal("V-2", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void ViewIdentityReplacementCanRewriteSheets()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            MustFail(() => editor.ReplaceView(project, "V-1", View("V-100", "Model 100"), false));

            var result = editor.ReplaceView(project, "V-1", View("V-100", "Model 100"), true);
            Equal(true, result.Changed);
            Equal(1, result.RewrittenPlacementCount);
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(true, catalog.Views.Any(x => x.Id == "V-100"));
            Equal(false, catalog.Views.Any(x => x.Id == "V-1"));
            Equal("V-100", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void InvalidUpsertDoesNotMutateCatalog()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var metadata = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var version = project.ChangeVersion;

            MustFail(() => editor.UpsertView(project, View("V-2", "Model 1")));
            Equal(version, project.ChangeVersion);
            Equal(metadata, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);

            MustFail(() => editor.UpsertSheet(project, Sheet("S-2", "A-02", "Broken", Placement("V-404", 10, 10, 100, 80))));
            Equal(version, project.ChangeVersion);
            Equal(metadata, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static ProjectState SeedCatalog()
        {
            var project = BuildProject();
            new SemanticDocumentationCatalogStore().Save(
                project,
                new[] { View("V-1", "Model 1"), View("V-2", "Model 2") },
                new[]
                {
                    Sheet(
                        "S-1",
                        "A-01",
                        "Sheet 1",
                        Placement("V-1", 10, 10, 100, 80),
                        Placement("V-2", 120, 10, 100, 80))
                });
            return project;
        }

        private static ProjectState BuildProject()
        {
            return new ProjectState("P-DOC-EDIT", "Documentation Editor Smoke");
        }

        private static SemanticViewDefinition View(string id, string name)
        {
            return new SemanticViewDefinition(id, name, SemanticViewKind.Model);
        }

        private static SemanticSheetDefinition Sheet(string id, string number, string name, params SemanticSheetPlacementDefinition[] placements)
        {
            return new SemanticSheetDefinition(id, number, name, 297d, 210d, placements, "A3");
        }

        private static SemanticSheetPlacementDefinition Placement(string viewId, double x, double y, double width, double height)
        {
            return new SemanticSheetPlacementDefinition(viewId, x, y, width, height);
        }

        private static void MustFail(Action action)
        {
            try { action(); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is System.Collections.Generic.KeyNotFoundException) { return; }
            throw new Exception("Expected semantic documentation edit to fail closed.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}