using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyServiceXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidCreateAndDuplicateBeforeMutation();
            RejectsXmlInvalidRenameAndLookupBeforeMutation();
            RejectsXmlInvalidPropertyKeyBeforeMutation();
            SupplementaryUnicodeRoundTripsThroughServiceAndQsdb();
        }

        private static void RejectsXmlInvalidCreateAndDuplicateBeforeMutation()
        {
            var project = new ProjectState("FAMILY-SERVICE-CREATE", "Family service create XML");
            var source = ProjectFamilyService.Create(project, "F-SOURCE", "Source family", ElementCategory.ArchitecturalWall);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.Families.Count;

            Throws<ArgumentException>(() => ProjectFamilyService.Create(project, "F-\uD800", "Valid family", ElementCategory.ArchitecturalWall));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family id create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family id create changed project timestamp.");
            Require(project.Families.Count == beforeCount, "XML-invalid Family id create changed Family collection.");

            Throws<ArgumentException>(() => ProjectFamilyService.Create(project, "F-VALID", "Family \uD800", ElementCategory.ArchitecturalWall));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family name create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family name create changed project timestamp.");
            Require(project.Families.Count == beforeCount, "XML-invalid Family name create changed Family collection.");

            Throws<ArgumentException>(() => ProjectFamilyService.Duplicate(project, source.Id, "F-DUP-\uD800", "Duplicate family"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid duplicate Family id changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid duplicate Family id changed project timestamp.");
            Require(project.Families.Count == beforeCount, "XML-invalid duplicate Family id changed Family collection.");

            Throws<ArgumentException>(() => ProjectFamilyService.Duplicate(project, source.Id, "F-DUP", "Duplicate \uD800 family"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid duplicate Family name changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid duplicate Family name changed project timestamp.");
            Require(project.Families.Count == beforeCount, "XML-invalid duplicate Family name changed Family collection.");
        }

        private static void RejectsXmlInvalidRenameAndLookupBeforeMutation()
        {
            var project = new ProjectState("FAMILY-SERVICE-RENAME", "Family service rename XML");
            var family = ProjectFamilyService.Create(project, "F-RENAME", "Original family", ElementCategory.ArchitecturalWall);
            var beforeName = family.Name;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.Families.Count;

            Throws<ArgumentException>(() => ProjectFamilyService.Rename(project, family.Id, "Invalid \uD800 family"));
            Require(family.Name == beforeName, "XML-invalid Family rename changed prior name.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family rename changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family rename changed project timestamp.");
            Require(project.Families.Count == beforeCount, "XML-invalid Family rename changed Family collection.");

            Throws<ArgumentException>(() => ProjectFamilyService.Rename(project, "F-\uD800", "Unused"));
            Require(family.Name == beforeName, "XML-invalid Family lookup id changed Family name.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family lookup id changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family lookup id changed project timestamp.");
        }

        private static void RejectsXmlInvalidPropertyKeyBeforeMutation()
        {
            var project = new ProjectState("FAMILY-SERVICE-PROPERTY", "Family service property XML");
            var family = ProjectFamilyService.Create(project, "F-PROPERTY", "Property family", ElementCategory.ArchitecturalWall);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = family.Properties.Count;

            Throws<ArgumentException>(() => ProjectFamilyService.SetProperty(project, family.Id, "Key-\uD800", "valid"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family property key changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family property key changed project timestamp.");
            Require(family.Properties.Count == beforeCount, "XML-invalid Family property key changed Family properties.");

            Throws<ArgumentException>(() => ProjectFamilyService.RemoveProperty(project, family.Id, "Key-\uD800"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Family remove-property key changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Family remove-property key changed project timestamp.");
            Require(family.Properties.Count == beforeCount, "XML-invalid Family remove-property key changed Family properties.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughServiceAndQsdb()
        {
            const string compass = "\U0001F9ED";
            var familyId = "F-" + compass;
            var createdName = "Family " + compass;
            var renamedName = "Family renamed " + compass;
            var propertyKey = "Key-" + compass;
            var propertyValue = "Value " + compass;
            var project = new ProjectState("FAMILY-SERVICE-ROUNDTRIP", "Family service Unicode roundtrip");

            var family = ProjectFamilyService.Create(project, familyId, createdName, ElementCategory.ArchitecturalWall);
            ProjectFamilyService.Rename(project, family.Id, renamedName);
            ProjectFamilyService.SetProperty(project, family.Id, propertyKey, propertyValue);

            Require(family.Id == familyId, "Supplementary-Unicode Family id changed in service memory.");
            Require(family.Name == renamedName, "Supplementary-Unicode Family rename did not preserve exact text.");
            Require(family.Properties.TryGetValue(propertyKey, out var value) && value == propertyValue, "Supplementary-Unicode Family property did not preserve exact text.");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-family-service-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var roundTripped = loaded.FindFamily(familyId) ?? throw new InvalidOperationException("Supplementary-Unicode Family was not found after QSDB round-trip.");
                Require(roundTripped.Id == familyId, "Supplementary-Unicode Family id changed across QSDB round-trip.");
                Require(roundTripped.Name == renamedName, "Supplementary-Unicode Family name changed across QSDB round-trip.");
                Require(roundTripped.Properties.TryGetValue(propertyKey, out var loadedValue) && loadedValue == propertyValue, "Supplementary-Unicode Family property changed across QSDB round-trip.");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
