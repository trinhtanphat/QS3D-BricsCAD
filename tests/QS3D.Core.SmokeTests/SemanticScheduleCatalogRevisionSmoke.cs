using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCatalogRevisionSmoke
    {
        internal static void Run()
        {
            CatalogMutationTouchesProjectExactlyOnce();
            CatalogUsesLastAvailableRevision();
            ScheduleBuildRejectsMoreThanFiveThousandMatchesBeforeTableMaterialization();
            ScheduleBuildCountsOnlyMatchingRowsAgainstTheLimit();
            DefinitionSnapshotRejectsOversizedKnownCountBeforeTraversal();
            DefinitionSnapshotRejectsInvalidAndConflictingKnownCounts();
            DefinitionSnapshotRejectsKnownCountTraversalMismatch();
            DefinitionSnapshotAcceptsHonestCountAndPureStreaming();
        }

        private static void CatalogMutationTouchesProjectExactlyOnce()
        {
            var project = Project();
            var definition = Definition("S1", "Beam schedule", "BEAMS");

            Equal(0L, project.ChangeVersion);
            SemanticScheduleCatalog.Save(project, new[] { definition });
            Equal(1L, project.ChangeVersion);
            True(project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            SemanticScheduleCatalog.Save(project, new[] { definition });
            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);

            SemanticScheduleCatalog.Save(project, Array.Empty<SemanticScheduleDefinition>());
            Equal(version + 1L, project.ChangeVersion);
            True(!project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));
        }

        private static void CatalogUsesLastAvailableRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-semantic-schedule-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(Project(), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for semantic schedule revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var project = store.Load(path);
                Equal(long.MaxValue - 1L, project.ChangeVersion);

                var definition = Definition("S1", "Beam schedule", "BEAMS");
                SemanticScheduleCatalog.Save(project, new[] { definition });

                Equal(long.MaxValue, project.ChangeVersion);
                True(project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));

                var beforeRejectedUpdatedUtc = project.UpdatedUtc;
                var beforeRejectedMetadata = project.Metadata[SemanticScheduleCatalog.MetadataKey];

                var rejectedRewrite = false;
                try
                {
                    SemanticScheduleCatalog.Save(project, new[] { Definition("S1", "Changed schedule", "CHANGED") });
                }
                catch (OverflowException)
                {
                    rejectedRewrite = true;
                }

                True(rejectedRewrite);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[SemanticScheduleCatalog.MetadataKey]);

                var rejectedClear = false;
                try
                {
                    SemanticScheduleCatalog.Save(project, Array.Empty<SemanticScheduleDefinition>());
                }
                catch (OverflowException)
                {
                    rejectedClear = true;
                }

                True(rejectedClear);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[SemanticScheduleCatalog.MetadataKey]);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void ScheduleBuildRejectsMoreThanFiveThousandMatchesBeforeTableMaterialization()
        {
            var project = new ProjectState("SCHEDULE-MATCH-LIMIT", "Schedule match limit");
            for (var i = 0; i < 5001; i++)
                project.Elements.Add(new ProjectElement("B" + i.ToString("D4", CultureInfo.InvariantCulture), ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            var message = ThrowsMessage<InvalidOperationException>(() =>
                SemanticScheduleCatalog.Build(project, Definition("S1", "Beam schedule", "BEAMS")));

            Equal("Semantic schedule supports at most 5000 matching elements.", message);
        }

        private static void ScheduleBuildCountsOnlyMatchingRowsAgainstTheLimit()
        {
            var project = new ProjectState("SCHEDULE-NONMATCH-LIMIT", "Schedule nonmatch limit");
            for (var i = 0; i < 5001; i++)
                project.Elements.Add(new ProjectElement("C" + i.ToString("D4", CultureInfo.InvariantCulture), ElementCategory.Column, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("B0001", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            var table = SemanticScheduleCatalog.Build(project, Definition("S1", "Beam schedule", "BEAMS"));

            Equal(1, table.Rows.Count);
            Equal("B0001", table.Rows[0].ElementId);
        }

        private static void DefinitionSnapshotRejectsOversizedKnownCountBeforeTraversal()
        {
            var categories = new CountedSequence<ElementCategory>(
                new[] { ElementCategory.Beam },
                genericCount: 5001,
                readOnlyCount: 5001,
                nonGenericCount: 5001);

            var message = ThrowsMessage<InvalidOperationException>(() => DefinitionWithCategories(categories));
            Equal("Semantic schedule category list exceeds 5000 entries.", message);
            Equal(0, categories.EnumerationCount);
        }

        private static void DefinitionSnapshotRejectsInvalidAndConflictingKnownCounts()
        {
            var negative = new CountedSequence<ElementCategory>(
                new[] { ElementCategory.Beam },
                genericCount: -1,
                readOnlyCount: -1,
                nonGenericCount: -1);
            Equal(
                "Semantic schedule collection source reports an invalid negative known Count.",
                ThrowsMessage<InvalidOperationException>(() => DefinitionWithCategories(negative)));
            Equal(0, negative.EnumerationCount);

            var conflictingColumns = new CountedSequence<SemanticDocumentationColumn>(
                new[] { new SemanticDocumentationColumn("Id", "{Id}") },
                genericCount: 1,
                readOnlyCount: 2,
                nonGenericCount: 1);
            Equal(
                "Semantic schedule collection source exposes conflicting known Count values.",
                ThrowsMessage<InvalidOperationException>(() => DefinitionWithColumns(conflictingColumns)));
            Equal(0, conflictingColumns.EnumerationCount);
        }

        private static void DefinitionSnapshotRejectsKnownCountTraversalMismatch()
        {
            var underEnumeratedInclude = new CountedSequence<string>(
                new[] { "E1" },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2);
            Equal(
                "Semantic schedule collection source known Count does not match completed traversal.",
                ThrowsMessage<InvalidOperationException>(() => DefinitionWithInclude(underEnumeratedInclude)));
            Equal(1, underEnumeratedInclude.EnumerationCount);

            var overEnumeratedExclude = new CountedSequence<string>(
                new[] { "E1" },
                genericCount: 0,
                readOnlyCount: 0,
                nonGenericCount: 0);
            Equal(
                "Semantic schedule collection source known Count does not match completed traversal.",
                ThrowsMessage<InvalidOperationException>(() => DefinitionWithExclude(overEnumeratedExclude)));
            Equal(1, overEnumeratedExclude.EnumerationCount);
        }

        private static void DefinitionSnapshotAcceptsHonestCountAndPureStreaming()
        {
            var countedCategories = new CountedSequence<ElementCategory>(
                new[] { ElementCategory.Beam },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1);
            var counted = DefinitionWithCategories(countedCategories);
            Equal(1, counted.Categories.Count);
            Equal(ElementCategory.Beam, counted.Categories[0]);
            Equal(1, countedCategories.EnumerationCount);

            var streaming = new SemanticScheduleDefinition(
                "S-STREAM",
                "Streaming",
                "STREAM",
                Stream(ElementCategory.Beam),
                string.Empty,
                string.Empty,
                Stream("E1", "E2"),
                Stream<string>(),
                Stream(new SemanticDocumentationColumn("Id", "{Id}")));
            Equal(1, streaming.Categories.Count);
            Equal(2, streaming.IncludeElementIds.Count);
            Equal(0, streaming.ExcludeElementIds.Count);
            Equal(1, streaming.Columns.Count);
        }

        private static SemanticScheduleDefinition DefinitionWithCategories(IEnumerable<ElementCategory> categories)
        {
            return new SemanticScheduleDefinition(
                "S-COUNT",
                "Count contract",
                "COUNT",
                categories,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
        }

        private static SemanticScheduleDefinition DefinitionWithInclude(IEnumerable<string> include)
        {
            return new SemanticScheduleDefinition(
                "S-INCLUDE",
                "Include count",
                "INCLUDE",
                new[] { ElementCategory.Beam },
                string.Empty,
                string.Empty,
                include,
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
        }

        private static SemanticScheduleDefinition DefinitionWithExclude(IEnumerable<string> exclude)
        {
            return new SemanticScheduleDefinition(
                "S-EXCLUDE",
                "Exclude count",
                "EXCLUDE",
                new[] { ElementCategory.Beam },
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                exclude,
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
        }

        private static SemanticScheduleDefinition DefinitionWithColumns(IEnumerable<SemanticDocumentationColumn> columns)
        {
            return new SemanticScheduleDefinition(
                "S-COLUMNS",
                "Column count",
                "COLUMNS",
                new[] { ElementCategory.Beam },
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                columns);
        }

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static SemanticScheduleDefinition Definition(string id, string name, string title)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                title,
                new[] { ElementCategory.Beam },
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new SemanticDocumentationColumn("Id", "{Id}"),
                    new SemanticDocumentationColumn("Mark", "{P:Mark}")
                });
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("SCHEDULE-REV", "Schedule revision");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            project.Elements.Single().Properties["Mark"] = "B1";
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new Exception("Expected condition to be true.");
        }

        private static string ThrowsMessage<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex.Message;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private sealed class CountedSequence<T> : ICollection<T>, IReadOnlyCollection<T>, System.Collections.ICollection
        {
            private readonly T[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal CountedSequence(T[] items, int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            internal int EnumerationCount { get; private set; }
            public int Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int System.Collections.ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool System.Collections.ICollection.IsSynchronized => false;
            object System.Collections.ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    EnumerationCount++;
                    yield return _items[i];
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void System.Collections.ICollection.CopyTo(Array array, int index) => ((System.Collections.ICollection)_items).CopyTo(array, index);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}