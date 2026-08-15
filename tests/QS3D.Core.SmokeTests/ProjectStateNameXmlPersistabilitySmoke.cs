using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateNameXmlPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidLoneSurrogateFailsBeforeMutation();
            SupplementaryUnicodeNameRoundTrips();
        }

        private static void InvalidLoneSurrogateFailsBeforeMutation()
        {
            var project = new ProjectState("P-NAME-XML-INVALID", "Original Project Name");
            var originalName = project.Name;
            var originalUpdatedUtc = project.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;
            var invalidName = new string(new[] { '\uD800' });

            Throws<ArgumentException>(() => project.Name = invalidName);

            if (!string.Equals(project.Name, originalName, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected XML-invalid project name mutated Name.");
            if (project.UpdatedUtc != originalUpdatedUtc)
                throw new InvalidOperationException("Rejected XML-invalid project name mutated UpdatedUtc.");
            if (project.ChangeVersion != originalChangeVersion)
                throw new InvalidOperationException("Rejected XML-invalid project name mutated ChangeVersion.");
        }

        private static void SupplementaryUnicodeNameRoundTrips()
        {
            const string expectedName = "QS3D \U0001F642 Supplementary Project";
            var root = Path.Combine(Path.GetTempPath(), "QS3D-ProjectNameXml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "project.qsdb");
            var project = new ProjectState("P-NAME-XML-VALID", "Initial Project Name")
            {
                Name = expectedName
            };

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                if (!string.Equals(loaded.Name, expectedName, StringComparison.Ordinal))
                    throw new InvalidOperationException("Supplementary-Unicode project name did not round-trip exactly through QSDB.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
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
