using System;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMeasurementWorkItemMappingCollectionSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("mapping-case-semantics", "Mapping case semantics");
            var mappings = project.MeasurementWorkItemMappings;
            var original = new MeasurementWorkItemMapping(
                "mapping-1",
                ElementCategory.Beam,
                "measurement-1",
                "class-1",
                "work-1");

            mappings.Add(original);
            Equal(1, mappings.Count);

            True(
                mappings.Contains(new MeasurementWorkItemMapping(
                    "MAPPING-1",
                    ElementCategory.Beam,
                    "measurement-1",
                    "class-1",
                    "work-1")),
                "MappingId identity should be case-insensitive.");

            True(
                mappings.Contains(new MeasurementWorkItemMapping(
                    "mapping-1",
                    ElementCategory.Beam,
                    "MEASUREMENT-1",
                    "class-1",
                    "work-1")),
                "MeasurementItemId identity should be case-insensitive.");

            False(
                mappings.Contains(new MeasurementWorkItemMapping(
                    "MAPPING-1",
                    ElementCategory.Beam,
                    "MEASUREMENT-1",
                    "CLASS-1",
                    "work-1")),
                "ClassificationId identity should remain case-sensitive.");

            False(
                mappings.Contains(new MeasurementWorkItemMapping(
                    "MAPPING-1",
                    ElementCategory.Beam,
                    "MEASUREMENT-1",
                    "class-1",
                    "WORK-1")),
                "WorkItemId identity should remain case-sensitive.");

            True(
                mappings.Remove(new MeasurementWorkItemMapping(
                    "MAPPING-1",
                    ElementCategory.Beam,
                    "MEASUREMENT-1",
                    "class-1",
                    "work-1")),
                "Remove should honor case-insensitive mapping and measurement-item identity.");
            Equal(0, mappings.Count);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException(message);
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual)
                throw new InvalidOperationException("Expected " + expected + " but found " + actual + ".");
        }
    }
}
