using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActiveDeleteCanonicalSmoke
    {
        public static void Run()
        {
            PaddedActiveIdBlocksDelete();
            CaseVariedPaddedActiveIdBlocksDelete();
            InactiveFamilyStillDeletes();
        }

        private static void PaddedActiveIdBlocksDelete()
        {
            var project = new ProjectState("P-FAMILY-DELETE-1", "Family delete test");
            var family = ProjectFamilyService.Create(project, "F-BEAM", "Beam A", ElementCategory.Beam);
            project.Metadata["ActiveFamilyId"] = "  F-BEAM  ";
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Families.Count;
            var beforeMetadata = project.Metadata["ActiveFamilyId"];

            ThrowsInvalid(() => ProjectFamilyService.Delete(project, family.Id));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeCount, project.Families.Count);
            Equal(beforeMetadata, project.Metadata["ActiveFamilyId"]);
            Same(family, project.FindFamily("F-BEAM"));
        }

        private static void CaseVariedPaddedActiveIdBlocksDelete()
        {
            var project = new ProjectState("P-FAMILY-DELETE-2", "Family delete case test");
            var family = ProjectFamilyService.Create(project, "Fam-Column", "Column A", ElementCategory.Column);
            project.Metadata["ActiveFamilyId"] = "  fAM-cOLUMN  ";
            var beforeVersion = project.ChangeVersion;

            ThrowsInvalid(() => ProjectFamilyService.Delete(project, " fam-COLUMN "));

            Equal(beforeVersion, project.ChangeVersion);
            Same(family, project.FindFamily("Fam-Column"));
        }

        private static void InactiveFamilyStillDeletes()
        {
            var project = new ProjectState("P-FAMILY-DELETE-3", "Family inactive delete test");
            var active = ProjectFamilyService.Create(project, "F-ACTIVE", "Beam Active", ElementCategory.Beam);
            var inactive = ProjectFamilyService.Create(project, "F-INACTIVE", "Beam Inactive", ElementCategory.Beam);
            project.Metadata["ActiveFamilyId"] = "  f-active  ";
            var beforeVersion = project.ChangeVersion;

            True(ProjectFamilyService.Delete(project, inactive.Id));

            True(project.ChangeVersion > beforeVersion);
            Same(active, ProjectFamilyActivationService.GetActive(project));
            Null(project.FindFamily(inactive.Id));
            Equal("  f-active  ", project.Metadata["ActiveFamilyId"]);
        }

        private static void ThrowsInvalid(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                Equal("Cannot delete the active Family. Activate another Family first.", ex.Message);
                return;
            }

            throw new Exception("Expected active Family deletion to fail.");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Null(object? value)
        {
            if (value != null) throw new Exception("Expected null.");
        }

        private static void Same(object expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected same object reference.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
