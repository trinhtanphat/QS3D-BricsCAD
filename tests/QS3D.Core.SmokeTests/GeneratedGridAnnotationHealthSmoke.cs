using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGridAnnotationHealthSmoke
    {
        private const string HandlesKey = "GeneratedGridAnnotationHandles";
        private const string BuiltLabelKey = "GeneratedGridAnnotationLabel";
        private const string OwnerProjectKey = "GeneratedGridAnnotationOwnerProjectId";
        private const string OwnerElementKey = "GeneratedGridAnnotationOwnerElementId";
        private const string OwnershipVersionKey = "GeneratedGridAnnotationOwnershipVersion";
        private const string BubbleRadiusKey = "GridBubbleRadiusM";
        private const string TextHeightKey = "GridTextHeightM";

        public static void Run()
        {
            NoMetadataIsOptional();
            HealthyMetadataHasNoAnnotationIssue();
            LabelChangeIsStale();
            CorruptOwnerAndHandleAreReported();
        }

        private static void NoMetadataIsOptional()
        {
            var project = Project();
            var grid = Grid(project, "G-1", "A");
            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            False(issues.Any(x => x.ElementId == grid.Id));
        }

        private static void HealthyMetadataHasNoAnnotationIssue()
        {
            var project = Project();
            var grid = Grid(project, "G-1", "A");
            AddHealthyAnnotation(project, grid, "A");

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            False(issues.Any(x => x.ElementId == grid.Id));
        }

        private static void LabelChangeIsStale()
        {
            var project = Project();
            var grid = Grid(project, "G-1", "A");
            AddHealthyAnnotation(project, grid, "A");
            grid.SetProperty(GridNamingService.GridLabelKey, "B");

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            True(issues.Any(x => x.ElementId == grid.Id && x.Code == "GRID_ANNOTATION_LABEL_STALE"));
        }

        private static void CorruptOwnerAndHandleAreReported()
        {
            var project = Project();
            var grid = Grid(project, "G-1", "A");
            AddHealthyAnnotation(project, grid, "A");
            grid.SetProperty(HandlesKey, "10;11;NOT-HEX;13;14;15");
            grid.SetProperty(OwnerProjectKey, "different-project");

            var issues = new GeneratedGridAnnotationHealthService().Inspect(project);
            True(issues.Any(x => x.ElementId == grid.Id && x.Code == "GRID_ANNOTATION_HANDLE_INVALID"));
            True(issues.Any(x => x.ElementId == grid.Id && x.Code == "GRID_ANNOTATION_PROJECT_MISMATCH"));
        }

        private static ProjectState Project() => new ProjectState("grid-annotation-health", "Grid Annotation Health");

        private static ProjectElement Grid(ProjectState project, string id, string label)
        {
            var grid = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            grid.SourceHandles.Add("A0");
            grid.SetProperty(GridNamingService.GridLabelKey, label);
            project.Elements.Add(grid);
            return grid;
        }

        private static void AddHealthyAnnotation(ProjectState project, ProjectElement grid, string builtLabel)
        {
            grid.SetProperty(HandlesKey, "10;11;12;13;14;15");
            grid.SetProperty(BuiltLabelKey, builtLabel);
            grid.SetProperty(OwnerProjectKey, project.ProjectId);
            grid.SetProperty(OwnerElementKey, grid.Id);
            grid.SetProperty(OwnershipVersionKey, "1");
            grid.SetProperty(BubbleRadiusKey, "0.25");
            grid.SetProperty(TextHeightKey, "0.18");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value) => True(!value);
    }
}
