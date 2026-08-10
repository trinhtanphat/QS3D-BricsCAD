using System;
using System.Collections.Generic;
using System.Linq;
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
            GeneratedHandlesAreCaseInsensitive();
        }

        private static void SourceHandlesAreCaseInsensitive()
        {
            var project = new ProjectState("P-health-source-case", "Source handle case normalization");
            var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            element.SourceHandles.Add(" ab12 ");
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
            element.SourceHandles.Add(string.Empty);
            element.SourceHandles.Add(" DEAD ");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(project, new HashSet<string>(StringComparer.Ordinal));
            if (!issues.Any(x => x.Code == "ORPHAN_HANDLE" && string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ModelHealthHandleNormalizationSmoke: blank source-handle noise must not suppress a real orphan.");
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
    }
}