using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeExportValidatorParitySmoke
    {
        private const int MaxNameLength = 512;
        private const int MaxPropertyValueLength = 32768;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcceptsCanonicalValidatorBoundaries();
            RejectsOversizedProjectName();
            RejectsOversizedPortablePropertyValue();
            RejectsInvalidSnapshotBeforeFilesystemMutation();
        }

        private static void AcceptsCanonicalValidatorBoundaries()
        {
            var project = new ProjectState("P-VALID", new string('N', MaxNameLength));
            var family = new ProjectFamily("F-VALID", "Family", ElementCategory.ArchitecturalWall);
            family.Properties["Description"] = new string('V', MaxPropertyValueLength);
            project.Families.Add(family);

            var json = ProjectInterchangeJsonExporter.Build(project);
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (!validation.IsValid)
                throw new InvalidOperationException("Canonical interchange export failed its own validator at the accepted boundaries.");
        }

        private static void RejectsOversizedProjectName()
        {
            var project = new ProjectState("P-NAME-LIMIT", new string('N', MaxNameLength + 1));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
        }

        private static void RejectsOversizedPortablePropertyValue()
        {
            var project = new ProjectState("P-PROPERTY-LIMIT", "Property limit");
            var family = new ProjectFamily("F-PROPERTY-LIMIT", "Family", ElementCategory.ArchitecturalWall);
            family.Properties["Description"] = new string('V', MaxPropertyValueLength + 1);
            project.Families.Add(family);

            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
        }

        private static void RejectsInvalidSnapshotBeforeFilesystemMutation()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-interchange-validator-parity-" + Guid.NewGuid().ToString("N"));
            DeleteDirectory(directory);
            var project = new ProjectState("P-FS-PREFLIGHT", new string('N', MaxNameLength + 1));
            try
            {
                Throws<InvalidDataException>(() =>
                    ProjectInterchangeJsonExporter.Export(Path.Combine(directory, "invalid.json"), project));
                if (Directory.Exists(directory))
                    throw new InvalidOperationException("Interchange export mutated the filesystem before canonical snapshot validation failed.");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
