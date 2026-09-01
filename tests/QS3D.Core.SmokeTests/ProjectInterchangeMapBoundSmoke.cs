using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeMapBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactLimitPortablePropertiesRemainExportable();
            FirstPortablePropertyBeyondLimitFailsClosed();
            StablePropertyAndQuantityMapsRemainExportable();
        }

        private static void ExactLimitPortablePropertiesRemainExportable()
        {
            var project = NewProject();
            var element = NewElement("E-limit");
            for (var i = 0; i < ProjectInterchangeJsonExporter.MaxInterchangeMapItems; i++)
                element.Properties["Custom" + i.ToString("D4")] = "V" + i.ToString("D4");
            project.Elements.Add(element);

            var json = ProjectInterchangeJsonExporter.Build(project);
            Contains(json, "\"Custom0000\":\"V0000\"", "exact-limit export lost the first portable property");
            Contains(json, "\"Custom4095\":\"V4095\"", "exact-limit export lost the final portable property");
        }

        private static void FirstPortablePropertyBeyondLimitFailsClosed()
        {
            var project = NewProject();
            var element = NewElement("E-over");
            for (var i = 0; i <= ProjectInterchangeJsonExporter.MaxInterchangeMapItems; i++)
                element.Properties["Custom" + i.ToString("D4")] = "V" + i.ToString("D4");
            project.Elements.Add(element);

            var error = Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
            Contains(error.Message, "4096-member map limit", "over-limit failure did not identify the guarded map ceiling");
        }

        private static void StablePropertyAndQuantityMapsRemainExportable()
        {
            var project = NewProject();
            var element = NewElement("E-stable");
            element.Properties["Material"] = "Concrete";
            element.Quantities["Area"] = 12.5;
            project.Elements.Add(element);

            var json = ProjectInterchangeJsonExporter.Build(project);
            Contains(json, "\"Material\":\"Concrete\"", "stable portable property serialization changed");
            Contains(json, "\"Area\":12.5", "stable quantity serialization changed");
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("P-interchange-map-bound", "Interchange map bound")
            {
                UpdatedUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        private static ProjectElement NewElement(string id)
        {
            return new ProjectElement(id, ElementCategory.ArchitecturalWall);
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new InvalidOperationException("ProjectInterchangeMapBoundSmoke expected " + typeof(T).Name + ".");
        }

        private static void Contains(string actual, string expected, string message)
        {
            if ((actual ?? string.Empty).IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("ProjectInterchangeMapBoundSmoke: " + message + ".");
        }
    }
}
