using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogIdempotentUpsertSmoke
    {
        internal static void Run()
        {
            IdenticalNormalizedUpsertDoesNotTouchProject();
            RealUpdateStillTouchesOnce();
        }

        private static void IdenticalNormalizedUpsertDoesNotTouchProject()
        {
            var project = new ProjectState("material-noop", "Material No-op");
            ProjectMaterialCatalog.UpsertCustom(project, "MAT-1", "Custom Steel", "kg", "Grade A");
            var beforeVersion = project.ChangeVersion;
            var beforeMetadata = project.Metadata[ProjectMaterialCatalog.MetadataKey];

            var material = ProjectMaterialCatalog.UpsertCustom(
                project,
                " MAT-1 ",
                " Custom Steel ",
                " kg ",
                " Grade A ");

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeMetadata, project.Metadata[ProjectMaterialCatalog.MetadataKey]);
            Equal("MAT-1", material.Id);
            Equal("Custom Steel", material.Name);
            Equal("kg", material.Unit);
            Equal("Grade A", material.Description);
        }

        private static void RealUpdateStillTouchesOnce()
        {
            var project = new ProjectState("material-update", "Material Update");
            ProjectMaterialCatalog.UpsertCustom(project, "MAT-1", "Custom Steel", "kg", "Grade A");
            var beforeVersion = project.ChangeVersion;
            var beforeMetadata = project.Metadata[ProjectMaterialCatalog.MetadataKey];

            ProjectMaterialCatalog.UpsertCustom(project, "MAT-1", "Custom Steel", "kg", "Grade B");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            NotEqual(beforeMetadata, project.Metadata[ProjectMaterialCatalog.MetadataKey]);
            var custom = ProjectMaterialCatalog.GetCustom(project);
            Equal(1, custom.Count);
            Equal("Grade B", custom[0].Description);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void NotEqual<T>(T left, T right)
        {
            if (Equals(left, right))
                throw new Exception("Expected values to differ, both were " + left + ".");
        }
    }

    internal static class ProjectMaterialCatalogIdempotentUpsertSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectMaterialCatalogIdempotentUpsertSmoke.Run();
    }
}
