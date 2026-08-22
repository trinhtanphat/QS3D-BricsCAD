using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyCreateDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsExistingDuplicateIdsWithoutMutation();
            PreservesValidCreate();
        }

        private static void RejectsExistingDuplicateIdsWithoutMutation()
        {
            var project = new ProjectState("FAMILY-DUP-CREATE", "Family duplicate create");
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("f1", "Family 1 duplicate", ElementCategory.Beam));

            var familyCount = project.Families.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                ProjectFamilyService.Create(project, "F2", "Family 2", ElementCategory.Beam);
                throw new InvalidOperationException("Create must reject pre-existing duplicate Family ids.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project contains duplicate family id: f1.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Create must fail closed with the canonical duplicate-Family integrity error.", ex);
            }

            if (project.Families.Count != familyCount)
                throw new InvalidOperationException("Rejected Family creation must not change the Family collection.");
            if (project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Rejected Family creation must not advance ChangeVersion.");
            if (project.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected Family creation must not change UpdatedUtc.");
        }

        private static void PreservesValidCreate()
        {
            var project = new ProjectState("FAMILY-VALID-CREATE", "Family valid create");
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Beam));
            var changeVersion = project.ChangeVersion;

            var created = ProjectFamilyService.Create(project, "F2", "Family 2", ElementCategory.Beam);
            if (!string.Equals(created.Id, "F2", StringComparison.Ordinal) || project.Families.Count != 2)
                throw new InvalidOperationException("Valid Family creation must preserve the existing Create contract.");
            if (project.ChangeVersion != checked(changeVersion + 1L))
                throw new InvalidOperationException("Valid Family creation must advance ChangeVersion exactly once.");
        }
    }
}
