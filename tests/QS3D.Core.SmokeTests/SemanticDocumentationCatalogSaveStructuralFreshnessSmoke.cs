using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogSaveStructuralFreshnessSmoke
    {
        internal static void Run()
        {
            ViewEnumerationElementReplacementFailsClosed();
            SheetEnumerationElementReplacementFailsClosed();
            ViewEnumerationRevisionDriftFailsClosed();
            SheetEnumerationRevisionDriftFailsClosed();
            StableSaveRemainsDeterministic();
        }

        private static void ViewEnumerationElementReplacementFailsClosed()
        {
            var project = BuildProject("DOC-SAVE-VIEW-STRUCTURE");
            var beforeVersion = project.ChangeVersion;
            var original = project.Elements[0];
            var store = new SemanticDocumentationCatalogStore();

            Throws<InvalidOperationException>(() => store.Save(
                project,
                ReplaceElementWhileEnumeratingViews(project),
                Array.Empty<SemanticSheetDefinition>()));

            Equal(beforeVersion, project.ChangeVersion);
            NotSame(original, project.Elements[0]);
            MetadataAbsent(project);
        }

        private static void SheetEnumerationElementReplacementFailsClosed()
        {
            var project = BuildProject("DOC-SAVE-SHEET-STRUCTURE");
            var beforeVersion = project.ChangeVersion;
            var original = project.Elements[0];
            var store = new SemanticDocumentationCatalogStore();

            Throws<InvalidOperationException>(() => store.Save(
                project,
                StableViews(),
                ReplaceElementWhileEnumeratingSheets(project)));

            Equal(beforeVersion, project.ChangeVersion);
            NotSame(original, project.Elements[0]);
            MetadataAbsent(project);
        }

        private static void ViewEnumerationRevisionDriftFailsClosed()
        {
            var project = BuildProject("DOC-SAVE-VIEW-REVISION");
            var beforeVersion = project.ChangeVersion;
            var store = new SemanticDocumentationCatalogStore();

            Throws<InvalidOperationException>(() => store.Save(
                project,
                TouchProjectWhileEnumeratingViews(project),
                Array.Empty<SemanticSheetDefinition>()));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion);
            MetadataAbsent(project);
        }

        private static void SheetEnumerationRevisionDriftFailsClosed()
        {
            var project = BuildProject("DOC-SAVE-SHEET-REVISION");
            var beforeVersion = project.ChangeVersion;
            var store = new SemanticDocumentationCatalogStore();

            Throws<InvalidOperationException>(() => store.Save(
                project,
                StableViews(),
                TouchProjectWhileEnumeratingSheets(project)));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion);
            MetadataAbsent(project);
        }

        private static void StableSaveRemainsDeterministic()
        {
            var project = BuildProject("DOC-SAVE-STABLE");
            var beforeVersion = project.ChangeVersion;
            var store = new SemanticDocumentationCatalogStore();
            var views = StableViews();
            var sheets = StableSheets();

            store.Save(project, views, sheets);

            Equal(checked(beforeVersion + 1L), project.ChangeVersion);
            if (!project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var firstPayload))
                throw new InvalidOperationException("Stable documentation catalog save did not persist its payload.");
            var catalog = store.Load(project);
            Equal(1, catalog.Views.Count);
            Equal(1, catalog.Sheets.Count);
            Equal("VIEW-01", catalog.Sheets[0].Placements[0].ViewId);

            store.Save(project, views, sheets);

            Equal(checked(beforeVersion + 1L), project.ChangeVersion);
            Equal(firstPayload, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static IEnumerable<SemanticViewDefinition> ReplaceElementWhileEnumeratingViews(ProjectState project)
        {
            project.Elements[0] = ReplacementElement();
            yield return StableView();
        }

        private static IEnumerable<SemanticSheetDefinition> ReplaceElementWhileEnumeratingSheets(ProjectState project)
        {
            project.Elements[0] = ReplacementElement();
            yield return StableSheet();
        }

        private static IEnumerable<SemanticViewDefinition> TouchProjectWhileEnumeratingViews(ProjectState project)
        {
            project.Touch();
            yield return StableView();
        }

        private static IEnumerable<SemanticSheetDefinition> TouchProjectWhileEnumeratingSheets(ProjectState project)
        {
            project.Touch();
            yield return StableSheet();
        }

        private static SemanticViewDefinition[] StableViews() => new[] { StableView() };

        private static SemanticSheetDefinition[] StableSheets() => new[] { StableSheet() };

        private static SemanticViewDefinition StableView() =>
            new SemanticViewDefinition("VIEW-01", "Plan 01", SemanticViewKind.Plan, "F-01", "Z-01");

        private static SemanticSheetDefinition StableSheet() =>
            new SemanticSheetDefinition(
                "SHEET-01",
                "A-01",
                "Plan sheet",
                420d,
                297d,
                new[] { new SemanticSheetPlacementDefinition("VIEW-01", 10d, 10d, 200d, 120d) },
                "A3");

        private static ProjectElement ReplacementElement() =>
            new ProjectElement("E-01", ElementCategory.Column, string.Empty, "F-01", "Z-01");

        private static ProjectState BuildProject(string id)
        {
            var project = new ProjectState(id, "Documentation catalog save freshness");
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 0d));
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));
            project.Elements.Add(new ProjectElement("E-01", ElementCategory.Beam, string.Empty, "F-01", "Z-01"));
            return project;
        }

        private static void MetadataAbsent(ProjectState project)
        {
            if (project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey))
                throw new InvalidOperationException("Rejected documentation catalog save must not persist metadata.");
        }

        private static void NotSame(object left, object right)
        {
            if (ReferenceEquals(left, right))
                throw new InvalidOperationException("Structural-freshness fixture did not replace the project entry.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
