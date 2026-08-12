using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationWhitespaceRepairSmoke
    {
        public static void Run()
        {
            MissingKeyIsNoOp();
            WhitespaceOnlyMetadataIsCleared();
            ValidPaddedIdentityIsPreserved();
            MissingNonBlankIdentityIsCleared();
        }

        private static void MissingKeyIsNoOp()
        {
            var project = new ProjectState("P-ACTIVE-FAMILY-1", "Missing active key");
            var beforeVersion = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(beforeVersion, project.ChangeVersion);
            False(project.Metadata.ContainsKey("ActiveFamilyId"));
            Null(ProjectFamilyActivationService.GetActive(project));
        }

        private static void WhitespaceOnlyMetadataIsCleared()
        {
            var project = new ProjectState("P-ACTIVE-FAMILY-2", "Whitespace active key");
            project.Metadata["ActiveFamilyId"] = "   \t  ";
            var beforeVersion = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(beforeVersion + 1L, project.ChangeVersion);
            False(project.Metadata.ContainsKey("ActiveFamilyId"));
            Null(ProjectFamilyActivationService.GetActive(project));
        }

        private static void ValidPaddedIdentityIsPreserved()
        {
            var project = new ProjectState("P-ACTIVE-FAMILY-3", "Valid padded active key");
            var family = ProjectFamilyService.Create(project, "F-BEAM", "Beam", ElementCategory.Beam);
            project.Metadata["ActiveFamilyId"] = "  f-beam  ";
            var beforeVersion = project.ChangeVersion;
            var beforeRaw = project.Metadata["ActiveFamilyId"];

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeRaw, project.Metadata["ActiveFamilyId"]);
            Same(family, ProjectFamilyActivationService.GetActive(project));
        }

        private static void MissingNonBlankIdentityIsCleared()
        {
            var project = new ProjectState("P-ACTIVE-FAMILY-4", "Missing active Family");
            project.Metadata["ActiveFamilyId"] = "  F-MISSING  ";
            var beforeVersion = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(beforeVersion + 1L, project.ChangeVersion);
            False(project.Metadata.ContainsKey("ActiveFamilyId"));
            Null(ProjectFamilyActivationService.GetActive(project));
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
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
