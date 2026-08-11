using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only licensed-runtime probe for the basic LOCAL-002 LINE curtain
    /// panel path. The caller must use a disposable synthetic drawing copy. The
    /// marker contains aggregate counts only; native Handles and local paths are
    /// deliberately excluded.
    /// </summary>
    public sealed class CurtainPanelRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_NONCE";
        private const string ResultFileName = "curtain-panel-runtime-result.txt";

        [CommandMethod("QS3DCURTAINPANELPREPARE", CommandFlags.Modal)]
        public void PrepareSourceSelection()
        {
            // This helper is intentionally automation-only. Direct Draw finishes by
            // selecting its generated host solid, while QS3DCURTAIN3D deliberately
            // requires the canonical LINE/POLYLINE source. Re-select that source
            // without exposing its Handle in the qualification marker.
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain-panel prepare is automation-only.");
            RequiredResultPath(requestedPath);

            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Curtain-panel prepare requires an existing QS3D project.");
            var candidates = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).ToList();
            if (candidates.Count != 1)
                throw new InvalidOperationException("Curtain-panel prepare requires exactly one GlassWall.");
            var sourceHandles = CanonicalHandles(candidates[0].SourceHandles, "source");
            if (sourceHandles.Count != 1)
                throw new InvalidOperationException("Curtain-panel prepare requires exactly one canonical source.");
            var sourceIds = CadHandleService.Resolve(document, sourceHandles);
            if (sourceIds.Count != 1)
                throw new InvalidOperationException("Curtain-panel prepare could not resolve the canonical source.");
            document.Editor.SetImpliedSelection(sourceIds.ToArray());
        }

        [CommandMethod("QS3DCURTAINPANELPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain-panel runtime probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Curtain-panel runtime nonce is invalid.");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Curtain-panel runtime result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Curtain-panel runtime probe requires an existing QS3D project.");

                var candidates = project.Elements
                    .Where(x => x.Category == ElementCategory.GlassWall &&
                                x.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state) &&
                                string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (candidates.Count != 1)
                    throw new InvalidOperationException("Curtain-panel runtime probe requires exactly one completed GlassWall panel owner.");
                var element = candidates[0];

                var sourceHandles = CanonicalHandles(element.SourceHandles, "source");
                var hostHandles = CanonicalHandles(PropertyValues(element, "GeneratedSolidHandle"), "host");
                var frameHandles = CanonicalHandles(PropertyValues(element, "GeneratedCurtainFrameHandles"), "frame");
                var panelHandles = CanonicalHandles(PropertyValues(element, "GeneratedCurtainPanelHandles"), "panel");
                if (sourceHandles.Count != 1 || hostHandles.Count != 1 || frameHandles.Count == 0 || panelHandles.Count == 0)
                    throw new InvalidOperationException("Curtain-panel runtime output is incomplete.");
                RequireDisjoint(sourceHandles, hostHandles, frameHandles, panelHandles);

                var expectedPanelCount = RequiredNonNegativeInteger(element, "GeneratedCurtainPanelCount");
                if (expectedPanelCount != panelHandles.Count)
                    throw new InvalidOperationException("GeneratedCurtainPanelCount does not match panel ownership.");

                var liveHosts = CadHandleService.GetLiveSolidHandles(document, hostHandles);
                var liveFrames = CadHandleService.GetLiveSolidHandles(document, frameHandles);
                var livePanels = CadHandleService.GetLiveSolidHandles(document, panelHandles);
                if (liveHosts.Count != hostHandles.Count || liveFrames.Count != frameHandles.Count || livePanels.Count != panelHandles.Count)
                    throw new InvalidOperationException("One or more curtain host/frame/panel outputs are not live Solid3d objects.");

                var coreIssues = new GeneratedCurtainPanelHealthService().Inspect(project, livePanels);
                var liveIssues = CurtainWallPanelLiveStateService.Inspect(document, project);
                var ownershipIssues = GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project);
                var blockingIssues = coreIssues.Concat(liveIssues).Concat(ownershipIssues)
                    .Count(x => x.Severity != HealthSeverity.Info);
                if (blockingIssues != 0)
                    throw new InvalidOperationException("Curtain-panel runtime health is not clean.");

                var selectedPanelIds = CadHandleService.Resolve(document, new[] { panelHandles[0] });
                if (selectedPanelIds.Count != 1)
                    throw new InvalidOperationException("Cannot resolve a generated curtain panel for Locate proof.");
                document.Editor.SetImpliedSelection(selectedPanelIds.ToArray());
                var owners = SemanticSelectionResolver.ResolveImplied(document, project);
                if (owners.Count != 1 || !ReferenceEquals(owners[0], element))
                    throw new InvalidOperationException("Generated curtain panel did not resolve to its canonical GlassWall owner.");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DCURTAINPANELPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_CURTAIN_PANEL_RUNTIME_V1",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "glass_wall_count=1",
                    "source_count=" + sourceHandles.Count.ToString(CultureInfo.InvariantCulture),
                    "host_solid_count=" + liveHosts.Count.ToString(CultureInfo.InvariantCulture),
                    "frame_solid_count=" + liveFrames.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_solid_count=" + livePanels.Count.ToString(CultureInfo.InvariantCulture),
                    "panel_metadata_count=" + expectedPanelCount.ToString(CultureInfo.InvariantCulture),
                    "health_issue_count=0",
                    "located_panel_count=1",
                    "canonical_owner_count=1",
                    "ownership_sets_disjoint=true",
                    "panel_build_state_complete=true"
                });
                document.Editor.WriteMessage("\nQS3D curtain-panel runtime probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(requestedPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curtain-panel runtime probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static IReadOnlyList<string> PropertyValues(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> values, string label)
        {
            var result = values
                .Select(x => CadHandleService.NormalizeHexHandle(x)
                    ?? throw new InvalidDataException("Curtain-panel runtime " + label + " ownership contains an invalid handle."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return result.AsReadOnly();
        }

        private static void RequireDisjoint(params IReadOnlyList<string>[] groups)
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
                foreach (var handle in group)
                    if (!all.Add(handle)) throw new InvalidOperationException("Curtain source/host/frame/panel ownership sets overlap.");
        }

        private static int RequiredNonNegativeInteger(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0)
                throw new InvalidDataException(key + " is missing or invalid.");
            return value;
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curtain-panel runtime result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain-panel runtime result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DCURTAINPANELPROBE",
                        "error_code=CURTAIN_PANEL_RUNTIME_FAILED"
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Curtain-panel runtime result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
