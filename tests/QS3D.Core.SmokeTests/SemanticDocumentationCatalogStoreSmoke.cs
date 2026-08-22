using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogStoreSmoke
    {
        public static void Run()
        {
            CatalogRoundTripsThroughQsdb();
            WriterCanonicalizesTextTokens();
            PaddedPersistedTextFailsClosed();
            SameCatalogDoesNotTouchProjectTwice();
            InvalidCatalogDoesNotReplaceStoredPayload();
            UnsafeXmlFailsClosed();
            PersistedViewCountBound();
            PersistedSheetCountBound();
            PersistedPlacementCountBound();
            SaveRejectsOversizedKnownCountBeforeTraversal();
            SaveRejectsNegativeAndConflictingKnownCounts();
            SaveRejectsKnownCountTraversalMismatch();
            SaveAcceptsHonestCountAndPureStreaming();
            EmptyCatalogClearsMetadata();
        }

        private static void CatalogRoundTripsThroughQsdb()
        {
            var project = BuildProject();
            var catalogStore = new SemanticDocumentationCatalogStore();
            catalogStore.Save(project, new[] { BuildView() }, new[] { BuildSheet() });

            if (!project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var payload))
                throw new Exception("Documentation catalog was not stored in project metadata.");
            if (payload.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 || payload.IndexOf("ObjectId", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new Exception("Documentation catalog must not persist native drawing ownership identifiers.");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-doc-catalog-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                new QsdbProjectStore().Save(project, path);
                var reopened = new QsdbProjectStore().Load(path);
                var catalog = catalogStore.Load(reopened);
                Equal(1, catalog.Views.Count);
                Equal("V-L02-BEAM", catalog.Views[0].Id);
                Equal("F-02", catalog.Views[0].FloorId);
                Equal(1, catalog.Sheets.Count);
                Equal("A-101", catalog.Sheets[0].Number);
                Equal("V-L02-BEAM", catalog.Sheets[0].Placements[0].ViewId);
                Equal(841d, catalog.Sheets[0].WidthMm);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void WriterCanonicalizesTextTokens()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            var view = new SemanticViewDefinition(
                " V-L02-BEAM ",
                " L02 Beams ",
                SemanticViewKind.Plan,
                floorId: " F-02 ",
                zoneId: "   ",
                categories: new[] { ElementCategory.Beam },
                includeElementIds: new[] { " B-001 " });
            var sheet = new SemanticSheetDefinition(
                " S-A101 ",
                " A-101 ",
                " Beam Plan ",
                841d,
                594d,
                new[] { new SemanticSheetPlacementDefinition(" V-L02-BEAM ", 20d, 20d, 380d, 250d) },
                " A1 Standard ");

            store.Save(project, new[] { view }, new[] { sheet });
            var payload = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            if (payload.IndexOf(" V-L02-BEAM ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" L02 Beams ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" F-02 ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" B-001 ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" A-101 ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" Beam Plan ", StringComparison.Ordinal) >= 0 ||
                payload.IndexOf(" A1 Standard ", StringComparison.Ordinal) >= 0)
                throw new Exception("Documentation catalog Save must not persist whitespace-padded text tokens.");

            var catalog = store.Load(project);
            Equal("V-L02-BEAM", catalog.Views[0].Id);
            Equal("L02 Beams", catalog.Views[0].Name);
            Equal("F-02", catalog.Views[0].FloorId);
            Equal(null, catalog.Views[0].ZoneId);
            Equal("B-001", catalog.Views[0].IncludeElementIds[0]);
            Equal("S-A101", catalog.Sheets[0].Id);
            Equal("A-101", catalog.Sheets[0].Number);
            Equal("Beam Plan", catalog.Sheets[0].Name);
            Equal("A1 Standard", catalog.Sheets[0].TitleBlockName);
            Equal("V-L02-BEAM", catalog.Sheets[0].Placements[0].ViewId);
        }

        private static void PaddedPersistedTextFailsClosed()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            var payload = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] =
                payload.Replace("id=\"V-L02-BEAM\"", "id=\" V-L02-BEAM \"");

            MustFailLoad(
                () => store.Load(project),
                "Whitespace-padded persisted documentation identity must fail closed instead of being silently trimmed.");
        }

        private static void SameCatalogDoesNotTouchProjectTwice()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            var version = project.ChangeVersion;
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            Equal(version, project.ChangeVersion);
        }

        private static void InvalidCatalogDoesNotReplaceStoredPayload()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            var before = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var badView = new SemanticViewDefinition("BAD", "Bad View", floorId: "F-404");
            MustFail(
                () => store.Save(project, new[] { badView }, Array.Empty<SemanticSheetDefinition>()),
                "Invalid documentation references must fail before metadata replacement.");
            Equal(before, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static void UnsafeXmlFailsClosed()
        {
            var project = BuildProject();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] =
                "<!DOCTYPE documentation [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><documentation version='1'><views/><sheets/></documentation>";
            var failed = false;
            try { new SemanticDocumentationCatalogStore().Load(project); }
            catch (InvalidDataException) { failed = true; }
            if (!failed) throw new Exception("Documentation metadata containing a DTD must fail closed.");
        }

        private static void PersistedViewCountBound()
        {
            var store = new SemanticDocumentationCatalogStore();
            var exact = BuildPersistedCatalog(10000, 0);
            if (exact.Length >= 1024 * 1024)
                throw new Exception("View count-bound fixture must remain below the metadata character cap.");

            var project = BuildProject();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = exact;
            Equal(10000, store.Load(project).Views.Count);

            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = BuildPersistedCatalog(10001, 0);
            MustFailLoad(
                () => store.Load(project),
                "Persisted documentation catalogs above the 10,000-view bound must fail closed.");
        }

        private static void PersistedSheetCountBound()
        {
            var store = new SemanticDocumentationCatalogStore();
            var exact = BuildPersistedCatalog(0, 10000);
            if (exact.Length >= 1024 * 1024)
                throw new Exception("Sheet count-bound fixture must remain below the metadata character cap.");

            var project = BuildProject();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = exact;
            Equal(10000, store.Load(project).Sheets.Count);

            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = BuildPersistedCatalog(0, 10001);
            MustFailLoad(
                () => store.Load(project),
                "Persisted documentation catalogs above the 10,000-sheet bound must fail closed.");
        }

        private static void PersistedPlacementCountBound()
        {
            var store = new SemanticDocumentationCatalogStore();
            var project = BuildProject();
            var exact = BuildPersistedPlacementCatalog(128);
            if (exact.Length >= 1024 * 1024)
                throw new Exception("Placement count-bound fixture must remain below the metadata character cap.");

            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = exact;
            var catalog = store.Load(project);
            Equal(1, catalog.Sheets.Count);
            Equal(128, catalog.Sheets[0].Placements.Count);

            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = BuildPersistedPlacementCatalog(129);
            MustFailLoad(
                () => store.Load(project),
                "Persisted documentation sheets above the 128-placement bound must fail closed at the persisted-data boundary.");
        }

        private static void SaveRejectsOversizedKnownCountBeforeTraversal()
        {
            var views = new CountedSequence<SemanticViewDefinition>(
                new[] { BuildView() },
                genericCount: 10001,
                readOnlyCount: 10001,
                nonGenericCount: 10001);
            var message = MustFailMessage(() =>
                new SemanticDocumentationCatalogStore().Save(
                    BuildProject(),
                    views,
                    Array.Empty<SemanticSheetDefinition>()));

            Equal("Semantic view catalog supports at most 10000 views.", message);
            Equal(0, views.EnumerationCount);
        }

        private static void SaveRejectsNegativeAndConflictingKnownCounts()
        {
            var negativeViews = new CountedSequence<SemanticViewDefinition>(
                new[] { BuildView() },
                genericCount: -1,
                readOnlyCount: -1,
                nonGenericCount: -1);
            Equal(
                "Semantic documentation catalog source reports an invalid negative known Count.",
                MustFailMessage(() =>
                    new SemanticDocumentationCatalogStore().Save(
                        BuildProject(),
                        negativeViews,
                        Array.Empty<SemanticSheetDefinition>())));
            Equal(0, negativeViews.EnumerationCount);

            var conflictingSheets = new CountedSequence<SemanticSheetDefinition>(
                new[] { BuildSheet() },
                genericCount: 1,
                readOnlyCount: 2,
                nonGenericCount: 1);
            Equal(
                "Semantic documentation catalog source exposes conflicting known Count values.",
                MustFailMessage(() =>
                    new SemanticDocumentationCatalogStore().Save(
                        BuildProject(),
                        new[] { BuildView() },
                        conflictingSheets)));
            Equal(0, conflictingSheets.EnumerationCount);
        }

        private static void SaveRejectsKnownCountTraversalMismatch()
        {
            var underEnumeratedViews = new CountedSequence<SemanticViewDefinition>(
                new[] { BuildView() },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2);
            Equal(
                "Semantic documentation catalog source known Count does not match completed traversal.",
                MustFailMessage(() =>
                    new SemanticDocumentationCatalogStore().Save(
                        BuildProject(),
                        underEnumeratedViews,
                        Array.Empty<SemanticSheetDefinition>())));
            Equal(1, underEnumeratedViews.EnumerationCount);

            var overEnumeratedSheets = new CountedSequence<SemanticSheetDefinition>(
                new[] { BuildSheet() },
                genericCount: 0,
                readOnlyCount: 0,
                nonGenericCount: 0);
            Equal(
                "Semantic documentation catalog source known Count does not match completed traversal.",
                MustFailMessage(() =>
                    new SemanticDocumentationCatalogStore().Save(
                        BuildProject(),
                        new[] { BuildView() },
                        overEnumeratedSheets)));
            Equal(1, overEnumeratedSheets.EnumerationCount);
        }

        private static void SaveAcceptsHonestCountAndPureStreaming()
        {
            var countedViews = new CountedSequence<SemanticViewDefinition>(
                new[] { BuildView() },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1);
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, countedViews, Stream(BuildSheet()));
            Equal(1, countedViews.EnumerationCount);
            Equal(1, store.Load(project).Views.Count);
            Equal(1, store.Load(project).Sheets.Count);

            var streamingProject = BuildProject();
            store.Save(streamingProject, Stream(BuildView()), Stream(BuildSheet()));
            Equal(1, store.Load(streamingProject).Views.Count);
            Equal(1, store.Load(streamingProject).Sheets.Count);
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            foreach (var item in items) yield return item;
        }

        // Keep these generated fixtures deliberately compact so count guards, not the 1 MiB XML cap, determine the result.
        private static string BuildPersistedCatalog(int viewCount, int sheetCount)
        {
            var payload = new StringBuilder("<documentation version=\"1\"><views>");
            for (var i = 0; i < viewCount; i++)
            {
                payload.Append("<view id=\"V").Append(i)
                    .Append("\" name=\"V").Append(i)
                    .Append("\" kind=\"Plan\" floorId=\"\" zoneId=\"\"/>");
            }
            payload.Append("</views><sheets>");
            for (var i = 0; i < sheetCount; i++)
            {
                payload.Append("<sheet id=\"S").Append(i)
                    .Append("\" number=\"").Append(i)
                    .Append("\" name=\"S").Append(i)
                    .Append("\" widthMm=\"1\" heightMm=\"1\" titleBlockName=\"\"/>");
            }
            return payload.Append("</sheets></documentation>").ToString();
        }

        private static string BuildPersistedPlacementCatalog(int placementCount)
        {
            var payload = new StringBuilder("<documentation version=\"1\"><views>");
            for (var i = 0; i < placementCount; i++)
            {
                payload.Append("<view id=\"PV").Append(i)
                    .Append("\" name=\"PV").Append(i)
                    .Append("\" kind=\"Plan\" floorId=\"\" zoneId=\"\"/>");
            }

            payload.Append("</views><sheets><sheet id=\"PS\" number=\"PS-1\" name=\"Placement Bound\" widthMm=\"256\" heightMm=\"1\" titleBlockName=\"\"><placements>");
            for (var i = 0; i < placementCount; i++)
            {
                payload.Append("<placement viewId=\"PV").Append(i)
                    .Append("\" xMm=\"").Append(i)
                    .Append("\" yMm=\"0\" widthMm=\"1\" heightMm=\"1\"/>");
            }
            return payload.Append("</placements></sheet></sheets></documentation>").ToString();
        }

        private static void EmptyCatalogClearsMetadata()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            store.Save(project, Array.Empty<SemanticViewDefinition>(), Array.Empty<SemanticSheetDefinition>());
            if (project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey))
                throw new Exception("Empty documentation catalog must remove the persisted metadata payload.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-DOC-PERSIST", "Documentation Persistence");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Families.Add(new ProjectFamily("FAM-B", "Beam 300x500", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A"));
            return project;
        }

        private static SemanticViewDefinition BuildView()
        {
            return new SemanticViewDefinition(
                "V-L02-BEAM",
                "L02 Beams",
                SemanticViewKind.Plan,
                floorId: "F-02",
                categories: new[] { ElementCategory.Beam });
        }

        private static SemanticSheetDefinition BuildSheet()
        {
            return new SemanticSheetDefinition(
                "S-A101",
                "A-101",
                "Beam Plan",
                841d,
                594d,
                new[] { new SemanticSheetPlacementDefinition("V-L02-BEAM", 20d, 20d, 380d, 250d) },
                "A1 Standard");
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static string MustFailMessage(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex) { return ex.Message; }
            throw new Exception("Expected InvalidOperationException.");
        }

        private static void MustFailLoad(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidDataException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private sealed class CountedSequence<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
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
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    EnumerationCount++;
                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
