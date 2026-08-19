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
            RejectsControlCharactersHiddenByIdNormalization();
            RejectsControlPaddedUpsertWithoutMutation();
            RejectsControlPaddedDeleteWithoutMutation();
            PreservesWhitespaceNormalizationAndCanonicalOperations();
        }

        private static void RejectsControlCharactersHiddenByIdNormalization()
        {
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("\tcustom-direct", "Direct", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("custom-direct\r", "Direct", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("custom\ndirect", "Direct", "m", "", false));
        }

        private static void RejectsControlPaddedUpsertWithoutMutation()
        {
            var project = NewProject("upsert");
            var beforeVersion = project.ChangeVersion;

            ExpectThrows<ArgumentException>(() =>
                ProjectMaterialCatalog.UpsertCustom(project, "\tcustom-upsert", "Custom Upsert", "m", ""));

            Equal(beforeVersion, project.ChangeVersion, "Rejected control-padded material upsert changed project version.");
            Equal(0, ProjectMaterialCatalog.GetCustom(project).Count, "Rejected control-padded material upsert mutated the catalog.");
        }

        private static void RejectsControlPaddedDeleteWithoutMutation()
        {
            var project = NewProject("delete");
            ProjectMaterialCatalog.UpsertCustom(project, "custom-delete", "Custom Delete", "m", "");
            var beforeVersion = project.ChangeVersion;

            ExpectThrows<ArgumentException>(() => ProjectMaterialCatalog.DeleteCustom(project, "custom-delete\n"));

            Equal(beforeVersion, project.ChangeVersion, "Rejected control-padded material delete changed project version.");
            var remaining = ProjectMaterialCatalog.GetCustom(project);
            Equal(1, remaining.Count, "Rejected control-padded material delete removed the canonical material.");
            Equal("custom-delete", remaining[0].Id, "Rejected control-padded material delete changed canonical identity.");
        }

        private static void PreservesWhitespaceNormalizationAndCanonicalOperations()
        {
            var project = NewProject("canonical");
            var created = ProjectMaterialCatalog.UpsertCustom(project, " custom-canonical ", "Custom Canonical", "m", "Control");
            Equal("custom-canonical", created.Id, "Ordinary surrounding-space normalization changed.");
            Equal(1, ProjectMaterialCatalog.GetCustom(project).Count, "Canonical material was not persisted.");

            if (!ProjectMaterialCatalog.DeleteCustom(project, " custom-canonical "))
                throw new InvalidOperationException("Space-normalized material delete unexpectedly returned false.");
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
