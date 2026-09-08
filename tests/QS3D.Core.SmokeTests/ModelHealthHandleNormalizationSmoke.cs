using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthHandleNormalizationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SourceHandlesAreCaseInsensitive();
            BlankSourceHandlesDoNotMaskOrphans();
            BlankOnlySourceHandlesAreIgnored();
            GeneratedHandlesAreCaseInsensitive();
        }

        private static void SourceHandlesAreCaseInsensitive()
        {
            var project = new ProjectState("P-health-source-case", "Source handle case normalization");
            var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            AddPersistedSourceHandle(element, " ab12 ");
            project.Elements.Add(element);

            var liveHandles = new HashSet<string>(StringComparer.Ordinal) { "AB12" };
            var issues = new ModelHealthService().Inspect(project, liveHandles);
            if (issues.Any(x => x.Code == "ORPHAN_HANDLE" && string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ModelHealthHandleNormalizationSmoke: CAD source handles must compare case-insensitively after trimming.");
        }

        private static void BlankSourceHandlesDoNotMaskOrphans()
        {
            var project = new ProjectState("P-health-blank-orphan", "Blank source handle orphan detection");
            var element = new ProjectElement("E2", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            AddPersistedSourceHandle(element, string.Empty);
            AddPersistedSourceHandle(element, " DEAD ");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(project, new HashSet<string>(StringComparer.Ordinal));
            if (!issues.Any(x => x.Code == "ORPHAN_HANDLE" && string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ModelHealthHandleNormalizationSmoke: blank source-handle noise must not suppress a real orphan.");
        }

        private static void BlankOnlySourceHandlesAreIgnored()
        {
            var project = new ProjectState("P-health-blank-only", "Blank-only source handle normalization");
            var element = new ProjectElement("E4", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            AddPersistedSourceHandle(element, "   ");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(project, new HashSet<string>(StringComparer.Ordinal));
            if (issues.Any(x => x.Code == "ORPHAN_HANDLE" && string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ModelHealthHandleNormalizationSmoke: blank-only source handles must not create a false orphan.");
        }

        private static void GeneratedHandlesAreCaseInsensitive()
        {
            var project = new ProjectState("P-health-generated-case", "Generated handle case normalization");
            var element = new ProjectElement("E3", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = " feed ";
            element.Properties["GeneratedSolidCategory"] = ElementCategory.Room.ToString();
            element.Properties["GeneratedSolidOwnershipVersion"] = "1";
            element.Properties["GeneratedSolidOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedSolidOwnerElementId"] = element.Id;
            project.Elements.Add(element);

            var liveGeneratedHandles = new HashSet<string>(StringComparer.Ordinal) { "FEED" };
            var issues = new ModelHealthService().Inspect(project, liveGeneratedSolidHandles: liveGeneratedHandles);
            if (issues.Any(x => x.Code == "GENERATED_SOLID_MISSING" && string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ModelHealthHandleNormalizationSmoke: generated Solid3d handles must compare case-insensitively after trimming.");
        }

        private static void AddPersistedSourceHandle(ProjectElement element, string sourceHandle)
        {
            var relationField = typeof(ProjectElement).GetField("_sourceHandles", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted source-handle backing relation field is unavailable.");
            var relation = relationField.GetValue(element)
                ?? throw new InvalidOperationException("ProjectElement persisted source-handle backing relation is unavailable.");
            var valuesField = relation.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted source-handle backing values field is unavailable.");
            var values = valuesField.GetValue(relation) as List<string>
                ?? throw new InvalidOperationException("ProjectElement persisted source-handle backing values are unavailable.");
            values.Add(sourceHandle);
        }
    }
}