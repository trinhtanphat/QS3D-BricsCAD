using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogEmptyRecordSmoke
    {
        public static void Run()
        {
            MissingAndExactEmptyMetadataRemainEmpty();
            WhitespaceOnlyMetadataFailsClosed();
            EmptyPersistedRecordFailsClosed();
            CanonicalCatalogStillRoundTrips();
        }

        private static void MissingAndExactEmptyMetadataRemainEmpty()
        {
            var missing = new ProjectState("material-missing-catalog", "Material missing catalog");
            if (ProjectMaterialCatalog.GetCustom(missing).Count != 0)
                throw new InvalidOperationException("Missing material catalog metadata must remain an empty custom catalog.");

            var empty = new ProjectState("material-empty-catalog", "Material empty catalog");
            empty.Metadata[ProjectMaterialCatalog.MetadataKey] = string.Empty;
            if (ProjectMaterialCatalog.GetCustom(empty).Count != 0)
                throw new InvalidOperationException("Exact-empty material catalog metadata must preserve the compatibility empty-catalog behavior.");
        }

        private static void WhitespaceOnlyMetadataFailsClosed()
        {
            var spaces = new ProjectState("material-space-catalog", "Material space catalog");
            spaces.Metadata[ProjectMaterialCatalog.MetadataKey] = "   ";
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(spaces));

            var tab = new ProjectState("material-tab-catalog", "Material tab catalog");
            tab.Metadata[ProjectMaterialCatalog.MetadataKey] = "\t";
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(tab));
        }

        private static void EmptyPersistedRecordFailsClosed()
        {
            var project = CreateCanonicalCatalog();
            project.Metadata[ProjectMaterialCatalog.MetadataKey] += "\n\n";

            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(project));
        }

        private static void CanonicalCatalogStillRoundTrips()
        {
            var project = CreateCanonicalCatalog();
            var materials = ProjectMaterialCatalog.GetCustom(project);
            if (materials.Count != 1 ||
                !string.Equals(materials[0].Id, "custom-mortar", StringComparison.Ordinal) ||
                !string.Equals(materials[0].Name, "Vữa xây", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical material catalog no longer round-trips.");
        }

        private static ProjectState CreateCanonicalCatalog()
        {
            var project = new ProjectState("material-empty-record", "Material empty record");
            ProjectMaterialCatalog.UpsertCustom(project, "custom-mortar", "Vữa xây", "m³", "Vật liệu xây");
            return project;
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class ProjectMaterialCatalogEmptyRecordSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectMaterialCatalogEmptyRecordSmoke.Run();
        }
    }
}
