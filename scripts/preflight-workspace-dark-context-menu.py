#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DarkContextMenu.cs"
BASE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml.cs"
QUICK = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.QuickDraw.cs"
errors = []

for path in (PARTIAL, BASE, QUICK):
    if not path.is_file():
        errors.append("missing Workspace dark-context-menu dependency: " + str(path.relative_to(ROOT)))

if BASE.is_file():
    base = BASE.read_text(encoding="utf-8")
    for token in (
        "ConfigureWorkspaceInteractions();",
        "var familyMenu = CreateContextMenu();",
        "FamilyList.ContextMenu = familyMenu;",
        "var inspectionMenu = CreateContextMenu();",
        "InspectionList.ContextMenu = inspectionMenu;",
        'CreateMenuItem("Nhân bản Family", OnAddClick)',
        'CreateMenuItem("Focus", OnFocusSelectedClick)',
        "private ContextMenu CreateContextMenu()",
        "private MenuItem CreateMenuItem(string header, RoutedEventHandler handler)",
        "item.Click += handler;",
    ):
        if token not in base:
            errors.append("canonical Workspace menu contract missing: " + token)

if QUICK.is_file():
    quick = QUICK.read_text(encoding="utf-8")
    for token in (
        'CreateMenuItem("Vẽ Nhanh (Ctrl+D)", OnQuickDrawClick)',
        'quick.Tag = "QS3DDRAWACTIVE";',
    ):
        if token not in quick:
            errors.append("Workspace quick-draw menu contract missing: " + token)

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    for token in (
        "public partial class WorkspacePanel",
        "_darkContextMenuClassHandlerRegistered = RegisterDarkContextMenuClassHandler()",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "if (_darkContextMenuPresentationApplied)",
        "_darkContextMenuPresentationApplied = true;",
        "ApplyDarkContextMenu(FamilyList.ContextMenu);",
        "ApplyDarkContextMenu(InspectionList.ContextMenu);",
        "menu.Opened -= OnDarkContextMenuOpened;",
        "menu.Opened += OnDarkContextMenuOpened;",
        "ApplyDarkMenuItems(menu.Items);",
        "if (!item.HasItems && _darkMenuItemStyle != null)",
        "if (item.HasItems)",
        "BuildDarkContextMenuStyle()",
        "BuildDarkMenuItemStyle()",
        "BuildDarkSeparatorStyle()",
        'TryFindResource("BgRaisedBrush")',
        'TryFindResource("TextBrush")',
        'TryFindResource("BorderStrongBrush")',
        'TryFindResource("BgHoverBrush")',
        'TryFindResource("BgSelectedBrush")',
        "new ControlTemplate(typeof(ContextMenu))",
        'new FrameworkElementFactory(typeof(Border), "PopupChrome")',
        "new ControlTemplate(typeof(MenuItem))",
        'new FrameworkElementFactory(typeof(Border), "MenuChrome")',
        "Property = MenuItem.IsHighlightedProperty",
        "Property = MenuItem.IsSubmenuOpenProperty",
        "Property = UIElement.IsEnabledProperty",
        'new FrameworkElementFactory(typeof(Border), "SeparatorRule")',
        "menu.HasDropShadow = false;",
    ):
        if token not in text:
            errors.append("Workspace dark context-menu presentation missing: " + token)

    for forbidden in (
        "new ContextMenu(",
        "new MenuItem(",
        ".Click +=",
        "Send(",
        "SendStringToExecute",
        "CommandMethod(",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "SemanticCaptureService",
        "RegenerationEngine",
        "SetActiveFamily(",
        "Application.DocumentManager",
        "Transaction",
        "Tag =",
    ):
        if forbidden in text:
            errors.append("dark context-menu partial must remain presentation-only: " + forbidden)

    if text.count("FamilyList.ContextMenu") != 1:
        errors.append("dark context-menu partial must style FamilyList.ContextMenu exactly once")
    if text.count("InspectionList.ContextMenu") != 1:
        errors.append("dark context-menu partial must style InspectionList.ContextMenu exactly once")

if errors:
    print("Workspace dark context-menu preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print(
    "Workspace dark context-menu preflight PASS: existing Family/inspection menu actions are reused, "
    "popup/highlight/disabled/separator chrome is presentation-only, and command/CAD/project paths are untouched."
)
