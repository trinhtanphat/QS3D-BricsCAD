#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs"
BASE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
DOC = ROOT / "docs/DIRECT-DRAW-ACTIVE-FAMILY.md"
errors = []

for path in (SOURCE, BASE, DOC):
    if not path.is_file():
        errors.append("missing Workspace active-family draw dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "if (_quickDrawInteractionsAttached) return;",
        "PreviewKeyDown += OnQuickDrawPreviewKeyDown;",
        "FamilyList.MouseDoubleClick += OnFamilyQuickDrawDoubleClick;",
        'CreateMenuItem("Vẽ Nhanh (Ctrl+D)", OnQuickDrawClick)',
        'quick.Tag = "QS3DDRAWACTIVE";',
        'CreateMenuItem("Vẽ tùy chỉnh (Ctrl+Shift+D)", OnAdvancedDrawClick)',
        'advanced.Tag = "QS3DDRAWACTIVEADV";',
        "if (e.Key != Key.D) return;",
        "modifiers == ModifierKeys.Control",
        "modifiers == (ModifierKeys.Control | ModifierKeys.Shift)",
        "ExecuteWorkspaceDraw(advanced: false)",
        "ExecuteWorkspaceDraw(advanced: true)",
        "FindContainer<ListBoxItem>(FamilyList, e.OriginalSource as DependencyObject)",
        "item.IsSelected = true;",
        "if (!(FamilyList.SelectedItem is ProjectFamily family))",
        "_viewModel.SetActiveFamily(family);",
        'var command = advanced ? "QS3DDRAWACTIVEADV" : "QS3DDRAWACTIVE";',
        "Send(command);",
    ):
        if token not in text:
            errors.append("Workspace active-family draw interaction missing: " + token)

    if text.count("Send(command);") != 1:
        errors.append("Workspace gesture layer must funnel Quick/Advanced through exactly one send site")

    for forbidden in (
        "new DirectDrawCommands",
        "new DirectDrawP1Commands",
        "new DirectDrawOpeningCommands",
        "new DirectDrawWindowCommands",
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext.Require",
        "SemanticCaptureService.Capture",
        "RegenerationEngine",
    ):
        if forbidden in text:
            errors.append("Workspace gesture layer must not duplicate authoring lifecycle: " + forbidden)

if BASE.is_file():
    text = BASE.read_text(encoding="utf-8")
    for token in (
        "AttachQuickDrawInteractions();",
        "private static T? FindContainer<T>(ItemsControl owner, DependencyObject? source)",
        "private MenuItem CreateMenuItem(string header, RoutedEventHandler handler)",
        "private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e)",
    ):
        if token not in text:
            errors.append("Workspace active-family partial relies on missing canonical helper: " + token)

    partials = list((ROOT / "src/QS3D.BricsCAD.V25/UI").glob("WorkspacePanel*.cs"))
    combined = "\n".join(path.read_text(encoding="utf-8") for path in partials)
    if combined.count("public WorkspacePanel()") != 1:
        errors.append("Workspace partials must keep exactly one public instance constructor")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "double-click a Family / Type",
        "Ctrl+D",
        "Ctrl+Shift+D",
        "Vẽ Nhanh (Ctrl+D)",
        "Vẽ tùy chỉnh (Ctrl+Shift+D)",
        "SetActiveFamily",
        "exactly the selected live Family",
        "LOCAL-008",
    ):
        if token not in text:
            errors.append("Workspace active-family draw documentation missing: " + token)

if errors:
    print("Workspace active-family draw preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Workspace active-family draw preflight PASS")
