using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogRevisionSmoke
    {
        internal static void Run()
        {
            CatalogMutationTouchesProjectExactlyOnce();
            CatalogUsesLastAvailableRevision();
        }

        private static void CatalogMutationTouchesProjectExactlyOnce()
        {
            var project = BuildProject();
            var store = new SemanticDocumentationCatalogStore();

            Equal(0L, project.ChangeVersion);
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            Equal(1L, project.ChangeVersion);
            True(project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            store.Save(project, new[] { BuildView() }, new[] { BuildSheet() });
            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);

            store.Save(project, Array.Empty<SemanticViewDefinition>(), Array.Empty<SemanticSheetDefinition>());
            Equal(version + 1L, project.ChangeVersion);
            True(!project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));
        }

        private static void CatalogUsesLastAvailableRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-doc-catalog-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("DOC-REV", "Documentation revision"), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for documentation catalog revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var project = store.Load(path);
                Equal(long.MaxValue - 1L, project.ChangeVersion);

                var catalogStore = new SemanticDocumentationCatalogStore();
                catalogStore.Save(project, new[] { BuildView() }, new[] { BuildSheet() });

                Equal(long.MaxValue, project.ChangeVersion);
                True(project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));

                var beforeRejectedUpdatedUtc = project.UpdatedUtc;
                var beforeRejectedMetadata = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
                var rejected = false;
                try
                {
                    var changedView = new SemanticViewDefinition(
                        "V-L02-BEAM",
                        "L02 Beams Changed",
                        SemanticViewKind.Plan,
                        floorId: "F-02",
                        categories: new[] { ElementCategory.Beam });
                    catalogStore.Save(project, new[] { changedView }, new[] { BuildSheet() });
                }
                catch (OverflowException)
                {
                    rejected = true;
                }

                True(rejected);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-DOC-REV", "Documentation Revision");
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new Exception("Expected condition to be true.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
