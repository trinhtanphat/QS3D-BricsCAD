using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogDecodedTextCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalRecordLoads();
            NonCanonicalDecodedFieldFails(" MAT-1 ", "Material A", "kg", "note", "id");
            NonCanonicalDecodedFieldFails("MAT-1", " Material A ", "kg", "note", "name");
            NonCanonicalDecodedFieldFails("MAT-1", "Material A", " kg ", "note", "unit");
            NonCanonicalDecodedFieldFails("MAT-1", "Material A", "kg", " note ", "description");
        }

        private static void CanonicalRecordLoads()
        {
            var project = ProjectWithRecord("MAT-1", "Material A", string.Empty, string.Empty);
            var materials = ProjectMaterialCatalog.GetCustom(project);
            if (materials.Count != 1 ||
                !string.Equals(materials[0].Id, "MAT-1", StringComparison.Ordinal) ||
                !string.Equals(materials[0].Name, "Material A", StringComparison.Ordinal) ||
                materials[0].Unit.Length != 0 ||
                materials[0].Description.Length != 0)
                throw new InvalidOperationException("Canonical material catalog record did not load exactly.");
        }

        private static void NonCanonicalDecodedFieldFails(string id, string name, string unit, string description, string label)
        {
            var project = ProjectWithRecord(id, name, unit, description);
            try
            {
                ProjectMaterialCatalog.GetCustom(project);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("non-canonical decoded " + label + " text", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Material catalog rejected padded decoded " + label + " text for an unexpected reason.", ex);
            }
            throw new InvalidOperationException("Material catalog accepted padded decoded " + label + " text.");
        }

        private static ProjectState ProjectWithRecord(string id, string name, string unit, string description)
        {
            var project = new ProjectState("P-MATERIAL-DECODED-CANONICAL", "Material decoded canonicality smoke");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = string.Join("|",
                Encode(id),
                Encode(name),
                Encode(unit),
                Encode(description));
            return project;
        }

        private static string Encode(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }
}
