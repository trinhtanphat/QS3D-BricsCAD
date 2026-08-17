using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class ComprehensiveModelHealthService
    {
        private const int MaximumProviderParallelism = 4;
        internal const int MaximumLiveHandleInputs = 10000;
        private readonly int _maxDegreeOfParallelism;

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

        private sealed class DiagnosticProvider
        {
            public DiagnosticProvider(string name, Func<IEnumerable<ModelHealthIssue>> inspect, bool catchDiagnosticDataFailures = true)
            {
                Name = name ?? string.Empty;
                Inspect = inspect ?? throw new ArgumentNullException(nameof(inspect));
                CatchDiagnosticDataFailures = catchDiagnosticDataFailures;
            }

            public string Name { get; }
            public Func<IEnumerable<ModelHealthIssue>> Inspect { get; }
            public bool CatchDiagnosticDataFailures { get; }
        }

        private sealed class DiagnosticProviderResult
        {
            public List<ModelHealthIssue> Issues { get; } = new List<ModelHealthIssue>();
            public Exception? FatalException { get; set; }
        }

        public ComprehensiveModelHealthService(int? maxDegreeOfParallelism = null)
        {
            var safeDefault = Math.Max(1, Math.Min(MaximumProviderParallelism, Environment.ProcessorCount));
            var configured = maxDegreeOfParallelism ?? safeDefault;
            if (configured < 1 || configured > MaximumProviderParallelism)
                throw new ArgumentOutOfRangeException(
                    nameof(maxDegreeOfParallelism),
                    configured,
                    "Comprehensive model-health parallelism must be between 1 and " + MaximumProviderParallelism + ".");
            _maxDegreeOfParallelism = configured;
        }

        public IReadOnlyList<ModelHealthIssue> Inspect(
            ProjectState project,
            ISet<string>? liveSourceHandles = null,
            ISet<string>? liveGeneratedSolidHandles = null)
        {
            return InspectCore(project, liveSourceHandles, liveGeneratedSolidHandles, _maxDegreeOfParallelism);
        }

        private static IReadOnlyList<ModelHealthIssue> InspectCore(
            ProjectState project,
            ISet<string>? liveSourceHandles,
            ISet<string>? liveGeneratedSolidHandles,
            int maxDegreeOfParallelism)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var normalizedLiveSourceHandles = NormalizeHandleSet(liveSourceHandles, "source");
            var normalizedLiveGeneratedSolidHandles = NormalizeGeneratedHandleSet(liveGeneratedSolidHandles);
            var modelHealthLiveGeneratedSolidHandles = ExpandGeneratedHandleAliasesForModelHealth(project, normalizedLiveGeneratedSolidHandles);
            var providers = new[]
            {
                new DiagnosticProvider("ModelHealthService", () => new ModelHealthService().Inspect(project, null, modelHealthLiveGeneratedSolidHandles)),
                new DiagnosticProvider("TextualSourceLiveness", () => BuildTextualSourceLivenessIssues(project, normalizedLiveSourceHandles), false),
                new DiagnosticProvider("RoomFinishHealthService", () => new RoomFinishHealthService().Inspect(project)),
                new DiagnosticProvider("SemanticScheduleHealthService", () => new SemanticScheduleHealthService().Inspect(project)),
                new DiagnosticProvider("DependencyHealthService", () => new DependencyHealthService().Inspect(project)),
                new DiagnosticProvider("LevelReferenceHealthService", () => new LevelReferenceHealthService().Inspect(project)),
                new DiagnosticProvider("GridNamingHealthService", () => new GridNamingHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedGridAnnotationHealthService", () => new GeneratedGridAnnotationHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedSemanticTagHealthService", () => new GeneratedSemanticTagHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedHandleOwnershipHealthService", () => new GeneratedHandleOwnershipHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedRebarOwnershipHealthService", () => new GeneratedRebarOwnershipHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedGeometryStaleHealthService", () => new GeneratedGeometryStaleHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedRebarModeHealthService", () => new GeneratedRebarModeHealthService().Inspect(project)),
                new DiagnosticProvider("RebarFabricationQualificationHealthService", () => new RebarFabricationQualificationHealthService().Inspect(project)),
                new DiagnosticProvider("GeneratedRebarHealthService", () => new GeneratedRebarHealthService().InspectAll(project, normalizedLiveGeneratedSolidHandles, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedTieRebarHealthService", () => new GeneratedTieRebarHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedBeamStirrupHealthService", () => new GeneratedBeamStirrupHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedSlabMeshHealthService", () => new GeneratedSlabMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedWallMeshHealthService", () => new GeneratedWallMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedFoundationMeshHealthService", () => new GeneratedFoundationMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedCurtainFrameHealthService", () => new GeneratedCurtainFrameHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles)),
                new DiagnosticProvider("GeneratedCurtainPanelHealthService", () => new GeneratedCurtainPanelHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles))
            };

            var results = maxDegreeOfParallelism == 1
                ? ExecuteProvidersSequentially(providers)
                : ExecuteProvidersInParallel(providers, maxDegreeOfParallelism);
            var issues = new List<ModelHealthIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < results.Length; index++)
            {
                var result = results[index];
                Add(issues, seen, result.Issues);
                if (result.FatalException == null) continue;
                ExceptionDispatchInfo.Capture(result.FatalException).Throw();
            }

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

        private static DiagnosticProviderResult[] ExecuteProvidersInParallel(DiagnosticProvider[] providers, int maxDegreeOfParallelism)
        {
            var results = new DiagnosticProviderResult[providers.Length];
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            Parallel.For(0, providers.Length, options, index =>
            {
                results[index] = ExecuteProvider(providers[index]);
            });
            return results;
        }

        private static DiagnosticProviderResult[] ExecuteProvidersSequentially(DiagnosticProvider[] providers)
        {
            var results = new DiagnosticProviderResult[providers.Length];
            for (var index = 0; index < providers.Length; index++)
            {
                var result = ExecuteProvider(providers[index]);
                results[index] = result;
                if (result.FatalException != null) break;
            }
            return results;
        }

        private static DiagnosticProviderResult ExecuteProvider(DiagnosticProvider provider)
        {
            var result = new DiagnosticProviderResult();
            try
            {
                foreach (var issue in provider.Inspect())
                {
                    if (issue == null)
                        throw new InvalidOperationException("Model health providers must not return null issues.");
                    result.Issues.Add(issue);
                }
            }
            catch (Exception ex) when (provider.CatchDiagnosticDataFailures && IsDiagnosticDataFailure(ex))
            {
                result.Issues.Add(new ModelHealthIssue(
                    "HEALTH_PROVIDER_FAILED",
                    HealthSeverity.Error,
                    provider.Name + " could not complete diagnostics because project state is invalid. Review project data and run Model Health again."));
            }
            catch (Exception ex)
            {
                result.FatalException = ex;
            }
            return result;
        }

        private static ISet<string>? NormalizeHandleSet(ISet<string>? handles, string label)
        {
            if (handles == null) return null;
            ValidateKnownCounts(handles, label);

            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var observedCount = 0;
            foreach (var raw in handles)
            {
                observedCount++;
                if (observedCount > MaximumLiveHandleInputs)
                    throw LiveHandleInputTooLarge(label);

                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0) normalized.Add(handle);
            }
            return normalized;
        }

        private static ISet<string>? NormalizeGeneratedHandleSet(ISet<string>? handles)
        {
            if (handles == null) return null;
            ValidateKnownCounts(handles, "generated-solid");

            var normalized = new HashSet<string>(GeneratedHandleIdentityComparer.Instance);
            var observedCount = 0;
            foreach (var raw in handles)
            {
                observedCount++;
                if (observedCount > MaximumLiveHandleInputs)
                    throw LiveHandleInputTooLarge("generated-solid");

                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0) normalized.Add(handle);
            }
            return normalized;
        }

        private static void ValidateKnownCounts(ISet<string> handles, string label)
        {
            var counts = new List<int> { handles.Count };
            if (handles is IReadOnlyCollection<string> readOnly)
                counts.Add(readOnly.Count);
            if (handles is System.Collections.ICollection nonGeneric)
                counts.Add(nonGeneric.Count);

            for (var index = 0; index < counts.Count; index++)
            {
                if (counts[index] > MaximumLiveHandleInputs)
                    throw LiveHandleInputTooLarge(label);
            }

            for (var index = 0; index < counts.Count; index++)
            {
                if (counts[index] < 0)
                    throw new InvalidOperationException(
                        "Comprehensive model-health live " + label + " Handle input exposes a negative Count contract.");
            }

            var expected = counts[0];
            for (var index = 1; index < counts.Count; index++)
            {
                if (counts[index] == expected) continue;
                throw new InvalidOperationException(
                    "Comprehensive model-health live " + label + " Handle input exposes conflicting Count contracts.");
            }
        }

        private static InvalidOperationException LiveHandleInputTooLarge(string label)
        {
            return new InvalidOperationException(
                "Comprehensive model-health live " + label + " Handle input exceeds the supported bound of " + MaximumLiveHandleInputs + ".");
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

        private static IReadOnlyList<ModelHealthIssue> BuildTextualSourceLivenessIssues(
            ProjectState project,
            ISet<string>? liveSourceHandles)
        {
            var issues = new List<ModelHealthIssue>();
            if (liveSourceHandles == null) return issues.AsReadOnly();

            foreach (var element in project.Elements)
            {
                if (element == null) continue;

                var hasSourceHandle = false;
                var hasLiveSourceHandle = false;
                foreach (var raw in element.SourceHandles)
                {
                    var handle = (raw ?? string.Empty).Trim();
                    if (handle.Length == 0) continue;
                    hasSourceHandle = true;
                    if (!liveSourceHandles.Contains(handle)) continue;
                    hasLiveSourceHandle = true;
                    break;
                }

                if (hasSourceHandle && !hasLiveSourceHandle)
                {
                    issues.Add(new ModelHealthIssue(
                        "ORPHAN_HANDLE",
                        HealthSeverity.Error,
                        "Không còn tìm thấy đối tượng CAD nguồn.",
                        element.Id));
                }
            }
            return issues.AsReadOnly();
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
