using System;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMeasurementWorkItemMappingIdentitySmoke
    {
        public static void Run()
        {
            ContainsAndRemoveHonorCaseInsensitiveMappingIdentity();
            OtherIdentityFieldsRemainUnchanged();
        }

        private static void ContainsAndRemoveHonorCaseInsensitiveMappingIdentity()
        {
            var project = CreateProject();
            project.MeasurementWorkItemMappings.Add(Mapping(
                "MAP-A",
                ElementCategory.Beam,
                "ITEM-A",
                "CLASS-A",
                "WORK-A"));

            var caseVariant = Mapping(
                "map-a",
                ElementCategory.Beam,
                "item-a",
                "CLASS-A",
                "WORK-A");

            Assert(
                project.MeasurementWorkItemMappings.Contains(caseVariant),
                "Contains should honor the catalog's case-insensitive MappingId/MeasurementItemId identity.");
            Assert(
                project.MeasurementWorkItemMappings.Remove(caseVariant),
                "Remove should honor the catalog's case-insensitive MappingId/MeasurementItemId identity.");
            Assert(
                project.MeasurementWorkItemMappings.Count == 0,
                "Case-variant Remove should remove exactly the stored mapping.");
        }

        private static void OtherIdentityFieldsRemainUnchanged()
        {
            var project = CreateProject();
            project.MeasurementWorkItemMappings.Add(Mapping(
                "MAP-A",
                ElementCategory.Beam,
                "ITEM-A",
                "CLASS-A",
                "WORK-A"));

            var classificationCaseVariant = Mapping(
                "map-a",
                ElementCategory.Beam,
                "item-a",
                "class-a",
                "WORK-A");
            Assert(
                !project.MeasurementWorkItemMappings.Contains(classificationCaseVariant),
                "ClassificationId comparison must remain ordinal/case-sensitive.");
            Assert(
                !project.MeasurementWorkItemMappings.Remove(classificationCaseVariant),
                "ClassificationId case variants must not remove the stored mapping.");

            var workItemCaseVariant = Mapping(
                "map-a",
                ElementCategory.Beam,
                "item-a",
                "CLASS-A",
                "work-a");
            Assert(
                !project.MeasurementWorkItemMappings.Contains(workItemCaseVariant),
                "WorkItemId comparison must remain ordinal/case-sensitive.");
            Assert(
                !project.MeasurementWorkItemMappings.Remove(workItemCaseVariant),
                "WorkItemId case variants must not remove the stored mapping.");

            var categoryVariant = Mapping(
                "map-a",
                ElementCategory.Slab,
                "item-a",
                "CLASS-A",
                "WORK-A");
            Assert(
                !project.MeasurementWorkItemMappings.Contains(categoryVariant),
                "Category comparison semantics must remain unchanged.");
            Assert(
                !project.MeasurementWorkItemMappings.Remove(categoryVariant),
                "A different category must not remove the stored mapping.");
            Assert(
                project.MeasurementWorkItemMappings.Count == 1,
                "Non-matching probes must leave the stored mapping unchanged.");
        }

        private static ProjectState CreateProject() =>
            new ProjectState("project-2914", "Mapping identity smoke");

        private static MeasurementWorkItemMapping Mapping(
            string mappingId,
            ElementCategory category,
            string measurementItemId,
            string classificationId,
            string workItemId) =>
            new MeasurementWorkItemMapping(
                mappingId,
                category,
                measurementItemId,
                classificationId,
                workItemId);

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
