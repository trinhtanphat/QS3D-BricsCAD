using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class Map02ProjectOwnedCoverageSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("map02-project-owned", "MAP-02 smoke");
            var element = new ProjectElement("wall-1", ElementCategory.ArchitecturalWall);
            element.SetQuantity("NetWallAreaM2", 12.5d);
            project.Elements.Add(element);
            project.MeasurementWorkItemMappings.Add(new MeasurementWorkItemMapping(
                "map-wall", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-wall", "work-wall"));

            var owned = MeasurementWorkItemCoverageEvaluator.Evaluate(project).Single();
            if (owned.Mapping == null || owned.Mapping.MappingId != "map-wall" || owned.Issues.Contains(MeasurementWorkItemCoverageIssue.UnmappedWorkItem))
                throw new Exception("Project-owned coverage did not consume the project's canonical mapping state.");

            var external = MeasurementWorkItemCoverageEvaluator.Evaluate(project, new MeasurementWorkItemMappingCatalog(Array.Empty<MeasurementWorkItemMapping>())).Single();
            if (external.Mapping != null || !external.Issues.Contains(MeasurementWorkItemCoverageIssue.UnmappedWorkItem))
                throw new Exception("Explicit catalog coverage no longer preserves scenario-specific mapping semantics.");
        }
    }
}
