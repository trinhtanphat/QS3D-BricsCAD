using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogNameControlSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsControlCharactersBeforeNameNormalization();
            RejectsControlPaddedUpsertWithoutMutation();
            PreservesOrdinarySpaceNormalization();
        }

        private static void RejectsControlCharactersBeforeNameNormalization()
        {
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("material-name-direct-1", "\tDirect", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("material-name-direct-2", "Direct\r", "m", "", false));
            ExpectThrows<ArgumentException>(() => new ProjectMaterial("material-name-direct-3", "Direct\nName", "m", "", false));
        }

        private static void RejectsControlPaddedUpsertWithoutMutation()
        {
            var project = NewProject("rejected");
            var beforeVersion = project.ChangeVersion;

            ExpectThrows<ArgumentException>(() =>
                ProjectMaterialCatalog.UpsertCustom(project, "material-name-upsert", "\tCustom Material", "m", ""));

            Equal(beforeVersion, project.ChangeVersion, "Rejected control-padded material name changed project version.");
            Equal(0, ProjectMaterialCatalog.GetCustom(project).Count, "Rejected control-padded material name mutated the catalog.");
        }

        private static void PreservesOrdinarySpaceNormalization()
        {
            var project = NewProject("spaces");
            var material = ProjectMaterialCatalog.UpsertCustom(project, "material-name-spaces", "  Custom Material  ", "m", "");

            Equal("Custom Material", material.Name, "Ordinary surrounding-space normalization changed.");
            Equal(1, ProjectMaterialCatalog.GetCustom(project).Count, "Valid material was not persisted.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("project-material-name-" + suffix, "Material name smoke");

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
