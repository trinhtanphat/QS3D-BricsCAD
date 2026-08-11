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
            "CURTAIN_PANEL",
            "GRID_ANNOTATION",
            "SEMANTIC_TAG"
        };

        public IReadOnlyList<ModelHealthIssue> Inspect(
            ProjectState project,
            ISet<string>? liveSourceHandles = null,
            ISet<string>? liveGeneratedSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var normalizedLiveSourceHandles = NormalizeHandleSet(liveSourceHandles);
            var normalizedLiveGeneratedSolidHandles = NormalizeHandleSet(liveGeneratedSolidHandles);
            var issues = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            AddSafely(issues, seen, "ModelHealthService", () => new ModelHealthService().Inspect(project, normalizedLiveSourceHandles, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "RoomFinishHealthService", () => new RoomFinishHealthService().Inspect(project));
            AddSafely(issues, seen, "SemanticScheduleHealthService", () => new SemanticScheduleHealthService().Inspect(project));
            AddSafely(issues, seen, "DependencyHealthService", () => new DependencyHealthService().Inspect(project));
            AddSafely(issues, seen, "LevelReferenceHealthService", () => new LevelReferenceHealthService().Inspect(project));
            AddSafely(issues, seen, "GridNamingHealthService", () => new GridNamingHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedGridAnnotationHealthService", () => new GeneratedGridAnnotationHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedSemanticTagHealthService", () => new GeneratedSemanticTagHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedHandleOwnershipHealthService", () => new GeneratedHandleOwnershipHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedRebarOwnershipHealthService", () => new GeneratedRebarOwnershipHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedGeometryStaleHealthService", () => new GeneratedGeometryStaleHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedRebarModeHealthService", () => new GeneratedRebarModeHealthService().Inspect(project));
            AddSafely(issues, seen, "RebarFabricationQualificationHealthService", () => new RebarFabricationQualificationHealthService().Inspect(project));
            AddSafely(issues, seen, "GeneratedRebarHealthService", () => new GeneratedRebarHealthService().InspectAll(project, normalizedLiveGeneratedSolidHandles, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedTieRebarHealthService", () => new GeneratedTieRebarHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedBeamStirrupHealthService", () => new GeneratedBeamStirrupHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedSlabMeshHealthService", () => new GeneratedSlabMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedWallMeshHealthService", () => new GeneratedWallMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedFoundationMeshHealthService", () => new GeneratedFoundationMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedCurtainFrameHealthService", () => new GeneratedCurtainFrameHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));
            AddSafely(issues, seen, "GeneratedCurtainPanelHealthService", () => new GeneratedCurtainPanelHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles));

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

        private static ISet<string>? NormalizeHandleSet(ISet<string>? handles)
        {
            if (handles == null) return null;
            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in handles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0) normalized.Add(handle);
            }
            return normalized;
        }

        private static void AddSafely(
            ICollection<ModelHealthIssue> target,
            ISet<string> seen,
            string providerName,
            Func<IEnumerable<ModelHealthIssue>> provider)
        {
            try
            {
                Add(target, seen, provider());
            }
            catch (Exception ex) when (IsDiagnosticDataFailure(ex))
            {
                Add(target, seen, new[]
                {
                    new ModelHealthIssue(
                        "HEALTH_PROVIDER_FAILED",
                        HealthSeverity.Error,
                        providerName + " không thể hoàn tất chẩn đoán do project state không hợp lệ: " + ex.Message)
                });
            }
        }

        private static bool IsDiagnosticDataFailure(Exception exception)
        {
            return exception is InvalidOperationException ||
                   exception is ArgumentException ||
                   exception is FormatException ||
                   exception is OverflowException ||
                   exception is KeyNotFoundException ||
                   exception is NullReferenceException;
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
