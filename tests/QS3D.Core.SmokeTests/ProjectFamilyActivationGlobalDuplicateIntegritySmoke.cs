using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationGlobalDuplicateIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsAmbiguousFamilyIdentityAcrossActivationApis();
            PreservesValidActivationSemantics();
        }

        private static void RejectsAmbiguousFamilyIdentityAcrossActivationApis()
        {
            var project = new ProjectState("FAMILY-ACTIVE-DUP", "Family activation duplicate");
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("f1", "Family 1 duplicate", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("F2", "Family 2", ElementCategory.Beam));
            project.Metadata["ActiveFamilyId"] = "F2";

            AssertRejectedWithoutMutation(project, () => ProjectFamilyActivationService.GetActive(project));
            AssertRejectedWithoutMutation(project, () => ProjectFamilyActivationService.SetActive(project, "F2"));
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
                if (!string.Equals(ex.Message, "Project contains duplicate family id: f1.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Family activation returned an unexpected duplicate-integrity error.", ex);
                if (!string.Equals(project.Metadata["ActiveFamilyId"], active, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected Family activation operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Family activation API must reject globally duplicate Family identities.");
        }

        private static void PreservesValidActivationSemantics()
        {
            var project = new ProjectState("FAMILY-ACTIVE-VALID", "Family activation valid");
            var first = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            var second = new ProjectFamily("F2", "Family 2", ElementCategory.Beam);
            project.Families.Add(first);
            project.Families.Add(second);
            project.Metadata["ActiveFamilyId"] = "F1";

            if (!ReferenceEquals(ProjectFamilyActivationService.GetActive(project), first))
                throw new InvalidOperationException("Valid GetActive must return the canonical active Family instance.");

            var beforeNoOp = project.ChangeVersion;
            ProjectFamilyActivationService.SetActive(project, " f1 ");
            if (project.ChangeVersion != beforeNoOp || !string.Equals(project.Metadata["ActiveFamilyId"], "F1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical active-Family no-op must remain mutation-free.");

            ProjectFamilyActivationService.SetActive(project, "F2");
            if (!string.Equals(project.Metadata["ActiveFamilyId"], "F2", StringComparison.Ordinal) ||
                !ReferenceEquals(ProjectFamilyActivationService.GetActive(project), second))
                throw new InvalidOperationException("Valid active-Family switch must preserve canonical behavior.");

            project.Metadata["ActiveFamilyId"] = "MISSING";
            var beforeClear = project.ChangeVersion;
            ProjectFamilyActivationService.ClearIfMissing(project);
            if (project.Metadata.ContainsKey("ActiveFamilyId") || project.ChangeVersion != checked(beforeClear + 1L))
                throw new InvalidOperationException("Missing active Family cleanup must preserve its existing mutation behavior.");
        }
    }
}
