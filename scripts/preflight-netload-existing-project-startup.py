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

require(ribbon_start, "StartTimedRetry();", "RibbonInitializationCoordinator.Start")
require(ribbon_document, "StartTimedRetry();", "RibbonInitializationCoordinator.OnDocumentAvailable")
require(ribbon_retry, "new DispatcherTimer(DispatcherPriority.ApplicationIdle)", "RibbonInitializationCoordinator.StartTimedRetry")
require(ribbon_tick, "TryInitializeAll()", "RibbonInitializationCoordinator.OnRetryTick")
require(ribbon, "private static bool _initialized;", "RibbonInitializationCoordinator")
if "TryInitializeAll()" in ribbon_start:
    errors.append("RibbonInitializationCoordinator.Start must not synchronously reconcile the ribbon during NETLOAD")
if "TryInitializeAll()" in ribbon_document:
    errors.append("RibbonInitializationCoordinator.OnDocumentAvailable must not synchronously reconcile the ribbon inside host document callbacks")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: V25 NETLOAD defers palette/ribbon construction and keeps ribbon reconciliation at application-idle priority; first QS3D show avoids duplicate full refresh.")
