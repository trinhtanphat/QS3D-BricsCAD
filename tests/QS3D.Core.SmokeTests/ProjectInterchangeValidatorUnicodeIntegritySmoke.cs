using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeValidatorUnicodeIntegritySmoke
    {
        private const string ProjectNameSentinel = "INTERCHANGE-VALIDATOR-UNICODE-SENTINEL";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LoneHighSurrogateFailsClosed();
            LoneLowSurrogateFailsClosed();
            SupplementaryUnicodeRoundTripsExactly();
        }

        private static void LoneHighSurrogateFailsClosed()
        {
            RequireMalformedUnicodeRejected(WithProjectName("Invalid-\uD800"), "lone high surrogate");
        }

        private static void LoneLowSurrogateFailsClosed()
        {
            RequireMalformedUnicodeRejected(WithProjectName("Invalid-\uDC00"), "lone low surrogate");
        }

        private static void SupplementaryUnicodeRoundTripsExactly()
        {
            const string expectedName = "Supplementary-\uD83D\uDE80";
            var json = BuildCanonicalJson(expectedName);
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (!validation.IsValid)
                throw new InvalidOperationException("Valid supplementary Unicode must pass interchange validation.");

            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(json);
            if (!string.Equals(expectedName, snapshot.Project.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("Typed interchange reading did not preserve supplementary Unicode exactly.");
        }

        private static void RequireMalformedUnicodeRejected(string json, string label)
        {
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (validation.IsValid || !validation.Issues.Any(x => string.Equals(x.Code, "JSON_UTF16", StringComparison.Ordinal)))
                throw new InvalidOperationException("Interchange validation must reject a " + label + " with JSON_UTF16.");

            try
            {
                ProjectInterchangeValidatedSnapshotReader.Read(json);
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException("Typed interchange reading must reject a " + label + " before materialization.");
        }

        private static string WithProjectName(string projectName)
        {
            var canonical = BuildCanonicalJson(ProjectNameSentinel);
            if (canonical.IndexOf(ProjectNameSentinel, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Interchange Unicode fixture did not reach the project-name field.");
            return canonical.Replace(ProjectNameSentinel, projectName);
        }

        private static string BuildCanonicalJson(string projectName)
        {
            var project = new ProjectState("P-INTERCHANGE-VALIDATOR-UNICODE", projectName)
            {
                UpdatedUtc = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            return ProjectInterchangeJsonExporter.Build(project);
        }
    }
}
