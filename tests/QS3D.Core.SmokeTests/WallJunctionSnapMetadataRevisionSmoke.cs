using System;
using System.Globalization;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionSnapMetadataRevisionSmoke
    {
        private static readonly string[] PreviewKeys =
        {
            "WallJunctionSnapPreviewPlanHash",
            "WallJunctionSnapPreviewSourceFingerprint",
            "WallJunctionSnapPreviewCount",
            "WallJunctionSnapPreviewUtc",
            "WallJunctionSnapPreviewProjectId",
            "WallJunctionSnapPreviewChangeVersion"
        };

        internal static void Run()
        {
            PreviewPublicationMatchesFinalRevision();
            PreviewCleanupConsumesOneExplicitRevision();
            OrdinaryMetadataStillTouchesProject();
        }

        private static void PreviewPublicationMatchesFinalRevision()
        {
            var project = NewProject("preview");
            var before = project.ChangeVersion;

            project.Metadata[PreviewKeys[0]] = "plan";
            project.Metadata[PreviewKeys[1]] = "source";
            project.Metadata[PreviewKeys[2]] = "2";
            project.Metadata[PreviewKeys[3]] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            project.Metadata[PreviewKeys[4]] = project.ProjectId;
            Equal(before, project.ChangeVersion, "five preview metadata writes before audit");

            AuditTrail.ForProject(project).Record("wall.junction.snap.preview", string.Empty, "2 endpoint edit(s)");
            Equal(checked(before + 1L), project.ChangeVersion, "preview audit revision");

            var approvedVersion = checked(project.ChangeVersion + 1L);
            project.Metadata[PreviewKeys[5]] = approvedVersion.ToString(CultureInfo.InvariantCulture);
            Equal(checked(before + 1L), project.ChangeVersion, "preview version stamp metadata write");

            project.Touch();
            Equal(approvedVersion, project.ChangeVersion, "final preview revision");
            Equal(approvedVersion.ToString(CultureInfo.InvariantCulture), project.Metadata[PreviewKeys[5]], "preview stamped final revision");
        }

        private static void PreviewCleanupConsumesOneExplicitRevision()
        {
            var project = NewProject("cleanup");
            foreach (var key in PreviewKeys)
                project.Metadata[key] = key;
            project.Touch();

            var before = project.ChangeVersion;
            var removed = 0;
            foreach (var key in PreviewKeys)
            {
                if (project.Metadata.Remove(key)) removed++;
            }

            Equal(PreviewKeys.Length, removed, "preview cleanup removed key count");
            Equal(before, project.ChangeVersion, "preview cleanup metadata removals before explicit revision");

            project.Touch();
            Equal(checked(before + 1L), project.ChangeVersion, "preview cleanup explicit revision");
        }

        private static void OrdinaryMetadataStillTouchesProject()
        {
            var project = NewProject("ordinary");
            var before = project.ChangeVersion;
            project.Metadata["WallJunctionSnapPersistentControl"] = "value";
            Equal(checked(before + 1L), project.ChangeVersion, "ordinary metadata remains revision tracked");
        }

        private static ProjectState NewProject(string suffix)
        {
            return new ProjectState("wall-snap-revision-" + suffix, "Wall Snap Revision " + suffix);
        }

        private static void Equal(long expected, long actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected '" + expected + "', actual '" + actual + "'.");
        }
    }
}
