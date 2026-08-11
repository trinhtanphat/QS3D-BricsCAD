using System;
using System.IO;
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
            SameCatalogDoesNotTouchProjectTwice();
            LegacyV1LoadsAndMigratesOnSave();
            InvalidCatalogDoesNotReplaceStoredPayload();
            UnsafeXmlFailsClosed();
            EmptyCatalogClearsMetadata();
        }

        private static void CatalogRoundTripsThroughQsdb()
        {
            var project = BuildProject();
            var catalogStore = new SemanticDocumentationCatalogStore();
            catalogStore.Save(
                project,
                new[] { BuildView(), BuildScheduleView() },
                new[] { BuildSheet() },
                new[] { BuildSchedule() });

            if (!project.Metadata.TryGetValue(SemanticDocumentationCatalogStore.MetadataKey, out var payload))
                throw new Exception("Documentation catalog was not stored in project metadata.");
            if (payload.IndexOf("version=\"2\"", StringComparison.Ordinal) < 0)
                throw new Exception("Documentation catalog must persist schema version 2 after schedule support is enabled.");
            if (payload.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 || payload.IndexOf("ObjectId", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new Exception("Documentation catalog must not persist native drawing ownership identifiers.");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-doc-catalog-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                new QsdbProjectStore().Save(project, path);
                var reopened = new QsdbProjectStore().Load(path);
                var catalog = catalogStore.Load(reopened);
                Equal(2, catalog.Views.Count);
                Equal(true, HasView(catalog, "V-L02-BEAM"));
                Equal(true, HasView(catalog, "V-L02-BEAM-SCHEDULE"));
                Equal(1, catalog.Sheets.Count);
                Equal("A-101", catalog.Sheets[0].Number);
                Equal("V-L02-BEAM", catalog.Sheets[0].Placements[0].ViewId);
                Equal(841d, catalog.Sheets[0].WidthMm);
                Equal(1, catalog.Schedules.Count);
                Equal("SCH-L02-BEAM", catalog.Schedules[0].Id);
                Equal("V-L02-BEAM-SCHEDULE", catalog.Schedules[0].ViewId);
                Equal(2, catalog.Schedules[0].Columns.Count);
                Equal("Mark", catalog.Schedules[0].Columns[0].Header);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void SameCatalogDoesNotTouchProjectTwice()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(
                project,
                new[] { BuildView(), BuildScheduleView() },
                new[] { BuildSheet() },
                new[] { BuildSchedule() });
            var version = project.ChangeVersion;
            store.Save(
                project,
                new[] { BuildView(), BuildScheduleView() },
                new[] { BuildSheet() },
                new[] { BuildSchedule() });
            Equal(version, project.ChangeVersion);
        }

        private static void LegacyV1LoadsAndMigratesOnSave()
        {
            var project = BuildProject();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] =
                "<documentation version=\"1\"><views><view id=\"V-L02-BEAM\" name=\"L02 Beams\" kind=\"Plan\" floorId=\"F-02\" zoneId=\"\"><categories><category value=\"Beam\" /></categories><include /><exclude /></view></views><sheets /></documentation>";

            var store = new SemanticDocumentationCatalogStore();
            var legacy = store.Load(project);
            Equal(1, legacy.Views.Count);
            Equal(0, legacy.Schedules.Count);

            store.Save(project, legacy.Views, legacy.Sheets, legacy.Schedules);
            var migrated = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            if (migrated.IndexOf("version=\"2\"", StringComparison.Ordinal) < 0 ||
                migrated.IndexOf("<schedules", StringComparison.Ordinal) < 0)
                throw new Exception("Saving a valid v1 documentation catalog must migrate it to schema v2 with an explicit schedules container.");
        }

        private static void InvalidCatalogDoesNotReplaceStoredPayload()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(
                project,
                new[] { BuildView(), BuildScheduleView() },
                new[] { BuildSheet() },
                new[] { BuildSchedule() });
            var before = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var badSchedule = new SemanticScheduleDefinition(
                "SCH-BAD",
                "Broken",
                "V-404",
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
            MustFail(
                () => store.Save(
                    project,
                    new[] { BuildView(), BuildScheduleView() },
                    new[] { BuildSheet() },
                    new[] { badSchedule }),
                "Invalid schedule references must fail before metadata replacement.");
            Equal(before, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static void UnsafeXmlFailsClosed()
        {
            var project = BuildProject();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] =
                "<!DOCTYPE documentation [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><documentation version='2'><views/><sheets/><schedules/></documentation>";
            var failed = false;
            try { new SemanticDocumentationCatalogStore().Load(project); }
            catch (InvalidDataException) { failed = true; }
            if (!failed) throw new Exception("Documentation metadata containing a DTD must fail closed.");
        }

        private static void EmptyCatalogClearsMetadata()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();
            store.Save(project, new[] { BuildScheduleView() }, Array.Empty<SemanticSheetDefinition>(), new[] { BuildSchedule() });
            store.Save(
                project,
                Array.Empty<SemanticViewDefinition>(),
                Array.Empty<SemanticSheetDefinition>(),
                Array.Empty<SemanticScheduleDefinition>());
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
            var beam = new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A");
            beam.SetProperty("Mark", "B1");
            project.Elements.Add(beam);
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

        private static SemanticViewDefinition BuildScheduleView()
        {
            return new SemanticViewDefinition(
                "V-L02-BEAM-SCHEDULE",
                "L02 Beam Schedule Source",
                SemanticViewKind.Schedule,
                floorId: "F-02",
                categories: new[] { ElementCategory.Beam });
        }

        private static SemanticScheduleDefinition BuildSchedule()
        {
            return new SemanticScheduleDefinition(
                "SCH-L02-BEAM",
                "L02 Beam Schedule",
                "V-L02-BEAM-SCHEDULE",
                new[]
                {
                    new SemanticDocumentationColumn("Mark", "{P:Mark}"),
                    new SemanticDocumentationColumn("Element ID", "{Id}")
                });
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

        private static bool HasView(SemanticDocumentationCatalog catalog, string id)
        {
            foreach (var view in catalog.Views)
                if (string.Equals(view.Id, id, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
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
