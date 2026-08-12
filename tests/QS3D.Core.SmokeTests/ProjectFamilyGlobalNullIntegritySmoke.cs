using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyGlobalNullIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullFamilyAcrossTargetOperations();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullFamilyAcrossTargetOperations()
        {
            var project = new ProjectState("FAMILY-GLOBAL-NULL", "Family global null");
            var source = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            var target = new ProjectFamily("F2", "Family 2", ElementCategory.Beam);
            target.Properties["P"] = "V";
            project.Families.Add(source);
            project.Families.Add(target);
            project.Families.Add(null!);
            var element = new ProjectElement("E1", ElementCategory.Beam, source.Id);
            project.Elements.Add(element);

            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.Duplicate(project, "F2", "F3", "Family 3"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.Rename(project, "F2", "Family 2 renamed"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.SetProperty(project, "F2", "P", "V2"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.RemoveProperty(project, "F2", "P"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.Assign(project, "F2", new[] { element }));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.Delete(project, "F2"));
            AssertRejectedWithoutMutation(project, target, element, () => ProjectFamilyService.ReferenceCount(project, "F2"));
        }

        private static void AssertRejectedWithoutMutation(ProjectState project, ProjectFamily target, ProjectElement element, Action action)
        {
            var familyCount = project.Families.Count;
            var targetName = target.Name;
            var targetProperty = target.Properties["P"];
            var elementFamilyId = element.FamilyId;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project family collection contains a null family.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Family target operation returned an unexpected null-integrity error.", ex);
                if (project.Families.Count != familyCount ||
                    !string.Equals(target.Name, targetName, StringComparison.Ordinal) ||
                    !string.Equals(target.Properties["P"], targetProperty, StringComparison.Ordinal) ||
                    !string.Equals(element.FamilyId, elementFamilyId, StringComparison.Ordinal) ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc)
                    throw new InvalidOperationException("Rejected Family target operation mutated project state.");
                return;
            }

            throw new InvalidOperationException("Family target operation must reject a null Family collection entry.");
        }

        private static void PreservesValidTargetOperations()
        {
            var project = new ProjectState("FAMILY-GLOBAL-NULL-VALID", "Family global null valid");
            var source = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            var target = new ProjectFamily("F2", "Family 2", ElementCategory.Beam);
            project.Families.Add(source);
            project.Families.Add(target);
            var element = new ProjectElement("E1", ElementCategory.Beam, source.Id);
            project.Elements.Add(element);

            ProjectFamilyService.Rename(project, target.Id, "Family 2 renamed");
            if (ProjectFamilyService.Assign(project, target.Id, new[] { element }) != 1)
                throw new InvalidOperationException("Valid Family assignment must preserve its mutation result.");
            if (ProjectFamilyService.ReferenceCount(project, target.Id) != 1)
                throw new InvalidOperationException("Valid Family reference count must preserve its result.");
            if (!string.Equals(target.Name, "Family 2 renamed", StringComparison.Ordinal) ||
                !string.Equals(element.FamilyId, target.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid Family target operations changed behavior after null-integrity hardening.");
        }
    }
}
