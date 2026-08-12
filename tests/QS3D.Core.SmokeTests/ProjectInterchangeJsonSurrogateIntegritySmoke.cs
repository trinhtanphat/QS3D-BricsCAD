using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeJsonSurrogateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsLoneHighSurrogate();
            RejectsLoneLowSurrogate();
            PreservesValidSurrogatePair();
        }

        private static void RejectsLoneHighSurrogate()
        {
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(CreateProject("P-\uD800")));
        }

        private static void RejectsLoneLowSurrogate()
        {
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(CreateProject("P-\uDC00")));
        }

        private static void PreservesValidSurrogatePair()
        {
            const string projectId = "P-\uD83D\uDE80";
            var json = ProjectInterchangeJsonExporter.Build(CreateProject(projectId));
            if (json.IndexOf(projectId, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Interchange JSON no longer preserves a valid UTF-16 surrogate pair.");

            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (!validation.IsValid)
                throw new InvalidOperationException("Interchange JSON containing a valid UTF-16 surrogate pair must remain canonical.");
        }

        private static ProjectState CreateProject(string projectId)
        {
            var project = new ProjectState(projectId, "Surrogate integrity");
            project.UpdatedUtc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
