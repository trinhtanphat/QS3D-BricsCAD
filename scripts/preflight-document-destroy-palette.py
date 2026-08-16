#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []


def read(relative):
    path = ADAPTER / relative
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


lifecycle = read("DocumentLifecycleCoordinator.cs")
palette = read("PaletteCoordinator.cs")
workspace = read("UI/WorkspacePanel.xaml.cs")

require(lifecycle, "docs.DocumentDestroyed += OnDocumentDestroyed;", "lifecycle start")
require(lifecycle, "docs.DocumentDestroyed -= OnDocumentDestroyed;", "lifecycle stop")
require(lifecycle, "private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e)", "destroyed handler")
require(lifecycle, "if (docs.Count == 0)", "last-document guard")
require(lifecycle, "_pendingNoDocumentReset = true;", "deferred no-document reset")
require(lifecycle, "StartLifecycleIdleTimer();", "deferred lifecycle reset scheduling")
require(lifecycle, "PaletteCoordinator.ResetForNoDocument();", "last-document palette reset")
require(lifecycle, "ScheduleReconcile(active, true);", "remaining-document deferred rebind")
require(lifecycle, "EnsureProject(document, refreshUi);", "deferred project reconcile")
require(lifecycle, "PaletteCoordinator.ResetForUnavailableProject(message);", "project-load failure workspace reset")

require(palette, "public static void ResetForNoDocument()", "no-document palette reset API")
require(palette, "private static void ResetPreservingVisibility()", "no-document palette teardown implementation")
require(palette, "var workspaceVisible = IsWorkspaceVisible;", "workspace visibility preservation")
require(palette, "var rightVisible = IsRightPanelVisible;", "right visibility preservation")
require(palette, "Dispose();", "no-document stale palette teardown")
require(palette, "EnsureCreated();", "palette creation guard")
require(palette, "public static void ResetForUnavailableProject(string status)", "unavailable-project reset API")
require(palette, "_workspacePanel?.ClearProjectForUnavailableDocument(status);", "unavailable-project workspace clear")
require(palette, "_rightPanel?.Refresh();", "unavailable-project CAD refresh")

require(workspace, "private WorkspaceViewModel _viewModel = new WorkspaceViewModel();", "replaceable workspace view model")
require(workspace, "public void ClearProject(string status)", "workspace clear API")
require(workspace, "_inspection = Array.Empty<EntitySnapshot>();", "workspace inspection clear")
require(workspace, "InspectionList.ItemsSource = _inspection;", "workspace inspection rebinding")
require(workspace, "_viewModel = new WorkspaceViewModel();", "workspace semantic callback reset")
require(workspace, "FamilyList.SelectedItem = null;", "workspace family selection clear")
require(workspace, 'ClearProject("Đọc Workspace lỗi: " + ex.Message);', "direct workspace refresh fail-closed")

if "EnsureProject(active, true);" in lifecycle:
    errors.append("DocumentDestroyed must not synchronously load/rebind project UI before BricsCAD returns to idle")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: destroyed or unavailable projects cannot leave stale Workspace semantic callbacks; no-document visibility is preserved and remaining drawings rebind through the idle reconcile boundary.")
