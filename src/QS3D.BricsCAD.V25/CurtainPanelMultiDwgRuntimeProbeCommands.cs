using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-002/P12 probe. It exercises the real modeless Curtain Hub,
    /// WPF routed buttons and BricsCAD multi-document/destroy lifecycle. It never replaces
    /// production window, project-affinity or document-lifetime logic.
    /// </summary>
    public sealed class CurtainPanelMultiDwgRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_P12_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_P12_NONCE";
        private const string DrawingAVariable = "QS3D_CURTAIN_P12_DWG_A";
        private const string DrawingBVariable = "QS3D_CURTAIN_P12_DWG_B";
        private const string ResultFileName = "curtain-panel-multidwg-result.txt";
        private const string DrawingSuffix = ".curtain-multidwg-probe-copy.dwg";
        private const string Schema = "QS3D_CURTAIN_PANEL_MULTIDWG_RUNTIME_V1";
        private const string RoleKey = "QS3D.CurtainP12Role";
        private const string FamilyIdPrefix = "curtain-p12-family-";
        private static ProbeState? _state;

        [CommandMethod("QS3DCURTAINP12SEEDA", CommandFlags.Modal)]
        public void SeedA() => Run("SEED_A", context =>
        {
            var document = RequireActive(context.DrawingA);
            _state = new ProbeState(context.Nonce, context.DrawingA, context.DrawingB);
            _state.SeedA = SeedProject(document, "A");
        });

        [CommandMethod("QS3DCURTAINP12CAPTURE", CommandFlags.Modal)]
        public void CaptureWindow() => Run("WINDOW_CAPTURE", context =>
        {
            RequireActive(context.DrawingA);
            var state = RequireState(context);
            var windows = CurtainWindows();
            if (windows.Count != 1) throw Fail("WINDOW_COUNT_REJECTED");
            var window = windows[0];
            if (!window.IsLoaded || !window.IsVisible) throw Fail("WINDOW_NOT_VISIBLE");
            if (window.Title.IndexOf(Path.GetFileName(context.DrawingA), StringComparison.OrdinalIgnoreCase) < 0)
                throw Fail("WINDOW_BINDING_REJECTED");
            state.Window = window;
            window.Closed += (_, __) => state.WindowClosedObserved = true;
            state.WindowCaptured = true;
        });

        [CommandMethod("QS3DCURTAINP12SEEDB", CommandFlags.Modal)]
        public void SeedB() => Run("SEED_B", context =>
        {
            var document = RequireActive(context.DrawingB);
            var state = RequireState(context);
            state.SeedB = SeedProject(document, "B");
            state.SeedA.Ensure(FindDocument(context.DrawingA), "A");
            state.SeedB.Ensure(document, "B");
            if (ReferenceEquals(state.SeedA.Project, state.SeedB.Project) ||
                string.Equals(state.SeedA.ProjectId, state.SeedB.ProjectId, StringComparison.OrdinalIgnoreCase))
                throw Fail("PROJECT_ISOLATION_REJECTED");
            state.TwoDocumentsObserved = BcadApplication.DocumentManager.Count >= 2;
            if (!state.TwoDocumentsObserved) throw Fail("DOCUMENT_COUNT_REJECTED");
        });

        [CommandMethod("QS3DCURTAINP12CHECKB", CommandFlags.Modal)]
        public void CheckWhileBIsActive() => Run("B_AFFINITY", context =>
        {
            var documentB = RequireActive(context.DrawingB);
            var state = RequireState(context);
            var window = RequireWindow(state);
            var documentA = FindDocument(context.DrawingA);
            state.SeedA.Ensure(documentA, "A");
            state.SeedB.Ensure(documentB, "B");

            InvokeButton(window, button =>
                button.Tag == null &&
                button.Content is string text &&
                text.IndexOf("Làm mới", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, documentB))
                throw Fail("REFRESH_SWITCHED_DOCUMENT");
            if (!ContainsActiveDocumentRefusal(window.StatusText.Text))
                throw Fail("REFRESH_DID_NOT_REFUSE");
            state.RefreshRefusedOnB = true;

            var healthWindowsBefore = VisibleHealthWindowCount();
            InvokeButton(window, button => string.Equals(button.Tag as string, "QS3DCURTAINFRAMEHEALTH", StringComparison.Ordinal));
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, documentB))
                throw Fail("COMMAND_SWITCHED_DOCUMENT");
            if (!ContainsActiveDocumentRefusal(window.StatusText.Text))
                throw Fail("COMMAND_DID_NOT_REFUSE");
            if (VisibleHealthWindowCount() != healthWindowsBefore)
                throw Fail("COMMAND_DISPATCHED_TO_WRONG_DOCUMENT");
            state.CommandRefusedOnB = true;

            state.SeedA.Ensure(documentA, "A");
            state.SeedB.Ensure(documentB, "B");
            state.ProjectsUnchangedOnB = true;
            if (!window.IsLoaded || !window.IsVisible) throw Fail("WINDOW_CLOSED_DURING_B_CHECK");
            state.WindowRemainedBoundToA = true;
        });

        [CommandMethod("QS3DCURTAINP12ACTIVATEA", CommandFlags.Modal)]
        public void ActivateA() => Run("ACTIVATE_A", context =>
        {
            RequireActive(context.DrawingB);
            var documentA = FindDocument(context.DrawingA);
            BcadApplication.DocumentManager.MdiActiveDocument = documentA;
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, documentA))
                throw Fail("DOCUMENT_ACTIVATION_REJECTED");
            RequireState(context).ActivatedA = true;
        });

        [CommandMethod("QS3DCURTAINP12CHECKA", CommandFlags.Modal)]
        public void CheckAfterAIsReactivated() => Run("A_REFRESH", context =>
        {
            var documentA = RequireActive(context.DrawingA);
            var state = RequireState(context);
            var window = RequireWindow(state);
            state.SeedA.Ensure(documentA, "A");

            InvokeButton(window, button =>
                button.Tag == null &&
                button.Content is string text &&
                text.IndexOf("Làm mới", StringComparison.OrdinalIgnoreCase) >= 0);
            if (ContainsActiveDocumentRefusal(window.StatusText.Text) ||
                window.StatusText.Text.IndexOf("Đã nạp", StringComparison.OrdinalIgnoreCase) < 0)
                throw Fail("REACTIVATED_REFRESH_REJECTED");
            if (window.FamilyCombo.Items.Count < 1) throw Fail("REACTIVATED_PROJECT_NOT_RENDERED");
            state.SeedA.Ensure(documentA, "A");
            state.SeedB.Ensure(FindDocument(context.DrawingB), "B");
            state.ReactivatedARefreshSucceeded = true;
        });

        [CommandMethod("QS3DCURTAINP12ACTIVATEB", CommandFlags.Modal)]
        public void ActivateB() => Run("ACTIVATE_B", context =>
        {
            RequireActive(context.DrawingA);
            var documentB = FindDocument(context.DrawingB);
            BcadApplication.DocumentManager.MdiActiveDocument = documentB;
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, documentB))
                throw Fail("DOCUMENT_ACTIVATION_REJECTED");
            RequireState(context).ActivatedB = true;
        });

        [CommandMethod("QS3DCURTAINP12CLOSEA", CommandFlags.Modal)]
        public void CloseA() => Run("CLOSE_A", context =>
        {
            var documentB = RequireActive(context.DrawingB);
            var state = RequireState(context);
            var window = RequireWindow(state);
            state.SeedB.Ensure(documentB, "B");
            var documentA = FindDocument(context.DrawingA);
            documentA.CloseAndDiscard();

            if (TryFindDocument(context.DrawingA, out _)) throw Fail("DOCUMENT_A_REMAINED_OPEN");
            if (window.IsVisible || CurtainWindows().Any(candidate => ReferenceEquals(candidate, window)))
                throw Fail("BOUND_WINDOW_REMAINED_OPEN");
            if (!state.WindowClosedObserved) throw Fail("WINDOW_CLOSED_EVENT_MISSING");
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, documentB))
                throw Fail("DOCUMENT_B_LOST_ACTIVE_STATE");
            state.SeedB.Ensure(documentB, "B");
            state.WindowClosedWithA = true;
            state.BRemainedActive = true;
            state.BUnchangedAfterAClose = true;
        });

        [CommandMethod("QS3DCURTAINP12FINAL", CommandFlags.Modal)]
        public void Complete() => Run("FINAL", context =>
        {
            var documentB = RequireActive(context.DrawingB);
            var state = RequireState(context);
            state.SeedB.Ensure(documentB, "B");
            if (!state.IsComplete) throw Fail("INCOMPLETE_STATE");
            WriteMarkerAtomic(context.ResultPath, new[]
            {
                "status=PASS",
                "command=QS3DCURTAINP12FINAL",
                "nonce=" + context.Nonce,
                "schema=" + Schema,
                "qualification_boundary=LOCAL_002_P12_ONLY",
                "production_local002_qualified=false",
                "p12_qualified=true",
                "two_documents_observed=true",
                "curtain_window_bound_to_a=true",
                "b_refresh_refused=true",
                "b_command_refused=true",
                "projects_unchanged_while_b_active=true",
                "reactivated_a_refresh_succeeded=true",
                "a_close_closed_bound_window=true",
                "window_closed_event_observed=true",
                "b_remained_active=true",
                "b_project_unchanged_after_a_close=true",
                "document_count_after_close=" + BcadApplication.DocumentManager.Count.ToString(CultureInfo.InvariantCulture)
            });
            documentB.Editor.WriteMessage("\nQS3D Curtain P12 multi-DWG/modeless probe PASS.");
        });

        private static void Run(string phase, Action<ProbeContext> action)
        {
            var requestedResult = (Environment.GetEnvironmentVariable(ResultVariable) ?? string.Empty).Trim();
            if (requestedResult.Length == 0)
            {
                BcadApplication.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Curtain P12 probe skipped: automation environment is not configured.");
                return;
            }
            try
            {
                if (File.Exists(Path.GetFullPath(requestedResult))) return;
                action(ProbeContext.Read());
            }
            catch (ProbeFailure failure)
            {
                TryWriteFailure(phase, failure.Code);
            }
            catch
            {
                TryWriteFailure(phase, "UNEXPECTED_FAILURE");
            }
        }

        private static ProjectStamp SeedProject(Document document, string role)
        {
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var familyId = FamilyIdPrefix + role.ToLowerInvariant();
            var family = project.FindFamily(familyId);
            if (family == null)
                family = ProjectFamilyService.Create(project, familyId, "Curtain P12 " + role, ElementCategory.GlassWall);
            if (family.Category != ElementCategory.GlassWall) throw Fail("FAMILY_CATEGORY_REJECTED");
            ProjectFamilyService.SetProperty(project, family.Id, "ThicknessM", "0.012");
            ProjectFamilyService.SetProperty(project, family.Id, "HeightM", "3.6");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainMaxPanelWidthM", "1.2");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainMaxPanelHeightM", "1.5");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainPerimeterFrameWidthM", "0.05");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainMullionWidthM", "0.05");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainTransomWidthM", "0.05");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainFrameDepthM", "0.05");
            ProjectFamilyService.SetProperty(project, family.Id, "Material", "Glass");
            ProjectFamilyService.SetProperty(project, family.Id, "CurtainFrameMaterial", "Aluminium");
            project.Metadata[RoleKey] = role;
            project.Touch();
            ProjectContextCoordinator.Save(document);
            if (ProjectContextCoordinator.HasPendingChanges(document)) throw Fail("PROJECT_SAVE_REJECTED");
            return ProjectStamp.Capture(document, role);
        }

        private static ProbeState RequireState(ProbeContext context)
        {
            var state = _state;
            if (state == null ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal) ||
                !SamePath(state.DrawingA, context.DrawingA) ||
                !SamePath(state.DrawingB, context.DrawingB))
                throw Fail("PROBE_STATE_REJECTED");
            return state;
        }

        private static CurtainWallWindow RequireWindow(ProbeState state)
        {
            if (!state.WindowCaptured || state.Window == null || !state.Window.IsLoaded || !state.Window.IsVisible)
                throw Fail("WINDOW_NOT_AVAILABLE");
            return state.Window;
        }

        private static void InvokeButton(CurtainWallWindow window, Func<Button, bool> predicate)
        {
            window.Dispatcher.Invoke(new Action(() =>
            {
                window.UpdateLayout();
                var matches = Descendants<Button>(window).Where(predicate).ToList();
                if (matches.Count != 1) throw Fail("BUTTON_LOOKUP_REJECTED");
                matches[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent, matches[0]));
            }));
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < count; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match) yield return match;
                foreach (var descendant in Descendants<T>(child)) yield return descendant;
            }
        }

        private static List<CurtainWallWindow> CurtainWindows()
        {
            return HostedWindows<CurtainWallWindow>()
                .Where(window => window.IsLoaded || window.IsVisible)
                .ToList();
        }

        private static int VisibleHealthWindowCount()
        {
            return HostedWindows<ModelHealthWindow>().Count(window => window.IsVisible);
        }

        private static IEnumerable<TWindow> HostedWindows<TWindow>() where TWindow : Window
        {
            return PresentationSource.CurrentSources
                .OfType<HwndSource>()
                .Select(source => source.RootVisual)
                .OfType<TWindow>()
                .Distinct();
        }

        private static bool ContainsActiveDocumentRefusal(string? value)
        {
            var text = value ?? string.Empty;
            return text.IndexOf("kích hoạt lại đúng bản vẽ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Document RequireActive(string path)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || !SamePath(document.Name, path)) throw Fail("ACTIVE_DOCUMENT_REJECTED");
            return document;
        }

        private static Document FindDocument(string path)
        {
            if (TryFindDocument(path, out var document)) return document;
            throw Fail("DOCUMENT_NOT_FOUND");
        }

        private static bool TryFindDocument(string path, out Document document)
        {
            foreach (Document candidate in BcadApplication.DocumentManager)
            {
                if (!SamePath(candidate.Name, path)) continue;
                document = candidate;
                return true;
            }
            document = null!;
            return false;
        }

        private static bool SamePath(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static ProbeFailure Fail(string code) => new ProbeFailure(code);

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var context = ProbeContext.Read();
                if (File.Exists(context.ResultPath)) return;
                WriteMarkerAtomic(context.ResultPath, new[]
                {
                    "status=FAIL",
                    "command=QS3DCURTAINP12FINAL",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_002_P12_ONLY",
                    "production_local002_qualified=false",
                    "p12_qualified=false",
                    "error_code=CURTAIN_PANEL_MULTIDWG_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Curtain P12 marker already exists.");
            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, path);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string? value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private sealed class ProbeContext
        {
            private ProbeContext(string resultPath, string nonce, string drawingA, string drawingB)
            {
                ResultPath = resultPath;
                Nonce = nonce;
                DrawingA = drawingA;
                DrawingB = drawingB;
            }

            public string ResultPath { get; }
            public string Nonce { get; }
            public string DrawingA { get; }
            public string DrawingB { get; }

            public static ProbeContext Read()
            {
                var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
                if (!Guid.TryParseExact(nonce, "N", out _)) throw Fail("NONCE_REJECTED");
                var result = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable));
                var drawingA = RequiredDrawingPath(Environment.GetEnvironmentVariable(DrawingAVariable));
                var drawingB = RequiredDrawingPath(Environment.GetEnvironmentVariable(DrawingBVariable));
                if (SamePath(drawingA, drawingB)) throw Fail("DRAWING_IDENTITY_REJECTED");
                var artifactRoot = Path.GetDirectoryName(result) ?? string.Empty;
                var fixtureRoot = Path.Combine(artifactRoot, "fixture-copies");
                if (!SamePath(Path.GetDirectoryName(drawingA), fixtureRoot) || !SamePath(Path.GetDirectoryName(drawingB), fixtureRoot))
                    throw Fail("ARTIFACT_SCOPE_REJECTED");
                return new ProbeContext(result, nonce, drawingA, drawingB);
            }

            private static string RequiredResultPath(string? value)
            {
                var path = Path.GetFullPath((value ?? string.Empty).Trim());
                var directory = Path.GetDirectoryName(path);
                if (!string.Equals(Path.GetFileName(path), ResultFileName, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                    throw Fail("RESULT_PATH_REJECTED");
                return path;
            }

            private static string RequiredDrawingPath(string? value)
            {
                var path = Path.GetFullPath((value ?? string.Empty).Trim());
                if (!path.EndsWith(DrawingSuffix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                    throw Fail("DRAWING_PATH_REJECTED");
                return path;
            }
        }

        private sealed class ProjectStamp
        {
            private ProjectStamp(ProjectState project, string role)
            {
                Project = project;
                ProjectId = project.ProjectId;
                ChangeVersion = project.ChangeVersion;
                UpdatedUtc = project.UpdatedUtc;
                FamilyCount = project.Families.Count;
                ElementCount = project.Elements.Count;
                AuditCount = project.AuditEvents.Count;
                MetadataCount = project.Metadata.Count;
                Role = role;
            }

            public ProjectState Project { get; }
            public string ProjectId { get; }
            private long ChangeVersion { get; }
            private DateTime UpdatedUtc { get; }
            private int FamilyCount { get; }
            private int ElementCount { get; }
            private int AuditCount { get; }
            private int MetadataCount { get; }
            private string Role { get; }

            public static ProjectStamp Capture(Document document, string role)
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw Fail("PROJECT_NOT_AVAILABLE");
                if (!project.Metadata.TryGetValue(RoleKey, out var actualRole) || !string.Equals(actualRole, role, StringComparison.Ordinal))
                    throw Fail("PROJECT_ROLE_REJECTED");
                return new ProjectStamp(project, role);
            }

            public void Ensure(Document document, string role)
            {
                var current = Capture(document, role);
                if (!ReferenceEquals(Project, current.Project) ||
                    !string.Equals(ProjectId, current.ProjectId, StringComparison.Ordinal) ||
                    ChangeVersion != current.ChangeVersion || UpdatedUtc != current.UpdatedUtc ||
                    FamilyCount != current.FamilyCount || ElementCount != current.ElementCount ||
                    AuditCount != current.AuditCount || MetadataCount != current.MetadataCount ||
                    !string.Equals(Role, current.Role, StringComparison.Ordinal))
                    throw Fail("PROJECT_STATE_CHANGED");
            }
        }

        private sealed class ProbeState
        {
            public ProbeState(string nonce, string drawingA, string drawingB)
            {
                Nonce = nonce;
                DrawingA = drawingA;
                DrawingB = drawingB;
            }

            public string Nonce { get; }
            public string DrawingA { get; }
            public string DrawingB { get; }
            public ProjectStamp SeedA { get; set; } = null!;
            public ProjectStamp SeedB { get; set; } = null!;
            public CurtainWallWindow? Window { get; set; }
            public bool WindowCaptured { get; set; }
            public bool WindowClosedObserved { get; set; }
            public bool TwoDocumentsObserved { get; set; }
            public bool RefreshRefusedOnB { get; set; }
            public bool CommandRefusedOnB { get; set; }
            public bool ProjectsUnchangedOnB { get; set; }
            public bool WindowRemainedBoundToA { get; set; }
            public bool ActivatedA { get; set; }
            public bool ReactivatedARefreshSucceeded { get; set; }
            public bool ActivatedB { get; set; }
            public bool WindowClosedWithA { get; set; }
            public bool BRemainedActive { get; set; }
            public bool BUnchangedAfterAClose { get; set; }

            public bool IsComplete => WindowCaptured && WindowClosedObserved && TwoDocumentsObserved &&
                                      RefreshRefusedOnB && CommandRefusedOnB && ProjectsUnchangedOnB &&
                                      WindowRemainedBoundToA && ActivatedA && ReactivatedARefreshSucceeded &&
                                      ActivatedB && WindowClosedWithA && BRemainedActive && BUnchangedAfterAClose;
        }

        private sealed class ProbeFailure : Exception
        {
            public ProbeFailure(string code) : base(code) { Code = code; }
            public string Code { get; }
        }
    }
}
