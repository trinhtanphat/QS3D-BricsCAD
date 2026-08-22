using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class ComprehensiveModelHealthService
    {
        private static readonly string[] GeneratedOutputCodeTokens =
        {
            "SHAPE_REBAR",
            "TIE_REBAR",
            "BEAM_STIRRUP",
            "SLAB_MESH",
            "WALL_MESH",
            "FOUNDATION_MESH",
            "CURTAIN_FRAME",
            "GRID_ANNOTATION"
        };

        public IReadOnlyList<ModelHealthIssue> Inspect(
            ProjectState project,
            ISet<string>? liveSourceHandles = null,
            ISet<string>? liveGeneratedSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            Add(issues, seen, new ModelHealthService().Inspect(project, liveSourceHandles, liveGeneratedSolidHandles));
            Add(issues, seen, new RoomFinishHealthService().Inspect(project));
            Add(issues, seen, new DependencyHealthService().Inspect(project));
            Add(issues, seen, new LevelReferenceHealthService().Inspect(project));
            Add(issues, seen, new GridNamingHealthService().Inspect(project));
            Add(issues, seen, new GeneratedGridAnnotationHealthService().Inspect(project));
            Add(issues, seen, new GeneratedHandleOwnershipHealthService().Inspect(project));
            Add(issues, seen, new GeneratedRebarOwnershipHealthService().Inspect(project));
            Add(issues, seen, new GeneratedGeometryStaleHealthService().Inspect(project));
            Add(issues, seen, new GeneratedRebarModeHealthService().Inspect(project));
            Add(issues, seen, new RebarFabricationQualificationHealthService().Inspect(project));
            Add(issues, seen, new GeneratedRebarHealthService().InspectAll(project, liveGeneratedSolidHandles, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedTieRebarHealthService().Inspect(project, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedBeamStirrupHealthService().Inspect(project, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedSlabMeshHealthService().Inspect(project, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedWallMeshHealthService().Inspect(project, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedFoundationMeshHealthService().Inspect(project, liveGeneratedSolidHandles));
            Add(issues, seen, new GeneratedCurtainFrameHealthService().Inspect(project, liveGeneratedSolidHandles));

            return issues.AsReadOnly();
        }

        public static bool TargetsGeneratedOutput(ModelHealthIssue issue)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            var code = (issue.Code ?? string.Empty).Trim();
            if (code.Length == 0) return false;
            if (code.IndexOf("GENERATED", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            foreach (var token in GeneratedOutputCodeTokens)
                if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static void Add(
            ICollection<ModelHealthIssue> target,
            ISet<string> seen,
            IEnumerable<ModelHealthIssue> source)
        {
            foreach (var issue in source)
            {
                if (issue == null) continue;
                var code = issue.Code ?? string.Empty;
                var elementId = issue.ElementId ?? string.Empty;
                var message = issue.Message ?? string.Empty;
                var key = code.EndsWith("_STALE", StringComparison.OrdinalIgnoreCase)
                    ? ((int)issue.Severity) + "\n" + code.ToUpperInvariant() + "\n" + elementId.ToUpperInvariant()
                    : ((int)issue.Severity) + "\n" + code.ToUpperInvariant() + "\n" + elementId.ToUpperInvariant() + "\n" + message;
                if (seen.Add(key)) target.Add(issue);
            }
        }
    }
}
