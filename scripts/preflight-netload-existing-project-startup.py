#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        errors.append("missing required startup source: " + path)
        return ""
    return target.read_text(encoding="utf-8")


def method(text: str, start: str, end: str, label: str) -> str:
    a = text.find(start)
    b = text.find(end, a + len(start)) if a >= 0 else -1
    if a < 0 or b < 0:
        errors.append(label + " method boundary not found")
        return ""
    return text[a:b]


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + " missing required token: " + token)


entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")
palette = read("src/QS3D.BricsCAD.V25/PaletteCoordinator.cs")
ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
lifecycle = read("src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs")
workspace = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs")
right_panel = read("src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs")

initialize = method(entry, "public void Initialize()", "public void Terminate()", "PluginEntry.Initialize")
show = method(palette, "public static void Show()", "public static void Hide()", "PaletteCoordinator.Show")
set_status = method(palette, "public static void SetStatus", "public static void RefreshProject", "PaletteCoordinator.SetStatus")
refresh_project = method(palette, "public static void RefreshProject", "public static void RefreshCad", "PaletteCoordinator.RefreshProject")
refresh_cad = method(palette, "public static void RefreshCad", "public static void ResetForNoDocument", "PaletteCoordinator.RefreshCad")
reset_unavailable = method(palette, "public static void ResetForUnavailableProject", "private static void ResetPreservingVisibility", "PaletteCoordinator.ResetForUnavailableProject")
ribbon_start = method(ribbon, "public static void Start()", "public static void Stop()", "RibbonInitializationCoordinator.Start")
ribbon_document = method(ribbon, "private static void OnDocumentAvailable", "private static void StartTimedRetry", "RibbonInitializationCoordinator.OnDocumentAvailable")
ribbon_retry = method(ribbon, "private static void StartTimedRetry", "private static void StopTimedRetry", "RibbonInitializationCoordinator.StartTimedRetry")
ribbon_tick = method(ribbon, "private static void OnRetryTick", "private static bool TryInitializeAll", "RibbonInitializationCoordinator.OnRetryTick")
lifecycle_start = method(lifecycle, "public static void Start()", "public static void Stop()", "DocumentLifecycleCoordinator.Start")
lifecycle_created = method(lifecycle, "private static void OnDocumentCreated", "private static void OnDocumentActivated", "DocumentLifecycleCoordinator.OnDocumentCreated")
lifecycle_activated = method(lifecycle, "private static void OnDocumentActivated", "private static void OnDocumentToBeDestroyed", "DocumentLifecycleCoordinator.OnDocumentActivated")
lifecycle_to_destroy = method(lifecycle, "private static void OnDocumentToBeDestroyed", "private static void OnDocumentDestroyed", "DocumentLifecycleCoordinator.OnDocumentToBeDestroyed")
lifecycle_destroyed = method(lifecycle, "private static void OnDocumentDestroyed", "private static void ScheduleReconcile", "DocumentLifecycleCoordinator.OnDocumentDestroyed")
lifecycle_schedule = method(lifecycle, "private static void ScheduleReconcile", "private static void CancelPendingReconcile", "DocumentLifecycleCoordinator.ScheduleReconcile")
lifecycle_idle_timer = method(lifecycle, "private static void StartLifecycleIdleTimer", "private static void StopLifecycleIdleTimer", "DocumentLifecycleCoordinator.StartLifecycleIdleTimer")
lifecycle_idle = method(lifecycle, "private static void OnLifecycleIdle", "private static void ReconcileDocument", "DocumentLifecycleCoordinator.OnLifecycleIdle")
lifecycle_reconcile = method(lifecycle, "private static void ReconcileDocument", "private static void AttachProjectPersistence", "DocumentLifecycleCoordinator.ReconcileDocument")
workspace_initial = method(workspace, "public WorkspacePanel()", "private void BindViewModel()", "WorkspacePanel initial load")
right_initial = method(right_panel, "public RightPanel()", "public void Refresh()", "RightPanel initial load")

for token in (
    "RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();",
    "DocumentLifecycleCoordinator.Start();",
    "RibbonInitializationCoordinator.Start();",
    "UpdateBootstrapper.Start();",
):
    require(initialize, token, "PluginEntry.Initialize")

if "PaletteCoordinator.EnsureCreated();" in initialize:
    errors.append("PluginEntry.Initialize must not construct palette/WPF trees during NETLOAD")

require(show, "EnsureCreated();", "PaletteCoordinator.Show")
require(show, "SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);", "PaletteCoordinator.Show")
if "RefreshAll();" in show:
    errors.append("PaletteCoordinator.Show must not duplicate panel first-load work with RefreshAll")

for label, block in (
    ("SetStatus", set_status),
    ("RefreshProject", refresh_project),
    ("RefreshCad", refresh_cad),
    ("ResetForUnavailableProject", reset_unavailable),
):
    if "EnsureCreated();" in block:
        errors.append(label + " must remain passive and must not materialize unopened palettes")

for token in (
    "_workspacePanel = new WorkspacePanel();",
    "_rightPanel = new RightPanel();",
    "_quantityInsightPanel = new QuantityInsightPanel();",
    "public static void RefreshAll() { RefreshProject(); RefreshCad(); }",
):
    require(palette, token, "PaletteCoordinator")
