using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbFamilyCategoryReferenceSmoke
    {
        public static void Run()
        {
            MatchingCategoryRoundTrips();
            MismatchedCategoryFailsBeforePublication();
            PersistedMismatchFailsOnLoad();
            UnboundFamilyRemainsValid();
        }

        private static void MatchingCategoryRoundTrips()
        {
            var path = TempPath("matching");
            try
            {
                var project = ReferencedProject(ElementCategory.ArchitecturalWall, ElementCategory.ArchitecturalWall);
                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);

                if (loaded.Families.Count != 1 || loaded.Elements.Count != 1)
                    throw new Exception("Matching Family/element category fixture did not roundtrip.");
                if (loaded.Families[0].Category != ElementCategory.ArchitecturalWall ||
                    loaded.Elements[0].Category != ElementCategory.ArchitecturalWall ||
                    !string.Equals(loaded.Elements[0].FamilyId, loaded.Families[0].Id, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Matching Family/element category relation changed during QSDB roundtrip.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void MismatchedCategoryFailsBeforePublication()
        {
            var path = TempPath("save-reject");
            try
            {
                var project = ReferencedProject(ElementCategory.ArchitecturalWall, ElementCategory.ArchitecturalWall);
                project.Families[0].Category = ElementCategory.Room;

                var rejected = false;
                try { new QsdbProjectStore().Save(project, path); }
                catch (InvalidDataException) { rejected = true; }

                if (!rejected)
                    throw new Exception("QSDB persisted an element that referenced a Family of a different category.");
                if (File.Exists(path))
                    throw new Exception("Rejected Family/element category mismatch still published a primary QSDB file.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void PersistedMismatchFailsOnLoad()
        {
            var path = TempPath("load-reject");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(ReferencedProject(ElementCategory.ArchitecturalWall, ElementCategory.ArchitecturalWall), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var family = document.Root?.Element("families")?.Element("family")
                    ?? throw new Exception("Serialized Family fixture was not found.");
                family.SetAttributeValue("category", ElementCategory.Room.ToString());
                document.Save(path, SaveOptions.DisableFormatting);

                var rejected = false;
                try { store.Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected)
                    throw new Exception("QSDB loaded an element that referenced a persisted Family of a different category.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void UnboundFamilyRemainsValid()
        {
            var path = TempPath("unbound");
            try
            {
                var project = new ProjectState("qsdb-family-category-unbound", "QSDB Family category unbound");
                project.Elements.Add(new ProjectElement(
                    "E1",
                    ElementCategory.ArchitecturalWall,
                    string.Empty,
                    string.Empty,
                    string.Empty));

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);
                if (loaded.Elements.Count != 1 || !string.IsNullOrEmpty(loaded.Elements[0].FamilyId))
                    throw new Exception("Unbound Family relation did not remain valid through QSDB roundtrip.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static ProjectState ReferencedProject(ElementCategory familyCategory, ElementCategory elementCategory)
        {
            var project = new ProjectState("qsdb-family-category-reference", "QSDB Family category reference");
            var family = new ProjectFamily("F1", "Family", familyCategory);
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement(
                "E1",
                elementCategory,
                family.Id,
                string.Empty,
                string.Empty));
            return project;
        }

        private static string TempPath(string suffix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-family-category-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".qsdb");

        private static void Cleanup(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
        }
    }
}
