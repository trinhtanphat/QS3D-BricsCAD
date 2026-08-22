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
            RejectsNullFamilyAcrossActivationApis();
            PreservesValidActivationRead();
        }

        private static void RejectsNullFamilyAcrossActivationApis()
        {
            var project = new ProjectState("FAMILY-ACTIVE-NULL", "Family activation null");
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Beam));
            project.Families.Add(null!);
            project.Metadata["ActiveFamilyId"] = "F1";

            AssertRejectedWithoutMutation(project, () => { _ = ProjectFamilyActivationService.GetActive(project); });
            AssertRejectedWithoutMutation(project, () => ProjectFamilyActivationService.SetActive(project, "F1"));
            AssertRejectedWithoutMutation(project, () => ProjectFamilyActivationService.ClearIfMissing(project));
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, Action action)
        {
            var active = project.Metadata["ActiveFamilyId"];
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project family collection contains a null family.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Family activation returned an unexpected null-integrity error.", ex);
                if (!string.Equals(project.Metadata["ActiveFamilyId"], active, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected null-Family activation operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Family activation API must reject null Family entries.");
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
