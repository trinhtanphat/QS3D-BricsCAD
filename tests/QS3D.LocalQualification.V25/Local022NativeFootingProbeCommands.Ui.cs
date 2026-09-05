using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;
using DrawingPoint = System.Drawing.Point;
using WpfApplication = System.Windows.Application;
using WpfPoint = System.Windows.Point;

#if BRICSCAD_V26
namespace QS3D.LocalQualification.V26
#else
namespace QS3D.LocalQualification.V25
#endif
{
    /// <summary>
    /// Opt-in physical UI extension for LOCAL-022. The probe only observes production WPF state
    /// and publishes bounded physical-input requests. The runner remains the sole input injector.
    /// </summary>
    public sealed partial class Local022NativeFootingProbeCommands
    {
        private const string UiSchema = "QS3D_LOCAL022_NATIVE_UI_V1";
        private const string UiActionSchema = "QS3D_LOCAL022_UI_ACTION_V1";
        private const string UiAckSchema = "QS3D_LOCAL022_UI_ACK_V1";
        private const string UiContinuitySchema = "QS3D_LOCAL022_UI_CONTINUITY_V1";
        private const string SingleFootingCategoryCode = "Foundation.SingleFooting";
        private const string QuickActionsTag = "QS3D_REFERENCE_QUICK_ACTIONS";
        private static readonly TimeSpan UiStageTimeout = TimeSpan.FromSeconds(25);
        private static UiController? _uiController;
        private static UiRunState? _uiRunState;

        [CommandMethod("QL22UI", CommandFlags.Modal)]
        public void Ui()
        {
            Context? context = null;
            try
            {
                context = BindContext("ui");
                RequireMeterDrawing(context.Document);
                RequireMcpMutationBoundaryPaused(context.Product);
                lock (Sync)
                {
                    if (_uiController != null || _uiRunState != null)
                        throw new ProbeException("ui_state_preexists");
                    RequireUiOutputAbsent(context, "ui");
                    RequireUiOutputAbsent(context, "uisaved");
                    _uiController = new UiController(context);
                    _uiController.Start();
                }
            }
            catch (System.Exception error)
            {
                WriteUiFailure(context, "ui", "ui_bind", error);
                QueueOwnedQuit(context, true);
            }
        }

        [CommandMethod("QL22UISAVED", CommandFlags.Modal)]
        public void UiSaved()
        {
            Context? context = null;
            try
            {
                // The runner deliberately keeps QS3D_LOCAL022[_V26]_PHASE=ui through the
                // same-process QS3DSAVE/QSAVE callback chain.
                context = BindContext("ui");
                RequireMeterDrawing(context.Document);
                RequireMcpMutationBoundaryPaused(context.Product);
                UiRunState state;
                lock (Sync)
                    state = _uiRunState ?? throw new ProbeException("ui_saved_state_missing");
                if (!ReferenceEquals(state.Document, context.Document) ||
                    !string.Equals(state.RunId, context.RunId, StringComparison.Ordinal) ||
                    !string.Equals(state.Drawing, context.Drawing, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeException("ui_saved_session_identity");

                var continuity = ReadUiContinuity(context);
                VerifyUiPersistedState(context, continuity, "saved");
                if (!File.Exists(ProjectPath(context.Document)))
                    throw new ProbeException("ui_sidecar_missing");
                if (!File.Exists(context.Drawing))
                    throw new ProbeException("ui_drawing_missing_after_qsave");

                WriteUiMarker(context, "uisaved", "PASS", "uisaved", "NONE", Checks(
                    "active_disposable_drawing", true,
                    "mcp_mutation_boundary_paused", true,
                    "same_process_ui_state", true,
                    "sidecar_exists_after_qs3dsave", true,
                    "qsave_command_completed", true,
                    "saved_semantic_native_state", true,
                    "saved_exact_artifact_digest", true,
                    "saved_exact_cardinality", true));

                WriteUiMarker(context, "ui", "PASS", "ui", "NONE", Checks(
                    "active_disposable_drawing", true,
                    "mcp_mutation_boundary_paused", true,
                    "workspace_visible", true,
                    "single_footing_tree_clicked", true,
                    "cancel_nonmutation", true,
                    "six_field_dialog_layout", true,
                    "six_field_physical_input", true,
                    "active_family_h2_zero", true,
                    "two_physical_centres", true,
                    "enter_command_termination", true,
                    "family_h2_physical_edit", true,
                    "existing_geometry_regenerated", true,
                    "former_generated_handles_erased", true,
                    "repeat_physical_centre", true,
                    "escape_command_termination", true,
                    "geometry_ownership_extents", true,
                    "exact_semantic_native_cardinality", true,
                    "physical_receipts_complete", true,
                    "saved_exact_artifact_digest", true));
                QueueOwnedQuit(context, false);
            }
            catch (System.Exception error)
            {
                WriteUiFailure(context, "uisaved", "uisaved_execute", error);
                WriteUiFailure(context, "ui", "ui_save", error);
                QueueOwnedQuit(context, true);
            }
        }

        [CommandMethod("QL22UIREOPEN", CommandFlags.Modal)]
        public void UiReopen()
        {
            Context? context = null;
            try
            {
                context = BindContext("uireopen");
                RequireMeterDrawing(context.Document);
                RequireMcpMutationBoundaryPaused(context.Product);
                lock (Sync)
                    if (_uiController != null || _uiRunState != null)
                        throw new ProbeException("ui_reopen_not_fresh_process");
                var continuity = ReadUiContinuity(context);
                VerifyUiPersistedState(context, continuity, "reopened");
                WriteUiMarker(context, "uireopen", "PASS", "uireopen", "NONE", Checks(
                    "active_disposable_drawing", true,
                    "mcp_mutation_boundary_paused", true,
                    "cold_project_bind", true,
                    "reopened_family_identity", true,
                    "reopened_semantic_identity", true,
                    "reopened_generated_solids_live", true,
                    "reopened_dimensions_volume_extents", true,
                    "reopened_exact_artifact_digest", true,
                    "reopened_exact_cardinality", true));
                QueueOwnedQuit(context, false);
            }
            catch (System.Exception error)
            {
                WriteUiFailure(context, "uireopen", "uireopen_execute", error);
                QueueOwnedQuit(context, true);
            }
        }

        private sealed class UiController
        {
            private static readonly string[] FieldOrder = { "L1", "W1", "L2", "W2", "H1", "H2" };
            private static readonly string[] FieldText = { "2000", "2000", "1000", "1000", "1000", "0" };
            private readonly Context _context;
            private readonly DispatcherTimer _timer;
            private readonly DateTime _startedUtc;
            private DateTime _deadlineUtc;
            private UiStage _stage;
            private bool _requestWritten;
            private bool _moveRequested;
            private bool _moveAcknowledged;
            private int _sequence;
            private FrameworkElement? _workspace;
            private ProjectState? _project;
            private HashSet<string>? _baselineFamilyIds;
            private HashSet<string>? _baselineElementIds;
            private int _familyBaseline;
            private int _semanticBaseline;
            private int _nativeBaseline;
            private string _cancelProjectDigest = string.Empty;
            private string _cancelNativeDigest = string.Empty;
            private Dictionary<string, TextBox>? _dialogInputs;
            private ProjectFamily? _family;
            private string _familyId = string.Empty;
            private readonly List<Point3d> _centres = new List<Point3d>();
            private readonly List<DrawingPoint> _screenPoints = new List<DrawingPoint>();
            private readonly List<string> _oldGeneratedHandles = new List<string>();
            private readonly Dictionary<string, string> _oldGeneratedByElement =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private HashSet<string>? _firstElementIds;
            private int _stableIdleTicks;
            private bool _treeScrolled;

            public UiController(Context context)
            {
                _context = context;
                _startedUtc = DateTime.UtcNow;
                _deadlineUtc = _startedUtc + UiStageTimeout;
                _stage = UiStage.LocateWorkspace;
                _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(125)
                };
                _timer.Tick += OnTick;
            }

            public void Start() => _timer.Start();

            private void OnTick(object? sender, EventArgs e)
            {
                try
                {
                    if (DateTime.UtcNow > _deadlineUtc)
                        throw new ProbeException("ui_timeout_" + _stage.ToString());
                    RequireUiContextStable(_context);
                    Tick();
                }
                catch (System.Exception error)
                {
                    Fail(error);
                }
            }

            private void Tick()
            {
                switch (_stage)
                {
                    case UiStage.LocateWorkspace:
                        _workspace = RequireProductionWorkspace(_context.Product);
                        RequireClickable(_workspace, "workspace_not_visible");
                        _workspace.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPhysicalMouse), true);
                        _workspace.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnPhysicalButtonClick), true);
                        Advance(UiStage.SelectTree);
                        break;

                    case UiStage.SelectTree:
                    {
                        var item = RequireSingleFootingTree(_workspace!);
                        if (!_treeScrolled)
                        {
                            // Scrolling prepares a reachable control; selection itself must
                            // still result from the acknowledged physical click below.
                            item.BringIntoView();
                            _workspace!.UpdateLayout();
                            _treeScrolled = true;
                            return;
                        }
                        if (!AwaitAction(() => FindVisualDescendants<TextBlock>(item).Single(text =>
                            text.IsVisible && string.Equals(text.Text, "Móng đơn", StringComparison.Ordinal)), "click", string.Empty)) return;
                        if (!item.IsSelected) return;
                        TraceWorkspaceRoute("selected_before_baseline");
                        _project = GetOrCreateProject(_context.Document);
                        TraceWorkspaceRoute("selected_after_baseline");
                        _familyBaseline = _project.Families.Count;
                        _semanticBaseline = _project.Elements.Count;
                        _nativeBaseline = CountModelSpaceEntities(_context.Document);
                        _baselineFamilyIds = new HashSet<string>(_project.Families.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                        _baselineElementIds = new HashSet<string>(_project.Elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                        _cancelProjectDigest = ProjectMutationDigest(_project);
                        _cancelNativeDigest = NativeModelDigest(_context.Document);
                        Advance(UiStage.OpenCancelDialog);
                        break;
                    }

                    case UiStage.OpenCancelDialog:
                    {
                        if (!AwaitAction(() => RequireWorkspaceButton(_workspace!, "+ Add", "+ Thêm"), "click", string.Empty)) return;
                        var dialog = FindSingleFootingDialog(_context.Product);
                        if (dialog == null) return;
                        _dialogInputs = RequireDialogLayout(dialog);
                        Advance(UiStage.CancelDialog);
                        break;
                    }

                    case UiStage.CancelDialog:
                    {
                        var dialog = FindSingleFootingDialog(_context.Product);
                        if (dialog == null && !_requestWritten)
                            throw new ProbeException("ui_cancel_dialog_disappeared");
                        if (!AwaitAction(dialog ?? _workspace!, "key", "ESC")) return;
                        if (FindSingleFootingDialog(_context.Product) != null) return;
                        RequireCancelUnchanged();
                        Advance(UiStage.OpenCreateDialog);
                        break;
                    }

                    case UiStage.OpenCreateDialog:
                    {
                        if (!AwaitAction(() => RequireWorkspaceButton(_workspace!, "+ Add", "+ Thêm"), "click", string.Empty)) return;
                        var dialog = FindSingleFootingDialog(_context.Product);
                        if (dialog == null) return;
                        _dialogInputs = RequireDialogLayout(dialog);
                        Advance(UiStage.InputL1);
                        break;
                    }

                    case UiStage.InputL1: InputField(0, UiStage.InputW1); break;
                    case UiStage.InputW1: InputField(1, UiStage.InputL2); break;
                    case UiStage.InputL2: InputField(2, UiStage.InputW2); break;
                    case UiStage.InputW2: InputField(3, UiStage.InputH1); break;
                    case UiStage.InputH1: InputField(4, UiStage.InputH2); break;
                    case UiStage.InputH2: InputField(5, UiStage.AcceptCreateDialog); break;

                    case UiStage.AcceptCreateDialog:
                    {
                        if (!AwaitAction(() => RequireDialogButton(FindSingleFootingDialog(_context.Product)
                            ?? throw new ProbeException("ui_create_dialog_disappeared"), true), "click", string.Empty)) return;
                        if (FindSingleFootingDialog(_context.Product) != null) return;
                        RequireCreatedFamily();
                        _screenPoints.AddRange(RequireViewportPoints(_context.Document, _workspace!, 3));
                        Advance(UiStage.StartFirstDraw);
                        break;
                    }

                    case UiStage.StartFirstDraw:
                    {
                        if (!AwaitAction(() => RequireDrawButton(_workspace!), "click", string.Empty)) return;
                        if (!IsDrawCommandActive()) return;
                        Advance(UiStage.FirstCentre);
                        break;
                    }

                    case UiStage.FirstCentre:
                        PlaceCentre(0, 1, UiStage.SecondCentre);
                        break;

                    case UiStage.SecondCentre:
                        PlaceCentre(1, 2, UiStage.EndFirstDraw);
                        break;

                    case UiStage.EndFirstDraw:
                        if (!AwaitAction(_workspace!, "key", "ENTER")) return;
                        if (IsDrawCommandActive()) return;
                        if (++_stableIdleTicks < 2) return;
                        _stableIdleTicks = 0;
                        CapturePreRegenerationState();
                        Advance(UiStage.EditH2);
                        break;

                    case UiStage.EditH2:
                    {
                        var h2 = RequirePropertyEditor(_workspace!, "H2");
                        if (!AwaitAction(h2, "text", "1000")) return;
                        if (!string.Equals((h2.Text ?? string.Empty).Trim(), "1000", StringComparison.Ordinal)) return;
                        Advance(UiStage.StartSecondDraw);
                        break;
                    }

                    case UiStage.StartSecondDraw:
                    {
                        // Moving physical focus from the H2 TextBox to this real button commits the
                        // LostFocus binding first; the same physical click then starts production Draw.
                        if (!AwaitAction(() => RequireDrawButton(_workspace!), "click", string.Empty)) return;
                        if (!FamilyHasDimensions(_family!, UiEditedDimensions())) return;
                        RequireRegeneratedFirstElements();
                        if (!IsDrawCommandActive()) return;
                        Advance(UiStage.RepeatCentre);
                        break;
                    }

                    case UiStage.RepeatCentre:
                        PlaceCentre(2, 3, UiStage.EndSecondDraw);
                        break;

                    case UiStage.EndSecondDraw:
                        if (!AwaitAction(_workspace!, "key", "ESC")) return;
                        if (IsDrawCommandActive()) return;
                        if (++_stableIdleTicks < 2) return;
                        _stableIdleTicks = 0;
                        CompleteUi();
                        break;

                    default:
                        throw new ProbeException("ui_stage_invalid");
                }
            }

            private void InputField(int index, UiStage next)
            {
                var dialog = FindSingleFootingDialog(_context.Product)
                    ?? throw new ProbeException("ui_input_dialog_missing");
                _dialogInputs = RequireDialogLayout(dialog);
                var key = FieldOrder[index];
                var expected = FieldText[index];
                var input = _dialogInputs[key];
                if (!AwaitAction(input, "text", expected)) return;
                if (!string.Equals((input.Text ?? string.Empty).Trim(), expected, StringComparison.Ordinal)) return;
                Advance(next);
            }

            private void PlaceCentre(int pointIndex, int expectedNewElements, UiStage next)
            {
                var observed = NewFamilyElements();
                // Freeze the world target immediately before publishing the physical click,
                // after hover acknowledgement. Later view changes cannot move that target.
                _pendingPlacementCentre = CapturePlacementCentre(_pendingPlacementCentre, _requestWritten,
                    _moveAcknowledged, () => ScreenWorldPoint(_context.Document, _screenPoints[pointIndex]));
                var targetPoint = _pendingPlacementCentre ?? ScreenWorldPoint(_context.Document, _screenPoints[pointIndex]);
                var placementTrace = "placement " + pointIndex + " active=" + IsDrawCommandActive() +
                    " count=" + observed.Count + " expected=" + targetPoint + " actual=" +
                    string.Join("|", observed.Select(element => ReadFootprintCenter(_context.Document, element).ToString()));
                if (!string.Equals(_lastPlacementTrace, placementTrace, StringComparison.Ordinal))
                {
                    UiTrace(placementTrace);
                    _lastPlacementTrace = placementTrace;
                }
                if (!IsDrawCommandActive()) return;
                var point = _screenPoints[pointIndex];
                if (!AwaitAction(point.X, point.Y, "click", string.Empty)) return;
                var created = NewFamilyElements();
                if (created.Count != expectedNewElements) return;
                var expected = _pendingPlacementCentre ?? throw new ProbeException("ui_placement_target_not_captured");
                var match = created.SingleOrDefault(element => SamePoint(ReadFootprintCenter(_context.Document, element), expected));
                if (match == null) return;
                var dimensions = expectedNewElements <= 2 ? UiBoxDimensions() : UiEditedDimensions();
                VerifySolid(_context.Document, match, dimensions, expected, "ui_physical");
                if (_centres.Count == pointIndex) _centres.Add(expected);
                _pendingPlacementCentre = null;
                Advance(next);
            }

            private string? _lastPlacementTrace;
            private Point3d? _pendingPlacementCentre;

            private void RequireCancelUnchanged()
            {
                if (_project == null || _project.Families.Count != _familyBaseline ||
                    _project.Elements.Count != _semanticBaseline ||
                    CountModelSpaceEntities(_context.Document) != _nativeBaseline ||
                    !string.Equals(ProjectMutationDigest(_project), _cancelProjectDigest, StringComparison.Ordinal) ||
                    !string.Equals(NativeModelDigest(_context.Document), _cancelNativeDigest, StringComparison.Ordinal))
                    throw new ProbeException("ui_cancel_mutated_state");
            }

            private void RequireCreatedFamily()
            {
                if (_project == null || _baselineFamilyIds == null ||
                    _project.Families.Count != _familyBaseline + 1 ||
                    _project.Elements.Count != _semanticBaseline ||
                    CountModelSpaceEntities(_context.Document) != _nativeBaseline)
                    throw new ProbeException("ui_family_create_cardinality");
                var created = _project.Families.Where(x => !_baselineFamilyIds.Contains(x.Id)).Take(2).ToList();
                foreach (var candidate in created)
                {
                    UiTrace("created_family_keys=" + string.Join("|", candidate.Properties.Keys.OrderBy(x => x, StringComparer.Ordinal)));
                    foreach (var field in FieldOrder)
                    {
                        var key = "SINGLE_FOOTING_" + field;
                        UiTrace("created_dimension " + key + "=" + (candidate.Properties.TryGetValue(key, out var raw) ? raw : "<missing>"));
                    }
                }
                if (created.Count != 1 || !FamilyHasDimensions(created[0], UiBoxDimensions()) ||
                    !IsSingleFootingFamily(created[0]))
                    throw new ProbeException("ui_family_create_identity");
                var active = ProjectFamilyActivationService.GetActive(_project);
                if (active == null || !ReferenceEquals(active, created[0]))
                    throw new ProbeException("ui_family_not_active");
                _family = created[0];
                _familyId = created[0].Id;
            }

            private void CapturePreRegenerationState()
            {
                var created = NewFamilyElements();
                if (created.Count != 2) throw new ProbeException("ui_first_draw_cardinality");
                _firstElementIds = new HashSet<string>(created.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                _oldGeneratedHandles.Clear();
                _oldGeneratedByElement.Clear();
                foreach (var element in created)
                {
                    if (!SameDimensions(ReadDimensions(element), UiBoxDimensions()))
                        throw new ProbeException("ui_first_draw_dimensions");
                    var handle = OwnedHandle(element);
                    _oldGeneratedHandles.Add(handle);
                    _oldGeneratedByElement.Add(element.Id, handle);
                }
            }

            private void RequireRegeneratedFirstElements()
            {
                if (_project == null || _firstElementIds == null || _firstElementIds.Count != 2)
                    throw new ProbeException("ui_regeneration_baseline_missing");
                foreach (var elementId in _firstElementIds)
                {
                    var element = RequireElementById(_project, elementId);
                    if (!SameDimensions(ReadDimensions(element), UiEditedDimensions()))
                        throw new ProbeException("ui_regeneration_dimensions");
                    if (!_oldGeneratedByElement.TryGetValue(elementId, out var oldHandle))
                        throw new ProbeException("ui_regeneration_handle_baseline_missing");
                    if (string.Equals(oldHandle, OwnedHandle(element), StringComparison.OrdinalIgnoreCase))
                        throw new ProbeException("ui_regeneration_handle_unchanged");
                    RequireErased(_context.Document, oldHandle);
                    VerifySolid(_context.Document, element, UiEditedDimensions(), ReadFootprintCenter(_context.Document, element), "ui_regenerated");
                }
            }

            private List<ProjectElement> NewFamilyElements()
            {
                if (_project == null || _baselineElementIds == null) throw new ProbeException("ui_project_state_missing");
                return _project.Elements.Where(x => !_baselineElementIds.Contains(x.Id) &&
                    string.Equals(x.FamilyId, _familyId, StringComparison.OrdinalIgnoreCase) &&
                    IsSingleFootingElement(x)).ToList();
            }

            private void CompleteUi()
            {
                if (_project == null || _family == null || _baselineElementIds == null)
                    throw new ProbeException("ui_completion_state_missing");
                var elements = NewFamilyElements();
                if (_project.Families.Count != _familyBaseline + 1 ||
                    _project.Elements.Count != _semanticBaseline + 3 ||
                    CountModelSpaceEntities(_context.Document) != _nativeBaseline + 6 ||
                    elements.Count != 3 || _centres.Count != 3)
                    throw new ProbeException("ui_final_cardinality");
                if (!FamilyHasDimensions(_family, UiEditedDimensions()))
                    throw new ProbeException("ui_final_family_dimensions");

                foreach (var center in _centres)
                {
                    var matches = elements.Where(element => SamePoint(ReadFootprintCenter(_context.Document, element), center)).ToList();
                    if (matches.Count != 1) throw new ProbeException("ui_final_center_identity");
                    if (!SameDimensions(ReadDimensions(matches[0]), UiEditedDimensions()))
                        throw new ProbeException("ui_final_element_dimensions");
                    VerifySolid(_context.Document, matches[0], UiEditedDimensions(), center, "ui_final");
                }
                foreach (var oldHandle in _oldGeneratedHandles) RequireErased(_context.Document, oldHandle);
                RequireAllPhysicalReceipts(_context, _sequence);

                var continuity = UiContinuity.Capture(
                    _context,
                    _project,
                    _family,
                    elements,
                    _centres,
                    _familyBaseline,
                    _semanticBaseline,
                    _nativeBaseline);
                WriteUiContinuity(_context, continuity);

                lock (Sync)
                {
                    _uiRunState = new UiRunState(_context.Document, _context.RunId, _context.Drawing, continuity.ArtifactDigest);
                    _uiController = null;
                }
                _timer.Stop();
                _context.Document.SendStringToExecute("QS3DSAVE QSAVE QL22UISAVED ", true, false, false);
            }

            private bool AwaitAction(FrameworkElement target, string action, string text)
            {
                if (_requestWritten) return HasExactUiAck(_context, _sequence);
                var point = ElementCenter(target);
                UiTrace("request " + _stage + " " + target.GetType().Name + " " + point + " size=" + target.ActualWidth + "," + target.ActualHeight);
                return AwaitAction(point.X, point.Y, action, text);
            }

            private void OnPhysicalMouse(object sender, MouseButtonEventArgs args)
            {
                var element = args.OriginalSource as FrameworkElement;
                UiTrace("mouse " + _stage + " source=" + args.OriginalSource?.GetType().FullName +
                    " text=" + (element is TextBlock text ? text.Text : element?.Name) +
                    " position=" + args.GetPosition(_workspace));
                DependencyObject? ancestor = args.OriginalSource as DependencyObject;
                while (ancestor != null && !ReferenceEquals(ancestor, _workspace))
                {
                    if (ancestor is TreeViewItem row) UiTrace("hit_tree=" + row.Header + " selected=" + row.IsSelected);
                    if (ancestor is Button button) UiTrace("hit_button=" + button.Content + " tooltip=" + button.ToolTip);
                    ancestor = VisualTreeHelper.GetParent(ancestor);
                }
                if (_stage == UiStage.SelectTree)
                {
                    var target = RequireSingleFootingTree(_workspace!);
                    var label = FindVisualDescendants<TextBlock>(target).Single(text => text.IsVisible && text.Text == "Móng đơn");
                    UiTrace("current_target=" + ElementCenter(label) + " relative=" + label.TranslatePoint(new WpfPoint(label.ActualWidth / 2,label.ActualHeight / 2), _workspace));
                }
            }

            private void OnPhysicalButtonClick(object sender, RoutedEventArgs args)
            {
                UiTrace("button_click " + _stage + " source=" + args.OriginalSource + " handled=" + args.Handled);
                TraceWorkspaceRoute("after_button");
            }

            private void TraceWorkspaceRoute(string label)
            {
                var tree = FindVisualDescendants<TreeView>(_workspace!).Single(x => x.Name == "ModelTree");
                var item = tree.SelectedItem as TreeViewItem;
                var category = _workspace!.GetType().GetField("_categoryFilter", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_workspace);
                UiTrace(label + " tree=" + item?.Header + " tag=" + item?.Tag + " category=" + category +
                    " families=" + (_project == null ? "unbound" : string.Join(",", _project.Families.Select(x => x.Category.ToString()))));
                UiTrace(label + " roots=" + string.Join(",", PresentationSource.CurrentSources.Cast<PresentationSource>().Select(x => x.RootVisual?.GetType().FullName)));
                foreach (var source in PresentationSource.CurrentSources.Cast<PresentationSource>())
                {
                    var root = source.RootVisual;
                    if (root == null) continue;
                    UiTrace(label + " window=" + Window.GetWindow(root)?.GetType().FullName + " texts=" +
                        string.Join("|", FindVisualDescendants<TextBlock>(root).Where(x => x.IsVisible).Take(18).Select(x => x.Text)));
                }
                var add = RequireWorkspaceButton(_workspace!, "+ Add", "+ Thêm");
                var handlers = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(add, null);
                var lookup = handlers?.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (lookup?.Invoke(handlers, new object[] { Button.ClickEvent }) is Array routes)
                    foreach (var route in routes)
                    {
                        var handler = route.GetType().GetProperty("Handler")?.GetValue(route, null) as Delegate;
                        UiTrace(label + " add_handler=" + handler?.Method.Name);
                    }
            }

            private void UiTrace(string value)
            {
                File.AppendAllText(RequireUiChildPath(_context, "ui-trace.private.txt"), DateTime.UtcNow.ToString("O") + " " + value + "\n");
            }

            private bool AwaitAction(Func<FrameworkElement> target, string action, string text)
            {
                // A physical click can disable its parent or close the dialog
                // before the ACK arrives. Resolve controls only before sending.
                return _requestWritten ? HasExactUiAck(_context, _sequence) : AwaitAction(target(), action, text);
            }

            private bool AwaitAction(int x, int y, string action, string text)
            {
                if (!_requestWritten && action != "key")
                {
                    if (!_moveRequested)
                    {
                        _sequence++;
                        WriteUiAction(_context, _sequence, "move", x, y, string.Empty);
                        _moveRequested = true;
                        return false;
                    }
                    if (!_moveAcknowledged)
                    {
                        if (HasExactUiAck(_context, _sequence)) _moveAcknowledged = true;
                        // Re-measure next dispatcher tick after hover/scrollbar layout.
                        return false;
                    }
                }
                if (!_requestWritten)
                {
                    _sequence++;
                    WriteUiAction(_context, _sequence, action, x, y, text);
                    _requestWritten = true;
                    return false;
                }
                return HasExactUiAck(_context, _sequence);
            }

            private void Advance(UiStage next)
            {
                _stage = next;
                _requestWritten = false;
                _moveRequested = false;
                _moveAcknowledged = false;
                _deadlineUtc = DateTime.UtcNow + UiStageTimeout;
            }

            private void Fail(System.Exception error)
            {
                _timer.Stop();
                try
                {
                    RequireUiCleanupContext(_context);
                    var dialog = FindSingleFootingDialog(_context.Product);
                    if (dialog != null) dialog.Close();
                }
                catch { }
                lock (Sync) _uiController = null;
                WriteUiFailure(_context, "ui", "ui_" + _stage.ToString().ToLowerInvariant(), error);
                QueueOwnedQuit(_context, true);
            }
        }

        private enum UiStage
        {
            LocateWorkspace,
            SelectTree,
            OpenCancelDialog,
            CancelDialog,
            OpenCreateDialog,
            InputL1,
            InputW1,
            InputL2,
            InputW2,
            InputH1,
            InputH2,
            AcceptCreateDialog,
            StartFirstDraw,
            FirstCentre,
            SecondCentre,
            EndFirstDraw,
            EditH2,
            StartSecondDraw,
            RepeatCentre,
            EndSecondDraw
        }

        private static FrameworkElement RequireProductionWorkspace(Assembly product)
        {
            var coordinator = product.GetType("QS3D.BricsCAD.V25.PaletteCoordinator", true)
                ?? throw new ProbeException("ui_palette_type_missing");
            var visible = coordinator.GetProperty("IsWorkspaceVisible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new ProbeException("ui_palette_visibility_missing");
            if (!Convert.ToBoolean(visible.GetValue(null, null), CultureInfo.InvariantCulture))
                throw new ProbeException("ui_workspace_not_visible");
            var field = coordinator.GetField("_workspacePanel", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new ProbeException("ui_workspace_field_missing");
            var panel = field.GetValue(null) as FrameworkElement
                ?? throw new ProbeException("ui_workspace_missing");
            if (!string.Equals(panel.GetType().FullName, "QS3D.BricsCAD.V25.UI.WorkspacePanel", StringComparison.Ordinal) ||
                !ReferenceEquals(panel.GetType().Assembly, product))
                throw new ProbeException("ui_workspace_identity");
            return panel;
        }

        private static TreeViewItem RequireSingleFootingTree(FrameworkElement workspace)
        {
            var tree = FindVisualDescendants<TreeView>(workspace)
                .SingleOrDefault(x => string.Equals(x.Name, "ModelTree", StringComparison.Ordinal))
                ?? throw new ProbeException("ui_model_tree_missing");
            var matches = TreeItems(tree.Items).Where(item =>
                string.Equals(Convert.ToString(item.Tag, CultureInfo.InvariantCulture), SingleFootingCategoryCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Convert.ToString(item.Header, CultureInfo.InvariantCulture), "Móng đơn", StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (matches.Count != 1) throw new ProbeException("ui_single_footing_tree_identity");
            RequireClickable(matches[0], "ui_single_footing_tree_not_clickable");
            return matches[0];
        }

        private static IEnumerable<TreeViewItem> TreeItems(ItemCollection items)
        {
            foreach (var item in items.OfType<TreeViewItem>())
            {
                yield return item;
                foreach (var child in TreeItems(item.Items)) yield return child;
            }
        }

        private static Button RequireWorkspaceButton(FrameworkElement workspace, params string[] labels)
        {
            // The finish pane also has an Add button. Scope to the production
            // FamilyList's toolbar and support its icon + TextBlock content.
            var familyList = FindVisualDescendants<ListBox>(workspace).SingleOrDefault(list =>
                string.Equals(list.Name, "FamilyList", StringComparison.Ordinal))
                ?? throw new ProbeException("ui_family_list_missing");
            DependencyObject? scope = VisualTreeHelper.GetParent(familyList);
            while (scope != null && !(scope is DockPanel)) scope = VisualTreeHelper.GetParent(scope);
            if (scope == null) throw new ProbeException("ui_family_toolbar_missing");
            var matches = FindVisualDescendants<Button>(scope).Where(button =>
                button.IsVisible && button.IsEnabled && labels.Any(label =>
                    string.Equals(button.Content as string, label, StringComparison.Ordinal) ||
                    FindVisualDescendants<TextBlock>(button).Any(text => text.IsVisible &&
                        string.Equals(text.Text, label, StringComparison.Ordinal)))).ToList();
            if (matches.Count != 1) throw new ProbeException("ui_workspace_button_identity");
            RequireClickable(matches[0], "ui_workspace_button_not_clickable");
            return matches[0];
        }

        private static Button RequireDrawButton(FrameworkElement workspace)
        {
            var matches = FindVisualDescendants<Button>(workspace).Where(button =>
                button.IsVisible && button.IsEnabled &&
                string.Equals(Convert.ToString(button.Content, CultureInfo.InvariantCulture), "Vẽ", StringComparison.Ordinal) &&
                HasAncestorTag(button, QuickActionsTag)).ToList();
            if (matches.Count != 1) throw new ProbeException("ui_draw_button_identity");
            RequireClickable(matches[0], "ui_draw_button_not_clickable");
            return matches[0];
        }

        private static bool HasAncestorTag(DependencyObject source, string tag)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    string.Equals(Convert.ToString(element.Tag, CultureInfo.InvariantCulture), tag, StringComparison.Ordinal)) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private static Window? FindSingleFootingDialog(Assembly product)
        {
            var app = WpfApplication.Current;
            // BricsCAD embeds WPF without necessarily creating Application.Current.
            // Observe actual presentation roots; never create an application or dialog.
            var windows = PresentationSource.CurrentSources.Cast<PresentationSource>()
                .Select(source => source.RootVisual == null ? null : Window.GetWindow(source.RootVisual)).OfType<Window>();
            if (app != null) windows = windows.Concat(app.Windows.Cast<Window>());
            var matches = windows.Distinct().Where(window => window.IsVisible &&
                string.Equals(window.GetType().FullName, "QS3D.BricsCAD.V25.UI.SingleFootingDimensionsDialog", StringComparison.Ordinal) &&
                ReferenceEquals(window.GetType().Assembly, product)).ToList();
            if (matches.Count > 1) throw new ProbeException("ui_dialog_ambiguous");
            return matches.SingleOrDefault();
        }

        private static Dictionary<string, TextBox> RequireDialogLayout(Window dialog)
        {
            RequireClickable(dialog, "ui_dialog_not_visible");
            var field = dialog.GetType().GetField("_inputs", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new ProbeException("ui_dialog_inputs_missing");
            var dictionary = field.GetValue(dialog) as IDictionary
                ?? throw new ProbeException("ui_dialog_inputs_identity");
            var expected = new HashSet<string>(new[] { "L1", "W1", "L2", "W2", "H1", "H2" }, StringComparer.Ordinal);
            var result = new Dictionary<string, TextBox>(StringComparer.Ordinal);
            foreach (DictionaryEntry pair in dictionary)
            {
                var key = pair.Key as string;
                var input = pair.Value as TextBox;
                if (key == null || input == null || !expected.Remove(key) || !input.IsVisible || !input.IsEnabled)
                    throw new ProbeException("ui_dialog_input_identity");
                RequireClickable(input, "ui_dialog_input_not_clickable");
                result.Add(key, input);
            }
            if (expected.Count != 0 || result.Count != 6) throw new ProbeException("ui_dialog_input_cardinality");

            var dialogBounds = ElementBounds(dialog);
            var bounds = result.Values.Select(ElementBounds).ToList();
            if (bounds.Any(rect => !Contains(dialogBounds, rect)))
                throw new ProbeException("ui_dialog_input_outside");
            for (var left = 0; left < bounds.Count; left++)
                for (var right = left + 1; right < bounds.Count; right++)
                    if (bounds[left].IntersectsWith(bounds[right]))
                        throw new ProbeException("ui_dialog_input_overlap");
            return result;
        }

        private static Button RequireDialogButton(Window dialog, bool isDefault)
        {
            var matches = FindVisualDescendants<Button>(dialog).Where(button =>
                button.IsVisible && button.IsEnabled && (isDefault ? button.IsDefault : button.IsCancel)).ToList();
            if (matches.Count != 1) throw new ProbeException("ui_dialog_button_identity");
            RequireClickable(matches[0], "ui_dialog_button_not_clickable");
            return matches[0];
        }

        private static TextBox RequirePropertyEditor(FrameworkElement workspace, string name)
        {
            var matches = FindVisualDescendants<TextBox>(workspace).Where(textBox =>
            {
                if (!textBox.IsVisible || !textBox.IsEnabled || textBox.IsReadOnly || textBox.DataContext == null) return false;
                var property = textBox.DataContext.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                return property != null && string.Equals(
                    Convert.ToString(property.GetValue(textBox.DataContext, null), CultureInfo.InvariantCulture),
                    name,
                    StringComparison.Ordinal);
            }).ToList();
            if (matches.Count != 1) throw new ProbeException("ui_property_editor_identity");
            RequireClickable(matches[0], "ui_property_editor_not_clickable");
            return matches[0];
        }

        private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < count; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match) yield return match;
                foreach (var nested in FindVisualDescendants<T>(child)) yield return nested;
            }
        }

        private static void RequireClickable(FrameworkElement element, string code)
        {
            if (!element.IsLoaded || !element.IsVisible || !element.IsEnabled ||
                !(element.ActualWidth > 2d) || !(element.ActualHeight > 2d) ||
                PresentationSource.FromVisual(element) == null)
                throw new ProbeException(code);
        }

        private static UiPixelRect ElementBounds(FrameworkElement element)
        {
            RequireClickable(element, "ui_element_not_clickable");
            var first = element.PointToScreen(new WpfPoint(0d, 0d));
            var second = element.PointToScreen(new WpfPoint(element.ActualWidth, element.ActualHeight));
            var left = checked((int)Math.Floor(Math.Min(first.X, second.X)));
            var top = checked((int)Math.Floor(Math.Min(first.Y, second.Y)));
            var right = checked((int)Math.Ceiling(Math.Max(first.X, second.X)));
            var bottom = checked((int)Math.Ceiling(Math.Max(first.Y, second.Y)));
            if (right <= left || bottom <= top) throw new ProbeException("ui_element_bounds_invalid");
            return new UiPixelRect(left, top, right, bottom);
        }

        private static DrawingPoint ElementCenter(FrameworkElement element)
        {
            var bounds = ElementBounds(element);
            return CheckedScreenPoint(bounds.Left + (bounds.Right - bounds.Left) / 2, bounds.Top + (bounds.Bottom - bounds.Top) / 2);
        }

        private static bool Contains(UiPixelRect outer, UiPixelRect inner) =>
            inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

        private static IReadOnlyList<DrawingPoint> RequireViewportPoints(Bricscad.ApplicationServices.Document document, FrameworkElement workspace, int count)
        {
            if (count != 3) throw new ProbeException("ui_viewport_point_count");
            using (var process = Process.GetCurrentProcess())
            {
                var window = process.MainWindowHandle;
                if (window == IntPtr.Zero || !GetClientRect(window, out var client))
                    throw new ProbeException("ui_host_client_missing");
                var origin = new UiNativePoint { X = client.Left, Y = client.Top };
                if (!ClientToScreen(window, ref origin)) throw new ProbeException("ui_host_mapping_failed");
                var width = client.Right - client.Left;
                var height = client.Bottom - client.Top;
                if (width < 640 || height < 480) throw new ProbeException("ui_host_client_too_small");
                var workspaceBounds = ElementBounds(workspace);
                var fractions = new[]
                {
                    new[] { 0.55d, 0.52d },
                    new[] { 0.66d, 0.52d },
                    new[] { 0.60d, 0.66d },
                    new[] { 0.73d, 0.60d },
                    new[] { 0.48d, 0.68d }
                };
                var viewport = Convert.ToInt32(Application.GetSystemVariable("CVPORT"), CultureInfo.InvariantCulture);
                if (viewport <= 1) throw new ProbeException("ui_model_viewport_missing");
                var result = new List<DrawingPoint>();
                foreach (var fraction in fractions)
                {
                    var point = CheckedScreenPoint(
                        origin.X + checked((int)Math.Round(width * fraction[0], MidpointRounding.AwayFromZero)),
                        origin.Y + checked((int)Math.Round(height * fraction[1], MidpointRounding.AwayFromZero)));
                    if (workspaceBounds.Contains(point.X, point.Y)) continue;
                    var drawingClient = ScreenToDrawingClient(document, point, false);
                    if (!drawingClient.HasValue) continue;
                    var world = document.Editor.PointToWorld(drawingClient.Value, viewport);
                    var roundTrip = document.Editor.PointToScreen(world, viewport);
                    if (Math.Abs(roundTrip.X - drawingClient.Value.X) > 2 || Math.Abs(roundTrip.Y - drawingClient.Value.Y) > 2) continue;
                    if (result.Any(existing => Math.Abs(existing.X - point.X) < 40 && Math.Abs(existing.Y - point.Y) < 40)) continue;
                    result.Add(point);
                    if (result.Count == count) return result;
                }
                throw new ProbeException("ui_viewport_mapping_unavailable");
            }
        }

        private static Point3d? CapturePlacementCentre(Point3d? pending, bool requestWritten, bool moveAcknowledged, Func<Point3d> readPoint)
        {
            if (pending.HasValue) return pending;
            if (requestWritten) throw new ProbeException("ui_placement_target_not_captured");
            return moveAcknowledged ? readPoint() : (Point3d?)null;
        }

        private static Point3d ScreenWorldPoint(Bricscad.ApplicationServices.Document document, DrawingPoint point)
        {
            var viewport = Convert.ToInt32(Application.GetSystemVariable("CVPORT"), CultureInfo.InvariantCulture);
            var drawingClient = ScreenToDrawingClient(document, point, true)
                ?? throw new ProbeException("ui_drawing_client_point_missing");
            var mapped = document.Editor.PointToWorld(drawingClient, viewport);
            var roundTrip = document.Editor.PointToScreen(mapped, viewport);
            if (Math.Abs(roundTrip.X - drawingClient.X) > 2 || Math.Abs(roundTrip.Y - drawingClient.Y) > 2)
                throw new ProbeException("ui_viewport_roundtrip_changed");
            return new Point3d(mapped.X, mapped.Y, 0d);
        }

        private static DrawingPoint? ScreenToDrawingClient(Bricscad.ApplicationServices.Document document, DrawingPoint point, bool requireInside)
        {
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new ProbeException("ui_drawing_document_changed");
            // sds_protos.h (installed V25/V26 SDK) declares this as HWND, unlike
            // acedGetAcadDwgView's CView*. Document.Window is the parent frame.
            var window = GetDrawingViewWindow();
            GetWindowThreadProcessId(window, out var owner);
            UiNativeRect bounds;
            using (var process = Process.GetCurrentProcess())
                if (window == IntPtr.Zero || owner != (uint)process.Id || !GetClientRect(window, out bounds))
                    throw new ProbeException("ui_drawing_window_identity");
            var native = new UiNativePoint { X = point.X, Y = point.Y };
            if (!ScreenToClient(window, ref native)) throw new ProbeException("ui_drawing_client_mapping");
            if (native.X < bounds.Left || native.X >= bounds.Right || native.Y < bounds.Top || native.Y >= bounds.Bottom)
            {
                if (requireInside) throw new ProbeException("ui_point_outside_drawing_client");
                return null;
            }
            return new DrawingPoint(native.X, native.Y);
        }

        private static DrawingPoint CheckedScreenPoint(int x, int y)
        {
            if (x < -32768 || x > 32767 || y < -32768 || y > 32767)
                throw new ProbeException("ui_screen_point_out_of_range");
            return new DrawingPoint(x, y);
        }

        private static bool IsDrawCommandActive()
        {
            var names = Convert.ToString(Application.GetSystemVariable("CMDNAMES"), CultureInfo.InvariantCulture) ?? string.Empty;
            return names.IndexOf("QS3DDRAWACTIVE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   names.IndexOf("QS3DDRAWSINGLEFOOTING", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSingleFootingFamily(ProjectFamily family) =>
            family.Category == ElementCategory.Foundation &&
            family.Properties.TryGetValue("CategoryCode", out var category) &&
            string.Equals(category, SingleFootingCategoryCode, StringComparison.OrdinalIgnoreCase);

        private static bool IsSingleFootingElement(ProjectElement element) =>
            element.Category == ElementCategory.Foundation &&
            element.Properties.TryGetValue("CategoryCode", out var category) &&
            string.Equals(category, SingleFootingCategoryCode, StringComparison.OrdinalIgnoreCase);

        private static bool FamilyHasDimensions(ProjectFamily family, SingleFootingDimensions expected) =>
            IsSingleFootingFamily(family) && SameDimensions(ReadDimensions(family), expected);

        private static SingleFootingDimensions UiBoxDimensions() =>
            new SingleFootingDimensions(2d, 2d, 1d, 1d, 1d, 0d);

        private static SingleFootingDimensions UiEditedDimensions() =>
            new SingleFootingDimensions(2d, 2d, 1d, 1d, 1d, 1d);

        private static void RequireUiContextStable(Context context)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, context.Document) ||
                !SamePath(context.Document.Name, context.Drawing))
                throw new ProbeException("ui_document_changed");
            RequireMcpMutationBoundaryPaused(context.Product);
        }

        private static void WriteUiAction(Context context, int sequence, string action, int x, int y, string value)
        {
            if (sequence < 1 || sequence > 100) throw new ProbeException("ui_action_sequence_invalid");
            if (action != "move" && action != "click" && action != "text" && action != "key") throw new ProbeException("ui_action_invalid");
            if (((action == "click" || action == "move") && value.Length != 0) ||
                (action == "text" && !Regex.IsMatch(value, @"^-?\d{1,7}(\.\d{1,4})?$", RegexOptions.CultureInvariant)) ||
                (action == "key" && value != "ENTER" && value != "ESC"))
                throw new ProbeException("ui_action_value_invalid");
            CheckedScreenPoint(x, y);
            var path = UiActionPath(context, sequence);
            var body = "{\"schema\":\"" + UiActionSchema + "\",\"run_id\":\"" + context.RunId +
                "\",\"sequence\":" + sequence.ToString(CultureInfo.InvariantCulture) +
                ",\"action\":\"" + action + "\",\"x\":" + x.ToString(CultureInfo.InvariantCulture) +
                ",\"y\":" + y.ToString(CultureInfo.InvariantCulture) + ",\"text\":\"" + value +
                "\",\"target_pid\":" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "}";
            WriteNewAtomic(path, body);
        }

        private static bool HasExactUiAck(Context context, int sequence)
        {
            var path = UiAckPath(context, sequence);
            if (!File.Exists(path)) return false;
            var file = new FileInfo(path);
            if (file.Length <= 0 || file.Length > 1024 || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ProbeException("ui_ack_file_invalid");
            var actual = File.ReadAllText(path, Encoding.UTF8);
            var expected = "{\"schema\":\"" + UiAckSchema + "\",\"run_id\":\"" + context.RunId +
                "\",\"sequence\":" + sequence.ToString(CultureInfo.InvariantCulture) + ",\"status\":\"SENT\"}";
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new ProbeException("ui_ack_identity_mismatch");
            return true;
        }

        private static void RequireAllPhysicalReceipts(Context context, int finalSequence)
        {
            if (finalSequence < 1 || finalSequence > 100) throw new ProbeException("ui_receipt_count_invalid");
            for (var sequence = 1; sequence <= finalSequence; sequence++)
            {
                if (!File.Exists(UiActionPath(context, sequence)) || !HasExactUiAck(context, sequence))
                    throw new ProbeException("ui_receipt_missing");
            }
            var unexpectedAction = Directory.EnumerateFiles(context.Root, "ui-action-*.private.json", SearchOption.TopDirectoryOnly)
                .Any(path => !Enumerable.Range(1, finalSequence).Any(sequence => SamePath(path, UiActionPath(context, sequence))));
            var unexpectedAck = Directory.EnumerateFiles(context.Root, "ui-ack-*.private.json", SearchOption.TopDirectoryOnly)
                .Any(path => !Enumerable.Range(1, finalSequence).Any(sequence => SamePath(path, UiAckPath(context, sequence))));
            if (unexpectedAction || unexpectedAck) throw new ProbeException("ui_receipt_residue");
        }

        private static string UiActionPath(Context context, int sequence) =>
            RequireUiChildPath(context, "ui-action-" + sequence.ToString("D4", CultureInfo.InvariantCulture) + ".private.json");

        private static string UiAckPath(Context context, int sequence) =>
            RequireUiChildPath(context, "ui-ack-" + sequence.ToString("D4", CultureInfo.InvariantCulture) + ".private.json");

        private static string UiContinuityPath(Context context) => RequireUiChildPath(context, "local022-ui-continuity.private");

        private static string RequireUiChildPath(Context context, string fileName)
        {
            var path = Path.GetFullPath(Path.Combine(context.Root, fileName));
            if (!IsChildPath(context.Root, path) || !string.Equals(Path.GetDirectoryName(path), Path.GetFullPath(context.Root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new ProbeException("ui_private_path_invalid");
            return path;
        }

        private static void WriteNewAtomic(string path, string body)
        {
            var temporary = path + ".tmp";
            if (File.Exists(path) || File.Exists(temporary)) throw new ProbeException("ui_output_preexists");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(body);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temporary, path);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch { }
            }
        }

        private static void WriteUiContinuity(Context context, UiContinuity continuity)
        {
            var lines = new List<string>
            {
                "schema=" + UiContinuitySchema,
                "run_id=" + context.RunId,
                "project_id=" + continuity.ProjectId,
                "family_id=" + continuity.FamilyId,
                "family_baseline=" + continuity.FamilyBaseline.ToString(CultureInfo.InvariantCulture),
                "semantic_baseline=" + continuity.SemanticBaseline.ToString(CultureInfo.InvariantCulture),
                "native_baseline=" + continuity.NativeBaseline.ToString(CultureInfo.InvariantCulture),
                "centres=" + continuity.Centres,
                "elements=" + continuity.Elements,
                "artifact_digest=" + continuity.ArtifactDigest
            };
            WriteNewAtomic(UiContinuityPath(context), string.Join("\n", lines));
        }

        private static UiContinuity ReadUiContinuity(Context context)
        {
            var path = UiContinuityPath(context);
            if (!File.Exists(path)) throw new ProbeException("ui_continuity_missing");
            var file = new FileInfo(path);
            if (file.Length <= 0 || file.Length > 16384 || (file.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ProbeException("ui_continuity_file_invalid");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var split = line.IndexOf('=');
                if (split <= 0 || values.ContainsKey(line.Substring(0, split)))
                    throw new ProbeException("ui_continuity_format");
                values.Add(line.Substring(0, split), line.Substring(split + 1));
            }
            var required = new[] { "schema", "run_id", "project_id", "family_id", "family_baseline", "semantic_baseline", "native_baseline", "centres", "elements", "artifact_digest" };
            if (values.Count != required.Length || required.Any(key => !values.ContainsKey(key)) ||
                values["schema"] != UiContinuitySchema || values["run_id"] != context.RunId)
                throw new ProbeException("ui_continuity_identity");
            return UiContinuity.Parse(values);
        }

        private static void VerifyUiPersistedState(Context context, UiContinuity continuity, string stage)
        {
            var project = GetOrCreateProject(context.Document);
            if (!string.Equals(project.ProjectId, continuity.ProjectId, StringComparison.OrdinalIgnoreCase))
                throw new ProbeException(stage + "_project_identity");
            if (project.Families.Count != continuity.FamilyBaseline + 1 ||
                project.Elements.Count != continuity.SemanticBaseline + 3 ||
                CountModelSpaceEntities(context.Document) != continuity.NativeBaseline + 6)
                throw new ProbeException(stage + "_cardinality");
            var family = project.FindFamily(continuity.FamilyId);
            if (family == null || !FamilyHasDimensions(family, UiEditedDimensions()))
                throw new ProbeException(stage + "_family_identity");
            var records = ParseElementRecords(continuity.Elements);
            var centres = ParseCentres(continuity.Centres);
            if (records.Count != 3 || centres.Count != 3) throw new ProbeException(stage + "_record_cardinality");
            foreach (var record in records)
            {
                var element = RequireElementById(project, record.ElementId);
                if (!string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase) ||
                    !IsSingleFootingElement(element) || !SameDimensions(ReadDimensions(element), UiEditedDimensions()) ||
                    !string.Equals(element.SourceHandles.SingleOrDefault(), record.SourceHandle, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(OwnedHandle(element), record.GeneratedHandle, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeException(stage + "_element_identity");
                var center = ReadFootprintCenter(context.Document, element);
                if (!centres.Any(expected => SamePoint(expected, center)))
                    throw new ProbeException(stage + "_center_identity");
                VerifySolid(context.Document, element, UiEditedDimensions(), center, stage);
            }
            var actual = ArtifactDigest(project, family, records.Select(record => RequireElementById(project, record.ElementId)), centres);
            if (!string.Equals(actual, continuity.ArtifactDigest, StringComparison.Ordinal))
                throw new ProbeException(stage + "_artifact_digest");
        }

        private static string ProjectMutationDigest(ProjectState project)
        {
            var values = new List<string>
            {
                "schema=" + project.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                "project=" + project.ProjectId,
                "version=" + project.ChangeVersion.ToString(CultureInfo.InvariantCulture)
            };
            values.AddRange(project.Metadata.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => "m=" + x.Key + "=" + x.Value));
            values.AddRange(project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(FamilySignature));
            values.AddRange(project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(ElementSignature));
            values.AddRange(project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => "z=" + x.Id + "=" + x.Name));
            values.AddRange(project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => "f=" + x.Id + "=" + x.Name + "=" + x.ElevationM.ToString("R", CultureInfo.InvariantCulture)));
            values.Add("rules=" + project.QuantityRules.Count.ToString(CultureInfo.InvariantCulture));
            values.Add("audits=" + project.AuditEvents.Count.ToString(CultureInfo.InvariantCulture));
            return HashLines(values);
        }

        private static string NativeModelDigest(Bricscad.ApplicationServices.Document document)
        {
            var values = new List<string>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                    if (entity != null && !entity.IsErased)
                        values.Add(entity.Handle + "=" + entity.GetType().FullName);
                }
            }
            return HashLines(values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private static string ArtifactDigest(ProjectState project, ProjectFamily family, IEnumerable<ProjectElement> elements, IEnumerable<Point3d> centres)
        {
            var values = new List<string> { "project=" + project.ProjectId, FamilySignature(family) };
            values.AddRange(elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(ElementSignature));
            values.AddRange(centres.OrderBy(x => x.X).ThenBy(x => x.Y).Select(x => "c=" + EncodePoint(x)));
            return HashLines(values);
        }

        private static string FamilySignature(ProjectFamily family) =>
            "family=" + family.Id + "|" + family.Name + "|" + family.Category + "|" +
            string.Join(";", family.Properties.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "=" + x.Value));

        private static string ElementSignature(ProjectElement element) =>
            "element=" + element.Id + "|" + element.FamilyId + "|" + element.Category + "|" +
            string.Join(",", element.SourceHandles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + "|" +
            string.Join(";", element.Properties.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "=" + x.Value));

        private static string HashLines(IEnumerable<string> lines)
        {
            using (var hasher = SHA256.Create())
            {
                var bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
                return string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string EncodePoint(Point3d point) =>
            point.X.ToString("R", CultureInfo.InvariantCulture) + "," +
            point.Y.ToString("R", CultureInfo.InvariantCulture) + "," +
            point.Z.ToString("R", CultureInfo.InvariantCulture);

        private static List<Point3d> ParseCentres(string raw)
        {
            var result = new List<Point3d>();
            foreach (var record in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var values = record.Split(',');
                if (values.Length != 3 || !double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                    !double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                    !double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
                    double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) ||
                    double.IsInfinity(x) || double.IsInfinity(y) || double.IsInfinity(z))
                    throw new ProbeException("ui_continuity_center_invalid");
                result.Add(new Point3d(x, y, z));
            }
            return result;
        }

        private static List<UiElementRecord> ParseElementRecords(string raw)
        {
            var result = new List<UiElementRecord>();
            foreach (var record in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var values = record.Split(',');
                if (values.Length != 3 || values.Any(value => string.IsNullOrWhiteSpace(value) ||
                    value.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_')))
                    throw new ProbeException("ui_continuity_element_invalid");
                result.Add(new UiElementRecord(values[0], values[1], values[2]));
            }
            return result;
        }

        private static void RequireUiOutputAbsent(Context context, string phase)
        {
            var marker = RequireUiChildPath(context, "phase-" + phase + ".json");
            var temporary = marker + ".tmp";
            if (File.Exists(marker) || File.Exists(temporary)) throw new ProbeException("ui_marker_preexists");
        }

        private static void WriteUiMarker(Context context, string phase, string status, string stage, string errorCode, IDictionary<string, bool> checks)
        {
            var marker = RequireUiChildPath(context, "phase-" + phase + ".json");
            var body = "{\"schema\":\"" + UiSchema + "\",\"run_id\":\"" + context.RunId + "\",\"phase\":\"" + phase +
                "\",\"status\":\"" + status + "\",\"stage\":\"" + RequireUiMarkerToken(stage, false) + "\",\"error_code\":\"" + NormalizeCode(errorCode) +
                "\",\"checks\":{" + string.Join(",", checks.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => "\"" + x.Key + "\":" + (x.Value ? "true" : "false"))) + "}}";
            WriteNewAtomic(marker, body);
        }

        private static string RequireUiMarkerToken(string value, bool upper)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0 || normalized.Length > 80 ||
                normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_'))
                throw new ProbeException("ui_marker_token_invalid");
            return upper ? normalized.ToUpperInvariant() : normalized.ToLowerInvariant();
        }

        private static void WriteUiFailure(Context? context, string phase, string stage, System.Exception error)
        {
            try
            {
                var resolved = context ?? ContextFromEnvironment(phase);
                var code = error is ProbeException probe ? probe.Code : NormalizeCode("UNEXPECTED_" + error.GetType().Name);
                WriteUiDiagnostic(resolved, phase, error);
                WriteUiMarker(resolved, phase, "FAIL", stage, code, new Dictionary<string, bool>(StringComparer.Ordinal));
            }
            catch { }
        }

        private static void WriteUiDiagnostic(Context context, string phase, System.Exception error)
        {
            try
            {
                var path = RequireUiChildPath(context, "phase-" + phase + "-diagnostic.private.txt");
                if (File.Exists(path)) return;
                var lines = new List<string>();
                for (System.Exception? current = error; current != null && lines.Count < 32; current = current.InnerException)
                {
                    lines.Add("type=" + NormalizeCode(current.GetType().FullName ?? "UNKNOWN"));
                    lines.Add("hresult=" + current.HResult.ToString("X8", CultureInfo.InvariantCulture));
                    foreach (var frame in (new StackTrace(current, false).GetFrames() ?? Array.Empty<StackFrame>()).Take(6))
                    {
                        var method = frame.GetMethod();
                        if (method != null) lines.Add("method=" + NormalizeCode((method.DeclaringType?.FullName ?? "UNKNOWN") + "_" + method.Name));
                    }
                }
                WriteNewAtomic(path, string.Join("\n", lines));
            }
            catch { }
        }

        private static void RequireUiCleanupContext(Context context)
        {
            RequireUiContextStable(context);
            if (Application.DocumentManager.Count != 1 || !IsChildPath(context.Root, context.Drawing))
                throw new ProbeException("ui_cleanup_ownership_changed");
        }

        private static void QueueOwnedQuit(Context? context, bool cancelFirst)
        {
            try
            {
                // A failed bind never grants authority over the active drawing.
                // QUIT is application-wide: refuse it after drift or another DWG opens.
                if (context == null) return;
                RequireUiCleanupContext(context);
                context.Document.SendStringToExecute((cancelFirst ? "\u001b\u001b" : string.Empty) + "_.QUIT _N ", true, false, false);
            }
            catch { }
        }

        private sealed class UiRunState
        {
            public UiRunState(Bricscad.ApplicationServices.Document document, string runId, string drawing, string artifactDigest)
            {
                Document = document; RunId = runId; Drawing = drawing; ArtifactDigest = artifactDigest;
            }
            public Bricscad.ApplicationServices.Document Document { get; }
            public string RunId { get; }
            public string Drawing { get; }
            public string ArtifactDigest { get; }
        }

        private sealed class UiContinuity
        {
            public string ProjectId { get; private set; } = string.Empty;
            public string FamilyId { get; private set; } = string.Empty;
            public int FamilyBaseline { get; private set; }
            public int SemanticBaseline { get; private set; }
            public int NativeBaseline { get; private set; }
            public string Centres { get; private set; } = string.Empty;
            public string Elements { get; private set; } = string.Empty;
            public string ArtifactDigest { get; private set; } = string.Empty;

            public static UiContinuity Capture(Context context, ProjectState project, ProjectFamily family, IEnumerable<ProjectElement> elements, IEnumerable<Point3d> centres, int familyBaseline, int semanticBaseline, int nativeBaseline)
            {
                var orderedElements = elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
                var orderedCentres = centres.OrderBy(x => x.X).ThenBy(x => x.Y).ToList();
                return new UiContinuity
                {
                    ProjectId = project.ProjectId,
                    FamilyId = family.Id,
                    FamilyBaseline = familyBaseline,
                    SemanticBaseline = semanticBaseline,
                    NativeBaseline = nativeBaseline,
                    Centres = string.Join(";", orderedCentres.Select(EncodePoint)),
                    Elements = string.Join(";", orderedElements.Select(element => element.Id + "," + element.SourceHandles.Single() + "," + OwnedHandle(element))),
                    ArtifactDigest = ArtifactDigest(project, family, orderedElements, orderedCentres)
                };
            }

            public static UiContinuity Parse(IDictionary<string, string> values)
            {
                return new UiContinuity
                {
                    ProjectId = RequireIdentity(values["project_id"], "ui_project_id_invalid"),
                    FamilyId = RequireIdentity(values["family_id"], "ui_family_id_invalid"),
                    FamilyBaseline = ParseCount(values["family_baseline"]),
                    SemanticBaseline = ParseCount(values["semantic_baseline"]),
                    NativeBaseline = ParseCount(values["native_baseline"]),
                    Centres = values["centres"],
                    Elements = values["elements"],
                    ArtifactDigest = RequireHash(values["artifact_digest"])
                };
            }

            private static int ParseCount(string value)
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result < 0)
                    throw new ProbeException("ui_continuity_count_invalid");
                return result;
            }

            private static string RequireIdentity(string value, string code)
            {
                var normalized = (value ?? string.Empty).Trim();
                if (normalized.Length == 0 || normalized.Length > 128 || normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_'))
                    throw new ProbeException(code);
                return normalized;
            }

            private static string RequireHash(string value)
            {
                var normalized = (value ?? string.Empty).Trim();
                if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
                    throw new ProbeException("ui_continuity_digest_invalid");
                return normalized.ToLowerInvariant();
            }
        }

        private sealed class UiElementRecord
        {
            public UiElementRecord(string elementId, string sourceHandle, string generatedHandle)
            {
                ElementId = elementId; SourceHandle = sourceHandle; GeneratedHandle = generatedHandle;
            }
            public string ElementId { get; }
            public string SourceHandle { get; }
            public string GeneratedHandle { get; }
        }

        private struct UiPixelRect
        {
            public UiPixelRect(int left, int top, int right, int bottom)
            {
                Left = left; Top = top; Right = right; Bottom = bottom;
            }
            public int Left { get; }
            public int Top { get; }
            public int Right { get; }
            public int Bottom { get; }
            public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;
            public bool IntersectsWith(UiPixelRect other) => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiNativeRect { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct UiNativePoint { public int X; public int Y; }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr window, out UiNativeRect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr window, ref UiNativePoint point);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr window, ref UiNativePoint point);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("bricscadapi.dll", EntryPoint = "sds_getviewhwnd", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetDrawingViewWindow();
    }
}
