using System;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialOptionalTextSmoke
    {
        public static void Run()
        {
            RejectsDirectConstructorControls();
            RejectsUpsertControlsBeforeMutation();
            RejectsControlBearingPersistedRecords();
            PreservesCanonicalOptionalTextRoundTrip();
        }

        private static void RejectsDirectConstructorControls()
        {
            Throws<ArgumentException>(() => new ProjectMaterial("mat-tab", "Tab", "kg\tbar", "safe", false));
            Throws<ArgumentException>(() => new ProjectMaterial("mat-lf", "LF", "kg", "line1\nline2", false));
            Throws<ArgumentException>(() => new ProjectMaterial("mat-cr", "CR", "kg", "line1\rline2", false));
            Throws<ArgumentException>(() => new ProjectMaterial("mat-leading-tab", "Leading tab", "\tkg", "safe", false));
        }

        private static void RejectsUpsertControlsBeforeMutation()
        {
            var project = new ProjectState("p-optional-controls", "Optional material text controls");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-safe", "Safe material", "kg", "baseline");
            var metadataBefore = project.Metadata[ProjectMaterialCatalog.MetadataKey];
            var versionBefore = project.ChangeVersion;
            var updatedBefore = project.UpdatedUtc;

            Throws<ArgumentException>(() => ProjectMaterialCatalog.UpsertCustom(project, "mat-unit-tab", "Unit tab", "kg\tbar", "safe"));
            AssertUnchanged(project, metadataBefore, versionBefore, updatedBefore, "TAB-bearing Unit");

            Throws<ArgumentException>(() => ProjectMaterialCatalog.UpsertCustom(project, "mat-description-lf", "Description LF", "kg", "line1\nline2"));
            AssertUnchanged(project, metadataBefore, versionBefore, updatedBefore, "LF-bearing Description");

            Throws<ArgumentException>(() => ProjectMaterialCatalog.UpsertCustom(project, "mat-description-cr", "Description CR", "kg", "line1\rline2"));
            AssertUnchanged(project, metadataBefore, versionBefore, updatedBefore, "CR-bearing Description");
        }

        private static void RejectsControlBearingPersistedRecords()
        {
            var project = new ProjectState("p-tampered-optional-controls", "Tampered optional material controls");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("mat-unit", "Tampered unit", "kg\tbar", "safe");
            Throws<ArgumentException>(() => ProjectMaterialCatalog.GetCustom(project));

            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("mat-description", "Tampered description", "kg", "line1\nline2");
            Throws<ArgumentException>(() => ProjectMaterialCatalog.GetCustom(project));
        }

        private static void PreservesCanonicalOptionalTextRoundTrip()
        {
            var project = new ProjectState("p-valid-optional-text", "Valid optional material text");
            var created = ProjectMaterialCatalog.UpsertCustom(
                project,
                "mat-unicode",
                "Unicode material",
                "  kg  ",
                "  Finish 😀 supplementary  ");

            if (created.Unit != "kg")
                throw new Exception("Ordinary surrounding optional Unit whitespace must remain trim-normalized.");
            if (created.Description != "Finish 😀 supplementary")
                throw new Exception("Valid supplementary-plane Description text must survive trim normalization.");

            var roundTrip = ProjectMaterialCatalog.GetCustom(project)[0];
            if (roundTrip.Unit != created.Unit || roundTrip.Description != created.Description)
                throw new Exception("Canonical optional material text did not survive Base64 persistence round-trip.");

            var empty = new ProjectMaterial("mat-empty", "Empty optional", "   ", "   ", false);
            if (empty.Unit.Length != 0 || empty.Description.Length != 0)
                throw new Exception("Whitespace-only optional material text must remain canonically empty.");
        }

        private static void AssertUnchanged(ProjectState project, string metadataBefore, long versionBefore, DateTime updatedBefore, string label)
        {
            if (!project.Metadata.TryGetValue(ProjectMaterialCatalog.MetadataKey, out var metadataAfter) ||
                !string.Equals(metadataAfter, metadataBefore, StringComparison.Ordinal) ||
                project.ChangeVersion != versionBefore ||
                project.UpdatedUtc != updatedBefore)
                throw new Exception("Rejected " + label + " must not mutate material catalog/project state.");
        }

        private static string Record(string id, string name, string unit, string description)
        {
            return string.Join("|", Encode(id), Encode(name), Encode(unit), Encode(description));
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
