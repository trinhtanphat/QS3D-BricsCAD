using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeElementArrayBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactLimitSourceHandlesRemainExportable();
            FirstSourceHandleBeyondLimitFailsClosed();
            StableDependenciesRemainExportable();
        }

        private static void ExactLimitSourceHandlesRemainExportable()
        {
            var project = NewProject();
            var element = NewElement("E-limit");
            for (var i = 0; i < ProjectInterchangeJsonExporter.MaxElementStringArrayItems; i++)
                element.SourceHandles.Add("H" + i.ToString("X8"));
            project.Elements.Add(element);

            var json = ProjectInterchangeJsonExporter.Build(project);
            Contains(json, "H00000000", "exact-limit export lost the first source handle");
            Contains(json, "H00000FFF", "exact-limit export lost the final source handle");
        }

        private static void FirstSourceHandleBeyondLimitFailsClosed()
        {
            var project = NewProject();
            var element = NewElement("E-over");
            for (var i = 0; i <= ProjectInterchangeJsonExporter.MaxElementStringArrayItems; i++)
                element.SourceHandles.Add("H" + i.ToString("X8"));
            project.Elements.Add(element);

            var error = Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
            Contains(error.Message, "4096-item per-element limit", "over-limit failure did not identify the guarded element-array ceiling");
        }

        private static void StableDependenciesRemainExportable()
        {
            var project = NewProject();
            var source = NewElement("E-source");
            source.SourceHandles.Add("ABC123");
            var dependent = NewElement("E-dependent");
            dependent.DependsOn.Add(source.Id);
            project.Elements.Add(source);
            project.Elements.Add(dependent);

            var json = ProjectInterchangeJsonExporter.Build(project);
            Contains(json, "\"dependencies\": [\"E-source\"]", "stable dependency serialization changed");
            Contains(json, "\"sourceHandles\": [\"ABC123\"]", "stable source-handle serialization changed");
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("P-interchange-array-bound", "Interchange array bound")
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
            throw new InvalidOperationException("ProjectInterchangeElementArrayBoundSmoke expected " + typeof(T).Name + ".");
        }

        private static void Contains(string actual, string expected, string message)
        {
            if ((actual ?? string.Empty).IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("ProjectInterchangeElementArrayBoundSmoke: " + message + ".");
        }
    }
}
