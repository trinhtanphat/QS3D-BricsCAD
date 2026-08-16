using System;
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
    }
}
