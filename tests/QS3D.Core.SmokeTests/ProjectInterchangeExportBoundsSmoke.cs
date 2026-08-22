using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeExportBoundsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SmallCanonicalProjectStillExports();
            ElementLimitFailsBeforeSemanticDuplicateValidation();
            TotalCollectionLimitFailsBeforeSemanticDuplicateValidation();
        }

        private static void SmallCanonicalProjectStillExports()
        {
            var project = new ProjectState("project-small", "Small Project");

            var json = ProjectInterchangeJsonExporter.Build(project);
            var validation = ProjectInterchangeJsonValidator.Validate(json);

            if (!validation.IsValid)
                throw new InvalidOperationException("A small canonical interchange project must remain exportable after collection preflight bounds are applied.");
        }

        private static void ElementLimitFailsBeforeSemanticDuplicateValidation()
        {
            var project = new ProjectState("project-elements", "Element Bound");
            var repeated = new ProjectElement("element-1", ElementCategory.Column);
            for (var index = 0; index <= ProjectInterchangeJsonValidator.MaxElements; index++)
                project.Elements.Add(repeated);

            var error = Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
            Contains(
                "more than " + ProjectInterchangeJsonValidator.MaxElements + " elements",
                error.Message,
                "The exporter must reject element count overflow before duplicate-id or other semantic traversal.");
        }

        private static void TotalCollectionLimitFailsBeforeSemanticDuplicateValidation()
        {
            var project = new ProjectState("project-collections", "Collection Bound");
            var repeated = new ZoneDefinition("zone-1", "Zone 1");
            for (var index = 0; index <= ProjectInterchangeJsonValidator.MaxCollectionItems; index++)
                project.Zones.Add(repeated);

            var error = Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
            Contains(
                ProjectInterchangeJsonValidator.MaxCollectionItems + "-item limit",
                error.Message,
                "The exporter must reject total collection overflow before duplicate-id or other semantic traversal.");
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if ((actual ?? string.Empty).IndexOf(expected ?? string.Empty, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Expected fragment: " + expected + ". Actual: " + actual + ".");
        }
    }
}
