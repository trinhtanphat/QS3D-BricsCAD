using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleDefinitionBoundedSnapshotSmoke
    {
        internal static void Run()
        {
            CategoriesAcceptExactLimit();
            CategoriesStopAtFirstOverBoundItem();
            IncludeIdsStopAtFirstOverBoundItem();
            ExcludeIdsStopAtFirstOverBoundItem();
            ColumnsStopAtFirstOverBoundItem();
            AcceptedCollectionsRemainDefensiveSnapshots();
        }

        private static void CategoriesAcceptExactLimit()
        {
            var definition = new SemanticScheduleDefinition(
                "S-CATEGORY-LIMIT",
                "Category limit",
                "CATEGORY LIMIT",
                RepeatCategories(5000),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                OneColumn());

            Equal(5000, definition.Categories.Count);
        }

        private static void CategoriesStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticScheduleDefinition(
                    "S-CATEGORY-OVER",
                    "Category bound",
                    "CATEGORY BOUND",
                    OverBoundedCategories(),
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    OneColumn()),
                "Semantic schedule category list exceeds 5000 entries.");
        }

        private static void IncludeIdsStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticScheduleDefinition(
                    "S-INCLUDE",
                    "Include bound",
                    "INCLUDE",
                    new[] { ElementCategory.Beam },
                    string.Empty,
                    string.Empty,
                    OverBoundedIds("I-", "Include source enumerated beyond the first over-bound id."),
                    Array.Empty<string>(),
                    OneColumn()),
                "Semantic schedule include list exceeds 5000 ids.");
        }

        private static void ExcludeIdsStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticScheduleDefinition(
                    "S-EXCLUDE",
                    "Exclude bound",
                    "EXCLUDE",
                    new[] { ElementCategory.Beam },
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    OverBoundedIds("E-", "Exclude source enumerated beyond the first over-bound id."),
                    OneColumn()),
                "Semantic schedule exclude list exceeds 5000 ids.");
        }

        private static void ColumnsStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticScheduleDefinition(
                    "S-COLUMNS",
                    "Column bound",
                    "COLUMNS",
                    new[] { ElementCategory.Beam },
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    OverBoundedColumns()),
                "Semantic schedule requires 1..32 columns.");
        }

        private static void AcceptedCollectionsRemainDefensiveSnapshots()
        {
            var categories = new List<ElementCategory> { ElementCategory.Beam };
            var include = new List<string> { "E-1" };
            var exclude = new List<string> { "E-2" };
            var columns = new List<SemanticDocumentationColumn> { new SemanticDocumentationColumn("Id", "{Id}") };
            var definition = new SemanticScheduleDefinition(
                "S-SNAPSHOT",
                "Snapshot",
                "SNAPSHOT",
                categories,
                string.Empty,
                string.Empty,
                include,
                exclude,
                columns);

            categories.Clear();
            include.Clear();
            exclude.Clear();
            columns.Clear();

            Equal(1, definition.Categories.Count);
            Equal(ElementCategory.Beam, definition.Categories[0]);
            Equal(1, definition.IncludeElementIds.Count);
            Equal("E-1", definition.IncludeElementIds[0]);
            Equal(1, definition.ExcludeElementIds.Count);
            Equal("E-2", definition.ExcludeElementIds[0]);
            Equal(1, definition.Columns.Count);
            Equal("Id", definition.Columns[0].Header);
        }

        private static IEnumerable<string> OverBoundedIds(string prefix, string sentinelMessage)
        {
            for (var i = 0; i <= 5000; i++) yield return prefix + i;
            throw new ApplicationException(sentinelMessage);
        }

        private static IEnumerable<ElementCategory> RepeatCategories(int count)
        {
            for (var i = 0; i < count; i++) yield return ElementCategory.Beam;
        }

        private static IEnumerable<ElementCategory> OverBoundedCategories()
        {
            for (var i = 0; i <= 5000; i++) yield return ElementCategory.Beam;
            throw new ApplicationException("Category source enumerated beyond the first over-bound item.");
        }

        private static IEnumerable<SemanticDocumentationColumn> OverBoundedColumns()
        {
            for (var i = 0; i <= 32; i++)
                yield return new SemanticDocumentationColumn("C" + i, "{Id}");
            throw new ApplicationException("Column source enumerated beyond the first over-bound column.");
        }

        private static SemanticDocumentationColumn[] OneColumn()
        {
            return new[] { new SemanticDocumentationColumn("Id", "{Id}") };
        }

        private static void MustFailCapacity(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal))
                    throw new Exception("Unexpected capacity error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected bounded Semantic Schedule capacity failure, got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("Expected bounded Semantic Schedule capacity failure.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
