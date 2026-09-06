using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationNullIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullFamilyAtCatalogBoundaryWithoutMutation();
            PreservesValidActivationRead();
        }

        private static void RejectsNullFamilyAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("FAMILY-ACTIVE-NULL", "Family activation null");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = "F1";

            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var familyCount = project.Families.Count;

            try
            {
                project.Families.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Family admission failed for the wrong parameter.", ex);
                if (project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc ||
                    project.Families.Count != familyCount)
                    throw new InvalidOperationException("Rejected null-Family admission mutated project catalog state.");
                if (!string.Equals(project.Metadata["ActiveFamilyId"], "F1", StringComparison.Ordinal) ||
                    !ReferenceEquals(ProjectFamilyActivationService.GetActive(project), family))
                    throw new InvalidOperationException("Rejected null-Family admission changed valid activation state.");
                return;
            }

            throw new InvalidOperationException("Family catalog must reject null entries at the admission boundary.");
        }

        private static void PreservesValidActivationRead()
        {
            var project = new ProjectState("FAMILY-ACTIVE-NULL-CONTROL", "Family activation null control");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = "F1";

            if (!ReferenceEquals(ProjectFamilyActivationService.GetActive(project), family))
                throw new InvalidOperationException("Valid Family activation lookup changed while adding null-integrity validation.");
        }
    }
}
