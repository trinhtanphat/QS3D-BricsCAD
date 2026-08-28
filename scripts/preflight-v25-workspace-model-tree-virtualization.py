#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ModelTreeVirtualizationSafety.cs"
workspace_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
browser_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs"
augmenter_path = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"
polish_path = ROOT / "src/QS3D.BricsCAD.V25/UI/ProductionUiPolish.cs"

for path in (safety_path, workspace_path, browser_path, augmenter_path, theme_path, polish_path):
    if not path.is_file():
        errors.append("missing Workspace virtualization contract file: " + str(path.relative_to(ROOT)))

if not errors:
    safety = safety_path.read_text(encoding="utf-8")
    workspace = workspace_path.read_text(encoding="utf-8")
    browser = browser_path.read_text(encoding="utf-8")
    augmenter = augmenter_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")
    polish = polish_path.read_text(encoding="utf-8")

    required_safety = {
        "private static void ApplyModelTreeVirtualizationSafety(WorkspacePanel panel)": "ModelTree safety helper missing",
        "VirtualizingPanel.SetVirtualizationMode(panel.ModelTree, VirtualizationMode.Standard);": "ModelTree must pin Standard mode before first host layout",
        "VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);": "ModelTree must opt out of recycling virtualization locally",
        "ScrollViewer.SetCanContentScroll(panel.ModelTree, false);": "ModelTree must use physical scrolling so a virtualizing items host is not selected",
    }
    for token, message in required_safety.items():
        if token not in safety:
            errors.append(message)

    constructor_match = re.search(
        r"public\s+WorkspacePanel\s*\(\s*\)\s*\{(.*?)\n\s*\}",
        workspace,
        flags=re.DOTALL,
    )
    if not constructor_match:
        errors.append("WorkspacePanel constructor not found")
    else:
        constructor_body = constructor_match.group(1)
        if not re.search(r"InitializeComponent\(\);\s*ApplyModelTreeVirtualizationSafety\(this\);", constructor_body):
            errors.append("ModelTree safety must be applied immediately after InitializeComponent and before first host layout")
        if constructor_body.count("ApplyModelTreeVirtualizationSafety(this);") != 1:
            errors.append("ModelTree safety must be applied exactly once from the constructor")

    # Regression contract from licensed V25 preview .10230: once the TreeView has a local
    # virtualization contract, reparenting must not re-write virtualization state at Loaded.
    # Local dependency-property values follow the same TreeView instance into the TabControl.
    forbidden_loaded_tokens = (
        "FrameworkElement.LoadedEvent",
        "OnModelTreeVirtualizationSafetyLoaded",
        "RegisterClassHandler",
        "RoutedEventHandler",
        "ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty)",
    )
    for token in forbidden_loaded_tokens:
        if token in safety:
            errors.append("ModelTree safety must remain constructor-only; forbidden Loaded-time token: " + token)

    if safety.count("ApplyModelTreeVirtualizationSafety(") != 1:
        errors.append("ModelTree safety helper must have no call path other than the constructor-owned call in WorkspacePanel.xaml.cs")

    mode_set_index = safety.find("VirtualizingPanel.SetVirtualizationMode(panel.ModelTree, VirtualizationMode.Standard);")
    is_virtualizing_index = safety.find("VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);")
    scroll_index = safety.find("ScrollViewer.SetCanContentScroll(panel.ModelTree, false);")
    if mode_set_index < 0 or is_virtualizing_index < 0 or scroll_index < 0:
        pass
    elif not (mode_set_index < is_virtualizing_index < scroll_index):
        errors.append("ModelTree virtualization contract must establish Standard mode, then disable virtualization, then physical scrolling")

    if "ApplyVirtualizationDefaults(root)" in polish:
        errors.append("ProductionUiPolish Loaded path must not traverse item controls to apply virtualization defaults")
    if "VirtualizingPanel.VirtualizationModeProperty" in polish:
        errors.append("ProductionUiPolish Loaded path must not mutate VirtualizationMode after ItemsHost Measure")

    browser_tokens = (
        "modelDock.Children.Remove(ModelTree);",
        'tabs.Items.Add(new TabItem { Header = "Mô hình", Content = ModelTree });',
    )
    if any(token not in browser for token in browser_tokens):
        errors.append("Project Browser must still use the canonical ModelTree reparenting path guarded by constructor-only local containment")

    if not re.search(r"\btree\.Items\.Remove\(", augmenter) or not re.search(r"\.Items\.Insert\(", augmenter):
        errors.append("reference-tree registry must retain explicit container reordering evidence covered by the containment")

    # The fix is deliberately narrow. Do not regress the production virtualization policy for
    # normal data-heavy controls while protecting the small explicit navigation tree.
    for control in ("TreeView", "ListView", "ListBox"):
        style_match = re.search(
            r'<Style\s+TargetType="\{x:Type\s+' + re.escape(control) + r'\}"[^>]*>(.*?)</Style>',
            theme,
            flags=re.DOTALL,
        )
        if not style_match:
            errors.append("Theme.xaml missing implicit " + control + " style")
            continue
        style = style_match.group(1)
        if 'Property="VirtualizingPanel.IsVirtualizing" Value="True"' not in style:
            errors.append("Theme.xaml must keep " + control + " virtualization enabled globally")
        if 'Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"' not in style:
            errors.append("Theme.xaml must keep " + control + " recycling virtualization for data-heavy surfaces")

if errors:
    print("V25 Workspace ModelTree virtualization containment guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("V25 Workspace ModelTree virtualization containment guard passed")
