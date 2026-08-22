using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialCatalogResourceBoundsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryCanonicalMetadataStillReads();
            OversizedRecordCountFailsClosed();
            ExcessFieldCountFailsClosed();
            OversizedSerializedMetadataFailsClosed();
        }

        private static void OrdinaryCanonicalMetadataStillReads()
        {
            var project = Project("MAT-BOUND-OK");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("custom-a", "Material A", "m2", "ordinary");
            var materials = ProjectMaterialCatalog.GetCustom(project);
            if (materials.Count != 1 ||
                !string.Equals(materials[0].Id, "custom-a", StringComparison.Ordinal) ||
                !string.Equals(materials[0].Name, "Material A", StringComparison.Ordinal))
                throw new InvalidOperationException("Ordinary canonical material metadata changed while bounding persisted parsing.");
        }

        private static void OversizedRecordCountFailsClosed()
        {
            var project = Project("MAT-BOUND-LINES");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = string.Join("\n", Enumerable.Repeat(Record("a", "A", "", ""), 501));
            ThrowsContaining(() => ProjectMaterialCatalog.GetCustom(project), "custom-material limit");
        }

        private static void ExcessFieldCountFailsClosed()
        {
            var project = Project("MAT-BOUND-FIELDS");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("a", "A", "", "") + "|extra";
            ThrowsContaining(() => ProjectMaterialCatalog.GetCustom(project), "Invalid material catalog record");
        }

        private static void OversizedSerializedMetadataFailsClosed()
        {
            var project = Project("MAT-BOUND-SIZE");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = new string('A', 1024 * 1024 + 1);
            ThrowsContaining(() => ProjectMaterialCatalog.GetCustom(project), "serialized safety limit");
        }

        private static ProjectState Project(string id) => new ProjectState(id, "Material resource bounds smoke");

        private static string Record(string id, string name, string unit, string description) =>
            string.Join("|", Encode(id), Encode(name), Encode(unit), Encode(description));

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

        private static void ThrowsContaining(Action action, string expected)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Material resource-bound guard failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("Material resource-bound guard accepted malformed persisted metadata.");
        }
    }
}
