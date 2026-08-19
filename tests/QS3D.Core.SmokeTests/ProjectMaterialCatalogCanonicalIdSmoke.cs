using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogCanonicalIdSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsPaddedDirectMaterialIds();
            RejectsPaddedUpsertIdsWithoutMutation();
            RejectsPaddedDeleteIdsWithoutMutation();
            PreservesCanonicalMaterialOperations();
        }

        private static void RejectsPaddedDirectMaterialIds()
        {
            ExpectThrows<ArgumentException>(() => new ProjectMaterial(" custom-direct", "Direct", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("custom-direct ", "Direct", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("\tcustom-direct", "Direct", "m", "", false));
        }

        private static void RejectsPaddedUpsertIdsWithoutMutation()
        {
            var project = NewProject("upsert");
            var beforeVersion = project.ChangeVersion;

            ExpectThrows<ArgumentException>(() =>
                ProjectMaterialCatalog.UpsertCustom(project, " custom-upsert ", "Custom Upsert", "m", ""));

            Equal(beforeVersion, project.ChangeVersion, "Rejected padded material upsert changed project version.");
            Equal(0, ProjectMaterialCatalog.GetCustom(project).Count, "Rejected padded material upsert mutated the catalog.");
        }

        private static void RejectsPaddedDeleteIdsWithoutMutation()
        {
            var project = NewProject("delete");
            ProjectMaterialCatalog.UpsertCustom(project, "custom-delete", "Custom Delete", "m", "");
            var beforeVersion = project.ChangeVersion;

            ExpectThrows<ArgumentException>(() => ProjectMaterialCatalog.DeleteCustom(project, " custom-delete "));

            Equal(beforeVersion, project.ChangeVersion, "Rejected padded material delete changed project version.");
            var remaining = ProjectMaterialCatalog.GetCustom(project);
            Equal(1, remaining.Count, "Rejected padded material delete removed the canonical material.");
            Equal("custom-delete", remaining[0].Id, "Rejected padded material delete changed canonical identity.");
        }

        private static void PreservesCanonicalMaterialOperations()
        {
            var project = NewProject("canonical");
            var created = ProjectMaterialCatalog.UpsertCustom(project, "custom-canonical", "Custom Canonical", "m", "Control");
            Equal("custom-canonical", created.Id, "Canonical material id changed during upsert.");
            Equal(1, ProjectMaterialCatalog.GetCustom(project).Count, "Canonical material was not persisted.");

            if (!ProjectMaterialCatalog.DeleteCustom(project, "custom-canonical"))
                throw new InvalidOperationException("Canonical material delete unexpectedly returned false.");
            Equal(0, ProjectMaterialCatalog.GetCustom(project).Count, "Canonical material remained after delete.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("project-material-id-" + suffix, "Material ID smoke");

        private static void ExpectThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal(long expected, long actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected='" + expected + "', actual='" + actual + "'.");
        }
    }
}
