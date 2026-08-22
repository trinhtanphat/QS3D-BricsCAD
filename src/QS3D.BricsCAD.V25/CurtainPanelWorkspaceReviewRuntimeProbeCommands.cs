using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P10 probe. It drives the production selection,
    /// Workspace, Health All and Release Check commands from a disposable drawing.
    /// The result contains classifications/counts only; project IDs, semantic IDs,
    /// Handles, names, paths and exception details are never published.
    /// </summary>
    public sealed class CurtainPanelWorkspaceReviewRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_P10_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_P10_NONCE";
        private const string ProgressVariable = "QS3D_CURTAIN_P10_PROGRESS";
        private const string ResultFileName = "curtain-panel-workspace-review-result.txt";
        private const string ProgressFileName = "curtain-panel-workspace-review-progress.txt";
        private const string Schema = "QS3D_CURTAIN_PANEL_WORKSPACE_REVIEW_RUNTIME_V1";
        private static readonly object Gate = new object();
        private static ProbeState? _state;

        [CommandMethod("QS3DCURTAINP10PROGRESSLOAD", CommandFlags.Modal)]
        public void ProgressPluginLoaded() => WriteProgress("plugin_loaded");

        [CommandMethod("QS3DCURTAINP10PROGRESSDRAW", CommandFlags.Modal)]
        public void ProgressDirectDrawComplete() => WriteProgress("direct_draw_complete");

        [CommandMethod("QS3DCURTAINP10PROGRESSPREPARE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ProgressSelectionPrepared()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("Curtain P10 selection progress requires an active document.");
            var preparedSelection = document.Editor.SelectImplied();
            if (preparedSelection.Status != Bricscad.EditorInput.PromptStatus.OK || preparedSelection.Value == null)
                throw new InvalidOperationException("Curtain P10 selection progress requires the prepared implied selection.");
            var preparedIds = preparedSelection.Value.GetObjectIds();
            if (preparedIds.Length == 0)
                throw new InvalidOperationException("Curtain P10 selection progress requires a non-empty prepared selection.");

            WriteProgress("source_selection_prepared");
            document.Editor.SetImpliedSelection(preparedIds);
        }

        [CommandMethod("QS3DCURTAINP10PROGRESSBUILD", CommandFlags.Modal)]
        public void ProgressCurtainBuilt() => WriteProgress("curtain_build_complete");

        [CommandMethod("QS3DCURTAINP10PROGRESSSELECT", CommandFlags.Modal)]
        public void ProgressPanelSelected() => WriteProgress("panel_selected");

        [CommandMethod("QS3DCURTAINP10PROGRESSWORKSPACE", CommandFlags.Modal)]
        public void ProgressWorkspaceOpened() => WriteProgress("workspace_opened");

        [CommandMethod("QS3DCURTAINP10PROGRESSINSPECT", CommandFlags.Modal)]
        public void ProgressWorkspaceInspected() => WriteProgress("workspace_inspected");

        [CommandMethod("QS3DCURTAINP10PROGRESSREVIEW", CommandFlags.Modal)]
        public void ProgressWorkspaceVerified() => WriteProgress("workspace_verified");

        [CommandMethod("QS3DCURTAINP10PROGRESSHEALTH", CommandFlags.Modal)]
        public void ProgressHealthOpened() => WriteProgress("health_all_opened");

        [CommandMethod("QS3DCURTAINP10PROGRESSHEALTHCHECK", CommandFlags.Modal)]
        public void ProgressHealthVerified() => WriteProgress("health_verified");

        [CommandMethod("QS3DCURTAINP10PROGRESSRELEASE", CommandFlags.Modal)]
        public void ProgressReleaseOpened() => WriteProgress("release_check_opened");

        [CommandMethod("QS3DCURTAINP10SELECT", CommandFlags.Modal)]
        public void SelectGeneratedPanel() => RunPhase("select_panel", "PANEL_SELECTION_REJECTED", () =>
        {
            var context = RequiredContext();
            var document = context.Document;
            var project = context.Project;
            var owners = project.Elements
                .Where(element => element.Category == ElementCategory.GlassWall &&
                    element.Properties.TryGetValue(GeneratedCurtainPanelHealthService.BuildStateKey, out var state) &&
                    string.Equals((state ?? string.Empty).Trim(), GeneratedCurtainPanelHealthService.BuildCompleteValue, StringComparison.Ordinal))
                .ToList();
            if (owners.Count != 1)
                throw new InvalidOperationException("P10 requires exactly one completed GlassWall owner.");

            var owner = owners[0];
            var family = project.FindFamily(owner.FamilyId)
                ?? throw new InvalidOperationException("P10 owner Family is unavailable.");
            if (family.Category != ElementCategory.GlassWall)
                throw new InvalidOperationException("P10 owner Family category is invalid.");

            var panelHandles = CanonicalHandles(PropertyValues(owner, GeneratedCurtainPanelHealthService.HandlesKey));
            var sourceHandles = CanonicalHandles(owner.SourceHandles);
            if (panelHandles.Count == 0 || sourceHandles.Count != 1)
                throw new InvalidOperationException("P10 owner output/source set is incomplete.");
            if (CadHandleService.GetLiveSolidHandles(document, panelHandles).Count != panelHandles.Count ||
                CadHandleService.GetLiveHandles(document, sourceHandles).Count != sourceHandles.Count)
                throw new InvalidOperationException("P10 owner output/source set is not live.");

            var selectedIds = CadHandleService.Resolve(document, new[] { panelHandles[0] });
            if (selectedIds.Count != 1)
                throw new InvalidOperationException("P10 panel selection could not be resolved.");
            document.Editor.SetImpliedSelection(selectedIds.ToArray());

            var state = new ProbeState(document, project, owner, family, panelHandles[0], sourceHandles);
            lock (Gate)
            {
                if (_state != null) throw new InvalidOperationException("P10 state is already active.");
                _state = state;
            }
        });

        [CommandMethod("QS3DCURTAINP10CHECKWORKSPACE", CommandFlags.Modal)]
        public void CheckWorkspaceReview() => RunPhase("workspace_review", "WORKSPACE_OWNER_REVIEW_REJECTED", () =>
        {
            var state = RequiredState();
            state.RequireCurrentAndUnchanged();
            var panel = WorkspacePanelFromProductionPalette();
            if (!PaletteCoordinator.IsWorkspaceVisible ||
                !panel.MatchesCurtainP10Review(state.Project, state.Owner, state.Family, state.PanelHandle))
                throw new InvalidOperationException("P10 Workspace did not present the canonical owner/Family review.");
            state.WorkspaceReviewVerified = true;
        });

        [CommandMethod("QS3DCURTAINP10CHECKHEALTH", CommandFlags.Modal)]
        public void CheckHealthAllReview() => RunPhase("health_all", "HEALTH_ALL_REVIEW_REJECTED", () =>
        {
            var state = RequiredState();
            state.RequireCurrentAndUnchanged();
            if (!state.WorkspaceReviewVerified)
                throw new InvalidOperationException("P10 Workspace review was not verified before Health All.");
            if (!WorkspacePanelFromProductionPalette().HasCurtainP10HealthAllReadyStatus())
                throw new InvalidOperationException("P10 Health All did not report zero Error/Warning.");
            state.HealthAllVerified = true;
        });

        [CommandMethod("QS3DCURTAINP10COMPLETE", CommandFlags.Modal)]
        public void Complete() => RunPhase("release_check", "RELEASE_CHECK_REVIEW_REJECTED", () =>
        {
            var context = RequiredContext();
            var state = RequiredState();
            state.RequireCurrentAndUnchanged();
            if (!state.WorkspaceReviewVerified || !state.HealthAllVerified)
                throw new InvalidOperationException("P10 prerequisite review phases are incomplete.");
            if (!WorkspacePanelFromProductionPalette().HasCurtainP10ReleaseReadyStatus())
                throw new InvalidOperationException("P10 Release Check did not report READY.");

            if (CadHandleService.GetLiveSolidHandles(state.Document, new[] { state.PanelHandle }).Count != 1 ||
                CadHandleService.GetLiveHandles(state.Document, state.SourceHandles).Count != state.SourceHandles.Count)
                throw new InvalidOperationException("P10 source/panel geometry changed during review.");
            var selectedOwners = SemanticSelectionResolver.ResolveImplied(state.Document, state.Project);
            if (selectedOwners.Count != 1 || !ReferenceEquals(selectedOwners[0], state.Owner))
                throw new InvalidOperationException("P10 selected panel no longer resolves to the canonical owner.");

            var livePanels = CadHandleService.GetLiveSolidHandles(
                state.Document,
                CanonicalHandles(PropertyValues(state.Owner, GeneratedCurtainPanelHealthService.HandlesKey)));
            var blockingIssues = new GeneratedCurtainPanelHealthService().Inspect(state.Project, livePanels)
                .Concat(CurtainWallPanelLiveStateService.Inspect(state.Document, state.Project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(state.Document, state.Project))
                .Count(issue => issue.Severity != HealthSeverity.Info);
            if (blockingIssues != 0)
                throw new InvalidOperationException("P10 final Curtain panel health is not clean.");

            WriteMarkerAtomic(context.ResultPath, new[]
            {
                "status=PASS",
                "command=QS3DCURTAINP10COMPLETE",
                "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + context.Nonce,
                "schema=" + Schema,
                "qualification_boundary=LOCAL_002_P10_ONLY",
                "production_local002_qualified=false",
                "p10_qualified=true",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                "selected_panel_count=1",
                "canonical_owner_count=1",
                "owner_category_glasswall=true",
                "family_review_match=true",
                "instance_scope_active=true",
                "health_all_ready=true",
                "release_check_ready=true",
                "project_unchanged=true",
                "source_preserved=true",
                "panel_live=true",
                "health_issue_count=0"
            });
            lock (Gate) _state = null;
        });

        private static void RunPhase(string phase, string failureCode, Action action)
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            try
            {
                action();
            }
            catch (System.Exception)
            {
                TryWriteFailure(requestedPath, phase, failureCode);
                lock (Gate) _state = null;
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Curtain P10 runtime probe FAIL. See the sanitized local qualification result.");
                throw;
            }
        }

        private static ProbeContext RequiredContext()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain P10 probe is automation-only.");
            var resultPath = RequiredResultPath(requestedPath);
            if (File.Exists(resultPath)) throw new IOException("Curtain P10 result already exists.");
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Curtain P10 requires an existing QS3D project.");
            return new ProbeContext(resultPath, nonce, document, project);
        }

        private static ProbeState RequiredState()
        {
            lock (Gate)
                return _state ?? throw new InvalidOperationException("Curtain P10 state is unavailable.");
        }

        private static WorkspacePanel WorkspacePanelFromProductionPalette()
        {
            var field = typeof(PaletteCoordinator).GetField("_workspacePanel", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingFieldException(typeof(PaletteCoordinator).FullName, "_workspacePanel");
            return field.GetValue(null) as WorkspacePanel
                ?? throw new InvalidOperationException("Production Workspace palette is unavailable.");
        }

        private static IReadOnlyList<string> PropertyValues(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> values) => values
            .Select(value => CadHandleService.NormalizeHexHandle(value)
                ?? throw new InvalidDataException("Curtain P10 ownership contains an invalid Handle."))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curtain P10 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain P10 result directory must already exist.");
            return fullPath;
        }

        private static string RequiredProgressPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ProgressFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curtain P10 progress filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curtain P10 progress directory must already exist.");
            return fullPath;
        }

        private static void WriteProgress(string phase)
        {
            var requestedPath = Environment.GetEnvironmentVariable(ProgressVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain P10 progress probe is automation-only.");
            var fullPath = RequiredProgressPath(requestedPath);
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine("phase=" + phase);
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(fullPath)) File.Replace(tempPath, fullPath, null);
                else File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch { }
            }
        }

        private static void TryWriteFailure(string? requestedPath, string phase, string failureCode)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (normalized.Length == 0 || File.Exists(normalized) || !Guid.TryParseExact(nonce, "N", out _)) return;
                WriteMarkerAtomic(normalized, new[]
                {
                    "status=FAIL",
                    "command=QS3DCURTAINP10COMPLETE",
                    "nonce=" + nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_002_P10_ONLY",
                    "production_local002_qualified=false",
                    "p10_qualified=false",
                    "error_code=CURTAIN_PANEL_WORKSPACE_REVIEW_RUNTIME_FAILED",
                    "failure_phase=" + phase,
                    "failure_code=" + failureCode
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Curtain P10 result already exists.");
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

        private sealed class ProbeContext
        {
            public ProbeContext(string resultPath, string nonce, Document document, ProjectState project)
            {
                ResultPath = resultPath;
                Nonce = nonce;
                Document = document;
                Project = project;
            }

            public string ResultPath { get; }
            public string Nonce { get; }
            public Document Document { get; }
            public ProjectState Project { get; }
        }

        private sealed class ProbeState
        {
            private readonly string _projectId;
            private readonly long _changeVersion;
            private readonly long _updatedUtcTicks;

            public ProbeState(
                Document document,
                ProjectState project,
                ProjectElement owner,
                ProjectFamily family,
                string panelHandle,
                IReadOnlyList<string> sourceHandles)
            {
                Document = document;
                Project = project;
                Owner = owner;
                Family = family;
                PanelHandle = panelHandle;
                SourceHandles = sourceHandles;
                _projectId = project.ProjectId;
                _changeVersion = project.ChangeVersion;
                _updatedUtcTicks = project.UpdatedUtc.Ticks;
            }

            public Document Document { get; }
            public ProjectState Project { get; }
            public ProjectElement Owner { get; }
            public ProjectFamily Family { get; }
            public string PanelHandle { get; }
            public IReadOnlyList<string> SourceHandles { get; }
            public bool WorkspaceReviewVerified { get; set; }
            public bool HealthAllVerified { get; set; }

            public void RequireCurrentAndUnchanged()
            {
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, Document) ||
                    !ProjectContextCoordinator.TryGetReadOnly(Document, out var current) ||
                    !ReferenceEquals(current, Project) ||
                    !string.Equals(current.ProjectId, _projectId, StringComparison.Ordinal) ||
                    current.ChangeVersion != _changeVersion ||
                    current.UpdatedUtc.Ticks != _updatedUtcTicks ||
                    !ReferenceEquals(current.FindElement(Owner.Id), Owner) ||
                    !ReferenceEquals(current.FindFamily(Family.Id), Family))
                    throw new InvalidOperationException("Curtain P10 document/project review state changed.");
            }
        }
    }
}

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        internal bool MatchesCurtainP10Review(
            ProjectState project,
            ProjectElement owner,
            ProjectFamily family,
            string panelHandle)
        {
            if (project == null || owner == null || family == null || string.IsNullOrWhiteSpace(panelHandle)) return false;
            if (_inspection.Count != 1 ||
                !string.Equals(
                    CadHandleService.NormalizeHexHandle(_inspection[0]?.Handle),
                    CadHandleService.NormalizeHexHandle(panelHandle),
                    StringComparison.OrdinalIgnoreCase)) return false;
            if (!(FamilyList.SelectedItem is ProjectFamily selectedFamily) || !ReferenceEquals(selectedFamily, family)) return false;
            if (!string.Equals(_viewModel.SelectedFamilyName, family.Name, StringComparison.Ordinal) ||
                !string.Equals(_viewModel.SelectedPropertyScope, WorkspaceViewModel.InstanceScope, StringComparison.Ordinal) ||
                !_viewModel.Status.StartsWith("Instance:", StringComparison.Ordinal)) return false;
            var elementRows = _viewModel.Properties
                .Where(row => string.Equals(row.Name, "Element ID", StringComparison.Ordinal))
                .ToList();
            return elementRows.Count == 1 && string.Equals(elementRows[0].Value, owner.Id, StringComparison.Ordinal);
        }

        internal bool HasCurtainP10HealthAllReadyStatus()
        {
            var status = _viewModel.Status ?? string.Empty;
            if (!status.StartsWith("Health All:", StringComparison.Ordinal)) return false;
            var numbers = Regex.Matches(status, "[0-9]+")
                .Cast<Match>()
                .Select(match => int.Parse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture))
                .ToArray();
            return numbers.Length >= 2 && numbers[0] == 0 && numbers[1] == 0;
        }

        internal bool HasCurtainP10ReleaseReadyStatus() =>
            (_viewModel.Status ?? string.Empty).StartsWith("Release Check: READY", StringComparison.Ordinal);
    }
}
