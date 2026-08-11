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
        errors.append("missing Workspace quick-draw dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "static WorkspacePanel()",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "if (_quickDrawInteractionsAttached) return;",
        "PreviewKeyDown += OnQuickDrawPreviewKeyDown;",
        "FamilyList.MouseDoubleClick += OnFamilyQuickDrawDoubleClick;",
        'CreateMenuItem("Vẽ Nhanh (Ctrl+D)", OnQuickDrawClick)',
        'quick.Tag = "QS3DDRAWACTIVE";',
        "Keyboard.Modifiers != ModifierKeys.Control || e.Key != Key.D",
        "FindContainer<ListBoxItem>(FamilyList, e.OriginalSource as DependencyObject)",
        "item.IsSelected = true;",
        "if (!(FamilyList.SelectedItem is ProjectFamily family))",
        "_viewModel.SetActiveFamily(family);",
        'Send("QS3DDRAWACTIVE");',
    ):
        if token not in text:
            errors.append("Workspace quick-draw interaction missing: " + token)

    if text.count('Send("QS3DDRAWACTIVE");') != 1:
        errors.append("Workspace gesture layer must funnel through exactly one QS3DDRAWACTIVE send site")

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
        "private static T? FindContainer<T>(ItemsControl owner, DependencyObject? source)",
        "private MenuItem CreateMenuItem(string header, RoutedEventHandler handler)",
        "private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e)",
    ):
        if token not in text:
            errors.append("Workspace quick-draw partial relies on missing canonical helper: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "double-click a Family / Type",
        "Ctrl+D",
        "Vẽ Nhanh (Ctrl+D)",
        "SetActiveFamily",
        "exactly the selected live Family",
        "LOCAL-008",
    ):
        if token not in text:
            errors.append("Workspace quick-draw documentation missing: " + token)

if errors:
    print("Workspace quick draw preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Workspace quick draw preflight PASS")