require(entry, "PaletteCoordinator.Dispose();", "PluginEntry.Terminate")

for label, block, refresh in (
    ("WorkspacePanel", workspace_initial, "RefreshProject();"),
    ("RightPanel", right_initial, "Refresh();"),
):
    require(block, "Loaded += OnInitialLoaded;", label + " constructor")
    require(block, "Loaded -= OnInitialLoaded;", label + " OnInitialLoaded")
    require(block, refresh, label + " OnInitialLoaded")
if "Loaded += (_, __) => RefreshProject();" in workspace:
    errors.append("WorkspacePanel initial Loaded refresh must be one-shot, not a permanent anonymous handler")
if "Loaded += (_, __) => Refresh();" in right_panel:
    errors.append("RightPanel initial Loaded refresh must be one-shot, not a permanent anonymous handler")

require(ribbon_start, "StartTimedRetry();", "RibbonInitializationCoordinator.Start")
require(ribbon_document, "StartTimedRetry();", "RibbonInitializationCoordinator.OnDocumentAvailable")
require(ribbon_retry, "new DispatcherTimer(DispatcherPriority.ApplicationIdle)", "RibbonInitializationCoordinator.StartTimedRetry")
require(ribbon_tick, "TryInitializeAll()", "RibbonInitializationCoordinator.OnRetryTick")
require(ribbon, "private static bool _initialized;", "RibbonInitializationCoordinator")
if "TryInitializeAll()" in ribbon_start:
    errors.append("RibbonInitializationCoordinator.Start must not synchronously reconcile the ribbon during NETLOAD")
if "TryInitializeAll()" in ribbon_document:
    errors.append("RibbonInitializationCoordinator.OnDocumentAvailable must not synchronously reconcile the ribbon inside host document callbacks")

require(lifecycle_start, "ScheduleReconcile(docs.MdiActiveDocument, false);", "DocumentLifecycleCoordinator.Start")
require(lifecycle_created, "ScheduleReconcile(e.Document, false);", "DocumentLifecycleCoordinator.OnDocumentCreated")
require(lifecycle_activated, "ScheduleReconcile(e.Document, true);", "DocumentLifecycleCoordinator.OnDocumentActivated")
require(lifecycle_destroyed, "ScheduleReconcile(docs.MdiActiveDocument, true);", "DocumentLifecycleCoordinator.OnDocumentDestroyed")
require(lifecycle_destroyed, "StartLifecycleIdleTimer();", "DocumentLifecycleCoordinator.OnDocumentDestroyed no-document path")
require(lifecycle_schedule, "StartLifecycleIdleTimer();", "DocumentLifecycleCoordinator.ScheduleReconcile")
require(lifecycle_idle_timer, "new DispatcherTimer(DispatcherPriority.ApplicationIdle)", "DocumentLifecycleCoordinator.StartLifecycleIdleTimer")
require(lifecycle_idle, "ReconcileDocument(pair.Key, pair.Value);", "DocumentLifecycleCoordinator.OnLifecycleIdle")
require(lifecycle_idle, "PaletteCoordinator.ResetForNoDocument();", "DocumentLifecycleCoordinator.OnLifecycleIdle")

for token in (
    "AttachProjectPersistence(document);",
    "SourceReconcileUndoCoordinator.Attach(document);",
    "CurtainWallUndoCoordinator.Attach(document);",
    "SelectionSyncCoordinator.Attach(document);",
    "EnsureProject(document, refreshUi);",
    "SelectionSyncCoordinator.Refresh(document);",
):
    require(lifecycle_reconcile, token, "DocumentLifecycleCoordinator.ReconcileDocument")

for token in (
    "CancelPendingReconcile(document);",
    "DetachProjectPersistence(document);",
    "SourceReconcileUndoCoordinator.Detach(document);",
    "CurtainWallUndoCoordinator.Detach(document);",
    "SelectionSyncCoordinator.Detach(document);",
    "ProjectContextCoordinator.Forget(document);",
):
    require(lifecycle_to_destroy, token, "DocumentLifecycleCoordinator.OnDocumentToBeDestroyed")

host_callback_heavy_tokens = (
    "AttachProjectPersistence(",
    "SourceReconcileUndoCoordinator.Attach(",
    "CurtainWallUndoCoordinator.Attach(",
    "SelectionSyncCoordinator.Attach(",
    "EnsureProject(",
    "SelectionSyncCoordinator.Refresh(",
    "PaletteCoordinator.RefreshAll(",
    "PaletteCoordinator.ResetForNoDocument(",
)
for label, block in (
    ("Start/NETLOAD", lifecycle_start),
    ("DocumentCreated", lifecycle_created),
    ("DocumentActivated", lifecycle_activated),
    ("DocumentDestroyed", lifecycle_destroyed),
):
    for token in host_callback_heavy_tokens:
        if token in block:
            errors.append("DocumentLifecycleCoordinator.%s must defer host-callback work; found %s" % (label, token))
    if "ReconcileDocument(" in block:
        errors.append("DocumentLifecycleCoordinator.%s must not call ReconcileDocument synchronously" % label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: V25 NETLOAD defers palette/ribbon/document-lifecycle work to application idle, coalesces document reconciliation outside host callbacks, preserves synchronous teardown, keeps Workspace/RightPanel initial refresh one-shot, and avoids duplicate first-show full refresh.")