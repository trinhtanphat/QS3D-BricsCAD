using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyCreateNullPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullFamilyAtCatalogBoundaryWithoutMutation();
            PreservesValidCreate();
        }

        private static void RejectsNullFamilyAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("FAMILY-NULL-CREATE", "Family null create");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Room);
            project.Families.Add(family);

            var familyCount = project.Families.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                project.Families.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Family admission failed for the wrong parameter.", ex);
                if (project.Families.Count != familyCount)
                    throw new InvalidOperationException("Rejected null-Family admission must not change the Family collection.");
                if (project.ChangeVersion != changeVersion)
                    throw new InvalidOperationException("Rejected null-Family admission must not advance project ChangeVersion.");
                if (project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected null-Family admission must not change UpdatedUtc.");
                if (!ReferenceEquals(project.FindFamily("F1"), family))
                    throw new InvalidOperationException("Rejected null-Family admission must preserve existing Family lookup state.");
                return;
            }

            throw new InvalidOperationException("Family catalog must reject null entries before Family creation preflight can observe malformed state.");
        }

        private static void PreservesValidCreate()
        {
            var valid = new ProjectState("FAMILY-CREATE-OK", "Family create ok");
            var created = ProjectFamilyService.Create(valid, "F1", "Family 1", ElementCategory.Room);
            if (!ReferenceEquals(valid.FindFamily("F1"), created))
                throw new InvalidOperationException("Ordinary Family creation must publish the created Family into the project.");
            if (valid.Families.Count != 1 || valid.ChangeVersion != 1)
                throw new InvalidOperationException("Ordinary Family creation must add one Family and advance ChangeVersion once.");
        }
    }
}
