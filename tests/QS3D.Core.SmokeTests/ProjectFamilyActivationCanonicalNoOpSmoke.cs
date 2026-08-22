using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationCanonicalNoOpSmoke
    {
        public static void Run()
        {
            CanonicalEquivalentSelectionIsNoOp();
            DifferentSelectionMutatesOnce();
        }

        private static void CanonicalEquivalentSelectionIsNoOp()
        {
            var project = new ProjectState("P-ACTIVE-CANONICAL-1", "Canonical active Family");
            var family = ProjectFamilyService.Create(project, "F-BEAM", "Beam", ElementCategory.Beam);
            project.Metadata["ActiveFamilyId"] = "  f-beam  ";
            var beforeVersion = project.ChangeVersion;
            var beforeRaw = project.Metadata["ActiveFamilyId"];

            ProjectFamilyActivationService.SetActive(project, " f-BEAM ");

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeRaw, project.Metadata["ActiveFamilyId"]);
            Same(family, ProjectFamilyActivationService.GetActive(project));
        }

        private static void DifferentSelectionMutatesOnce()
        {
            var project = new ProjectState("P-ACTIVE-CANONICAL-2", "Changed active Family");
            ProjectFamilyService.Create(project, "F-BEAM", "Beam", ElementCategory.Beam);
            var column = ProjectFamilyService.Create(project, "F-COLUMN", "Column", ElementCategory.Column);
            project.Metadata["ActiveFamilyId"] = "  f-beam  ";
            var beforeVersion = project.ChangeVersion;

            ProjectFamilyActivationService.SetActive(project, " f-column ");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(column.Id, project.Metadata["ActiveFamilyId"]);
            Same(column, ProjectFamilyActivationService.GetActive(project));
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
