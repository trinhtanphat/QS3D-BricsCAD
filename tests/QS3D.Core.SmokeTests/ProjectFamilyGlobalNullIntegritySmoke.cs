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
            RejectsNullFamilyAtCatalogBoundaryWithoutMutation();
            PreservesValidTargetOperations();
        }

        private static void RejectsNullFamilyAtCatalogBoundaryWithoutMutation()
        {
            var project = new ProjectState("FAMILY-GLOBAL-NULL", "Family global null");
            var source = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            var target = new ProjectFamily("F2", "Family 2", ElementCategory.Beam);
            target.Properties["P"] = "V";
            project.Families.Add(source);
            project.Families.Add(target);

            var familyCount = project.Families.Count;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var targetName = target.Name;
            var targetProperty = target.Properties["P"];

            try
            {
                project.Families.Add(null!);
            }
            catch (ArgumentNullException ex)
            {
                if (!string.Equals(ex.ParamName, "item", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null Family admission failed for the wrong parameter.", ex);
                if (project.Families.Count != familyCount ||
                    project.ChangeVersion != changeVersion ||
                    project.UpdatedUtc != updatedUtc ||
                    !string.Equals(target.Name, targetName, StringComparison.Ordinal) ||
                    !string.Equals(target.Properties["P"], targetProperty, StringComparison.Ordinal) ||
                    !ReferenceEquals(project.FindFamily("F1"), source) ||
                    !ReferenceEquals(project.FindFamily("F2"), target))
                    throw new InvalidOperationException("Rejected null-Family admission mutated project state.");
                return;
            }

            throw new InvalidOperationException("Family catalog must reject null entries at the admission boundary.");
        }

        private static void PreservesValidTargetOperations()
        {
            var project = new ProjectState("FAMILY-GLOBAL-NULL-VALID", "Family global null valid");
            var source = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            var target = new ProjectFamily("F2", "Family 2", ElementCategory.Beam);
            project.Families.Add(source);
            project.Families.Add(target);
            var element = new ProjectElement("E1", ElementCategory.Beam, source.Id, string.Empty, string.Empty);
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
