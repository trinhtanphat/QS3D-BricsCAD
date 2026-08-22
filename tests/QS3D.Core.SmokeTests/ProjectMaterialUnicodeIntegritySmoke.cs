using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialUnicodeIntegritySmoke
    {
        public static void Run()
        {
            MalformedSurrogatesAreRejected();
            RejectedUpsertDoesNotMutateProject();
            ValidSupplementaryUnicodeRoundTrips();
        }

        private static void MalformedSurrogatesAreRejected()
        {
            Throws<ArgumentException>(() => new ProjectMaterial("bad-high", "Bad\uD800", "m", string.Empty, false));
            Throws<ArgumentException>(() => new ProjectMaterial("bad-low", "Bad", "m", "Bad\uDC00", false));
        }

        private static void RejectedUpsertDoesNotMutateProject()
        {
            var project = new ProjectState("P-UNICODE-FAIL", "Unicode failure");
            var beforeVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => ProjectMaterialCatalog.UpsertCustom(project, "bad", "Bad\uD800", "m", string.Empty));

            if (project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Rejected malformed material Unicode advanced the project change version.");
            if (project.Metadata.ContainsKey(ProjectMaterialCatalog.MetadataKey))
                throw new InvalidOperationException("Rejected malformed material Unicode created catalog metadata.");
        }

        private static void ValidSupplementaryUnicodeRoundTrips()
        {
            const string scalar = "\uD83E\uDDF1";
            var project = new ProjectState("P-UNICODE-OK", "Unicode roundtrip");
            var id = "mat-" + scalar;
            var name = "Vật liệu " + scalar;
            var unit = "m²";
            var description = "Mô tả " + scalar;

            ProjectMaterialCatalog.UpsertCustom(project, id, name, unit, description);
            var material = ProjectMaterialCatalog.GetCustom(project).Single();

            if (!string.Equals(material.Id, id, StringComparison.Ordinal) ||
                !string.Equals(material.Name, name, StringComparison.Ordinal) ||
                !string.Equals(material.Unit, unit, StringComparison.Ordinal) ||
                !string.Equals(material.Description, description, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary material Unicode did not round-trip exactly.");
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

    internal static class ProjectMaterialUnicodeIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectMaterialUnicodeIntegritySmoke.Run();
        }
    }
}
