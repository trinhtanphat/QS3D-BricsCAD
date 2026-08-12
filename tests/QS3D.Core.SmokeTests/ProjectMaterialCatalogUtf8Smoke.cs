using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogUtf8Smoke
    {
        public static void Run()
        {
            InvalidUtf8CatalogFieldFailsClosed();
            ValidUnicodeMaterialRoundTrips();
        }

        private static void InvalidUtf8CatalogFieldFailsClosed()
        {
            var project = new ProjectState("material-utf8-corrupt", "Material UTF8 corrupt");
            // wyg= is Base64 for bytes C3 28: valid Base64 but invalid UTF-8.
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = "wyg=|TmFtZQ==|bQ==|";

            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(project));
        }

        private static void ValidUnicodeMaterialRoundTrips()
        {
            var project = new ProjectState("material-utf8-valid", "Material UTF8 valid");
            ProjectMaterialCatalog.UpsertCustom(project, "custom-vua", "Vữa tô", "m²", "Hoàn thiện tường");

            var materials = ProjectMaterialCatalog.GetCustom(project);
            if (materials.Count != 1)
                throw new InvalidOperationException("Expected one custom material after valid Unicode round-trip.");
            Equal("custom-vua", materials[0].Id);
            Equal("Vữa tô", materials[0].Name);
            Equal("m²", materials[0].Unit);
            Equal("Hoàn thiện tường", materials[0].Description);
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
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

    internal static class ProjectMaterialCatalogUtf8SmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectMaterialCatalogUtf8Smoke.Run();
        }
    }
}
