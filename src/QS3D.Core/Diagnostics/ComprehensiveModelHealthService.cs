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

        private sealed class GeneratedHandleIdentityComparer : IEqualityComparer<string>
        {
            public static readonly GeneratedHandleIdentityComparer Instance = new GeneratedHandleIdentityComparer();

            public bool Equals(string left, string right) =>
                string.Equals(
                    GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(left),
                    GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(right),
                    StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(string value) =>
                StringComparer.OrdinalIgnoreCase.GetHashCode(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(value));
        }

        public IReadOnlyList<ModelHealthIssue> Inspect(
            ProjectState project,
            ISet<string>? liveSourceHandles = null,
            ISet<string>? liveGeneratedSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var normalizedLiveSourceHandles = NormalizeHandleSet(liveSourceHandles);
            var normalizedLiveGeneratedSolidHandles = NormalizeGeneratedHandleSet(liveGeneratedSolidHandles);
            var modelHealthLiveGeneratedSolidHandles = ExpandGeneratedHandleAliasesForModelHealth(project, normalizedLiveGeneratedSolidHandles);
            var issues = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            AddSafely(issues, seen, "ModelHealthService", () => new ModelHealthService().Inspect(project, normalizedLiveSourceHandles, modelHealthLiveGeneratedSolidHandles));
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

        private static ISet<string>? NormalizeGeneratedHandleSet(ISet<string>? handles)
        {
            if (handles == null) return null;
            var normalized = new HashSet<string>(GeneratedHandleIdentityComparer.Instance);
            foreach (var raw in handles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0) normalized.Add(handle);
            }
            return normalized;
        }

        private static ISet<string>? ExpandGeneratedHandleAliasesForModelHealth(ProjectState project, ISet<string>? liveHandles)
        {
            if (liveHandles == null) return null;
            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in liveHandles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0) expanded.Add(handle);
            }

            foreach (var element in project.Elements)
            {
                if (element == null || !element.Properties.TryGetValue("GeneratedSolidHandle", out var rawHandle)) continue;
                var persistedHandle = (rawHandle ?? string.Empty).Trim();
                if (persistedHandle.Length > 0 && liveHandles.Contains(persistedHandle)) expanded.Add(persistedHandle);
            }
            return expanded;
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
                        providerName + " could not complete diagnostics because project state is invalid. Review project data and run Model Health again.")
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
                if (issue == null)
                    throw new InvalidOperationException("Model health providers must not return null issues.");
                if (seen.Add(IssueKey(issue))) target.Add(issue);
            }
        }

        private static string IssueKey(ModelHealthIssue issue)
        {
            var code = issue.Code ?? string.Empty;
            var key = KeyPart(((int)issue.Severity).ToString(System.Globalization.CultureInfo.InvariantCulture)) +
                      KeyPart(code.ToUpperInvariant()) +
                      KeyPart((issue.ElementId ?? string.Empty).ToUpperInvariant());
            return code.EndsWith("_STALE", StringComparison.OrdinalIgnoreCase)
                ? key
                : key + KeyPart(issue.Message ?? string.Empty);
        }

        private static string KeyPart(string value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + text;
        }
    }
}