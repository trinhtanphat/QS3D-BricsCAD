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
            ScheduleCrudAndViewReferenceGuards();
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
            Equal(0, first.ScheduleCount);

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
            Equal(0, catalog.Schedules.Count);
            Equal("V-1", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void ContentOnlyUpsertDoesNotReportPlacementRewrite()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var result = editor.UpsertView(project, View("V-1", "Model 1 renamed"));
            Equal(true, result.Changed);
            Equal(0, result.RewrittenPlacementCount);
            Equal(0, result.RewrittenScheduleReferenceCount);

            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal("Model 1 renamed", catalog.Views.Single(x => x.Id == "V-1").Name);
            Equal("V-1", catalog.Sheets[0].Placements[0].ViewId);
            Equal("V-S", catalog.Schedules[0].ViewId);
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
            Equal(3, catalog.Views.Count);
            Equal(2, catalog.Sheets[0].Placements.Count);
            Equal(1, catalog.Schedules.Count);
        }

        private static void CascadedViewRemovalDropsOnlyItsPlacements()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            var result = editor.RemoveView(project, "v-1", true);
            Equal(true, result.Changed);
            Equal(1, result.RewrittenPlacementCount);
            Equal(0, result.RewrittenScheduleReferenceCount);
            Equal(2, result.ViewCount);

            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(false, catalog.Views.Any(x => string.Equals(x.Id, "V-1", StringComparison.OrdinalIgnoreCase)));
            Equal(1, catalog.Sheets[0].Placements.Count);
            Equal("V-2", catalog.Sheets[0].Placements[0].ViewId);
            Equal(1, catalog.Schedules.Count);
        }

        private static void ViewIdentityReplacementCanRewriteSheets()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();
            MustFail(() => editor.ReplaceView(project, "V-1", View("V-100", "Model 100"), false));

            var result = editor.ReplaceView(project, "V-1", View("V-100", "Model 100"), true);
            Equal(true, result.Changed);
            Equal(1, result.RewrittenPlacementCount);
            Equal(0, result.RewrittenScheduleReferenceCount);
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(true, catalog.Views.Any(x => x.Id == "V-100"));
            Equal(false, catalog.Views.Any(x => x.Id == "V-1"));
            Equal("V-100", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void ScheduleCrudAndViewReferenceGuards()
        {
            var project = SeedCatalog();
            var editor = new SemanticDocumentationCatalogEditor();

            var schedule = Schedule("SCH-2", "Schedule 2", "V-S");
            var inserted = editor.UpsertSchedule(project, schedule);
            Equal(true, inserted.Changed);
            Equal(2, inserted.ScheduleCount);

            var version = project.ChangeVersion;
            var same = editor.UpsertSchedule(project, schedule);
            Equal(false, same.Changed);
            Equal(version, project.ChangeVersion);

            MustFail(() => editor.ReplaceView(project, "V-S", ScheduleView("V-S2", "Schedule Source 2"), false, false));
            var rewritten = editor.ReplaceView(project, "V-S", ScheduleView("V-S2", "Schedule Source 2"), false, true);
            Equal(2, rewritten.RewrittenScheduleReferenceCount);
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(true, catalog.Schedules.All(x => x.ViewId == "V-S2"));

            MustFail(() => editor.RemoveView(project, "V-S2", false, false));
            var removed = editor.RemoveView(project, "V-S2", false, true);
            Equal(2, removed.RewrittenScheduleReferenceCount);
            Equal(0, removed.ScheduleCount);
            Equal(false, new SemanticDocumentationCatalogStore().Load(project).Views.Any(x => x.Id == "V-S2"));

            var noOp = editor.RemoveSchedule(project, "SCH-404");
            Equal(false, noOp.Changed);
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

            MustFail(() => editor.UpsertSchedule(project, Schedule("SCH-BAD", "Broken Schedule", "V-404")));
            Equal(version, project.ChangeVersion);
            Equal(metadata, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static ProjectState SeedCatalog()
        {
            var project = BuildProject();
            new SemanticDocumentationCatalogStore().Save(
                project,
                new[] { View("V-1", "Model 1"), View("V-2", "Model 2"), ScheduleView("V-S", "Schedule Source") },
                new[]
                {
                    Sheet(
                        "S-1",
                        "A-01",
                        "Sheet 1",
                        Placement("V-1", 10, 10, 100, 80),
                        Placement("V-2", 120, 10, 100, 80))
                },
                new[] { Schedule("SCH-1", "Schedule 1", "V-S") });
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

        private static SemanticViewDefinition ScheduleView(string id, string name)
        {
            return new SemanticViewDefinition(id, name, SemanticViewKind.Schedule);
        }

        private static SemanticScheduleDefinition Schedule(string id, string name, string viewId)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                viewId,
                new[] { new SemanticDocumentationColumn("Element ID", "{Id}") });
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
