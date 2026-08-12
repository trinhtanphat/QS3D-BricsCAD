using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditFamilyGlobalIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            UnrelatedDuplicateFamilyIdsFailBeforeMutation();
            ValidAssignmentStillWorks();
        }

        private static void UnrelatedDuplicateFamilyIdsFailBeforeMutation()
        {
            var project = new ProjectState("BULK-FAMILY-DUP", "Bulk Family duplicate identity");
            project.Families.Add(new ProjectFamily("F1", "Duplicate A", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("f1", "Duplicate B", ElementCategory.Beam));
            var target = new ProjectFamily("F2", "Target", ElementCategory.Beam);
            target.Properties["Material"] = "Steel";
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["Keep"] = "original";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdatedUtc = project.UpdatedUtc;
            var beforeElementUpdatedUtc = element.UpdatedUtc;

            try
            {
                new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);
            }
            catch (InvalidOperationException ex)
            {
                if ((ex.Message ?? string.Empty).IndexOf("duplicate family id", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Bulk Family duplicate preflight returned the wrong failure.", ex);

                Require(string.IsNullOrEmpty(element.FamilyId), "Rejected bulk Family assignment changed FamilyId.");
                Require(element.Properties.Count == 1 && element.Properties["Keep"] == "original",
                    "Rejected bulk Family assignment changed element properties.");
                Require(element.Dirty == ElementDirtyFlags.None, "Rejected bulk Family assignment dirtied the element.");
                Require(element.UpdatedUtc == beforeElementUpdatedUtc, "Rejected bulk Family assignment changed element persistence time.");
                Require(project.ChangeVersion == beforeVersion, "Rejected bulk Family assignment advanced ChangeVersion.");
                Require(project.UpdatedUtc == beforeProjectUpdatedUtc, "Rejected bulk Family assignment changed project UpdatedUtc.");
                return;
            }

            throw new InvalidOperationException("Bulk Family assignment accepted an unrelated duplicate Family-ID collection.");
        }

        private static void ValidAssignmentStillWorks()
        {
            var project = new ProjectState("BULK-FAMILY-VALID", "Bulk Family valid control");
            var target = new ProjectFamily("F2", "Target", ElementCategory.Beam);
            target.Properties["Material"] = "Steel";
            project.Families.Add(target);
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            Require(changed == 1, "Valid bulk Family assignment did not report one changed element.");
            Require(string.Equals(element.FamilyId, target.Id, StringComparison.Ordinal), "Valid bulk Family assignment did not bind the target Family.");
            Require(element.Properties.TryGetValue("Material", out var material) && string.Equals(material, "Steel", StringComparison.Ordinal),
                "Valid bulk Family assignment did not inherit target Family properties.");
            Require(element.Dirty != ElementDirtyFlags.None, "Valid bulk Family assignment did not dirty the element.");
            Require(project.ChangeVersion == beforeVersion + 1L, "Valid bulk Family assignment must advance ChangeVersion exactly once.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
