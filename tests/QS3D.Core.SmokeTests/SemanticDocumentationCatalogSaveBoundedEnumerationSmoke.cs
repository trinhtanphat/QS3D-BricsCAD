using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogSaveBoundedEnumerationSmoke
    {
        public static void Run()
        {
            OversizeLazyViewsStopAtFirstItemBeyondCapacity();
            OversizeLazySheetsStopAtFirstItemBeyondCapacity();
        }

        private static void OversizeLazyViewsStopAtFirstItemBeyondCapacity()
        {
            var project = new ProjectState("P-DOC-CATALOG-VIEW-BOUND", "Documentation catalog view bound");
            var source = new GuardedInfiniteViews();
            var beforeVersion = project.ChangeVersion;

            try
            {
                new SemanticDocumentationCatalogStore().Save(
                    project,
                    source.Values(),
                    Array.Empty<SemanticSheetDefinition>());
            }
            catch (InvalidOperationException ex)
            {
                Equal("Semantic view catalog supports at most 10000 views.", ex.Message);
                Equal(10001, source.YieldCount);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(false, project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));
                return;
            }

            throw new Exception("Expected semantic documentation view catalog capacity rejection.");
        }

        private static void OversizeLazySheetsStopAtFirstItemBeyondCapacity()
        {
            var project = new ProjectState("P-DOC-CATALOG-SHEET-BOUND", "Documentation catalog sheet bound");
            var source = new GuardedInfiniteSheets();
            var beforeVersion = project.ChangeVersion;

            try
            {
                new SemanticDocumentationCatalogStore().Save(
                    project,
                    Array.Empty<SemanticViewDefinition>(),
                    source.Values());
            }
            catch (InvalidOperationException ex)
            {
                Equal("Semantic sheet catalog supports at most 10000 sheets.", ex.Message);
                Equal(10001, source.YieldCount);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(false, project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));
                return;
            }

            throw new Exception("Expected semantic documentation sheet catalog capacity rejection.");
        }

        private sealed class GuardedInfiniteViews
        {
            public int YieldCount { get; private set; }

            public IEnumerable<SemanticViewDefinition> Values()
            {
                while (true)
                {
                    YieldCount++;
                    if (YieldCount > 10001)
                        throw new Exception("Documentation catalog save enumerated views beyond the first item over capacity.");
                    yield return new SemanticViewDefinition("V-" + YieldCount, "View " + YieldCount);
                }
            }
        }

        private sealed class GuardedInfiniteSheets
        {
            public int YieldCount { get; private set; }

            public IEnumerable<SemanticSheetDefinition> Values()
            {
                while (true)
                {
                    YieldCount++;
                    if (YieldCount > 10001)
                        throw new Exception("Documentation catalog save enumerated sheets beyond the first item over capacity.");
                    yield return new SemanticSheetDefinition(
                        "S-" + YieldCount,
                        "A-" + YieldCount,
                        "Sheet " + YieldCount,
                        841d,
                        594d,
                        Array.Empty<SemanticSheetPlacementDefinition>());
                }
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
