#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceQuickActions.cs"
errors = []

if not source_path.is_file():
    errors.append("missing WorkspacePanel.ReferenceQuickActions.cs")
    source = ""
else:
    source = source_path.read_text(encoding="utf-8")

for token in (
    "using System.Windows.Threading;",
    'private const string ReferenceQuickActionsTag = "QS3D_REFERENCE_QUICK_ACTIONS";',
    "panel.Dispatcher.BeginInvoke(",
    "DispatcherPriority.Loaded,",
    "new Action(panel.ApplyReferenceQuickActions));",
    "var modelDock = ResolveReferenceQuickActionsHost();",
    "private DockPanel? ResolveReferenceQuickActionsHost()",
    "if (ModelTree.Parent is DockPanel modelDock)",
    "if (!(ModelTree.Parent is TabItem modelTab) || !ReferenceEquals(modelTab.Content, ModelTree))",
    "modelTab.Content = null;",
    "var modelHost = new DockPanel { LastChildFill = true };",
    "modelHost.Children.Add(ModelTree);",
    "modelTab.Content = modelHost;",
    "DockPanel.SetDock(band, Dock.Top);",
    "modelDock.Children.Insert(modelTreeIndex, band);",
):
    if token not in source:
        errors.append("reference quick-actions model-tab contract missing: " + token)

apply_start = source.find("private void ApplyReferenceQuickActions()")
resolver_start = source.find("private DockPanel? ResolveReferenceQuickActionsHost()")
apply_method = source[apply_start:resolver_start] if apply_start >= 0 and resolver_start > apply_start else ""
if not apply_method:
    errors.append("could not isolate ApplyReferenceQuickActions for parent-gate regression check")
else:
    for stale in (
        "if (!(ModelTree.Parent is DockPanel modelDock))",
        "if (ModelTree.Parent is not DockPanel modelDock)",
    ):
        if stale in apply_method:
            errors.append("stale direct DockPanel parent gate returned to ApplyReferenceQuickActions: " + stale)

if source.count('"QS3D_REFERENCE_QUICK_ACTIONS"') != 1:
    errors.append("reference quick-actions tag literal must have exactly one declaration")

for token in (
    "ExecuteWorkspaceDraw(advanced: true);",
    "ExecuteWorkspaceDraw(advanced: false);",
    'ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường");',
    'ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật");',
    'ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn");',
    "private void OnReferenceAddClick(object sender, RoutedEventArgs e) => OnAddClick(sender, e);",
    "private void OnReferenceDeleteClick(object sender, RoutedEventArgs e) => OnDeleteClick(sender, e);",
    "private void OnReferenceCaptureClick(object sender, RoutedEventArgs e) => OnCaptureSelectedClick(sender, e);",
):
    if token not in source:
        errors.append("reference quick-actions must keep delegating to the existing Workspace flow: " + token)

print("QS3D reference quick-actions model-tab preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Loaded-order convergence, Project Browser TabItem normalization, single-band identity, and existing Workspace action delegation are source-guarded.")
