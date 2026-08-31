#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

model_safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ModelTreeVirtualizationSafety.cs"
property_safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.PropertyListVirtualizationSafety.cs"
workspace_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
browser_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs"
augmenter_path = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"
polish_path = ROOT / "src/QS3D.BricsCAD.V25/UI/ProductionUiPolish.cs"

for path in (model_safety_path, property_safety_path, workspace_path, browser_path, augmenter_path, theme_path, polish_path):
    if not path.is_file():
        errors.append("missing Workspace virtualization contract file: " + str(path.relative_to(ROOT)))

if not errors:
    model_safety = model_safety_path.read_text(encoding="utf-8")
    property_safety = property_safety_path.read_text(encoding="utf-8")
    workspace = workspace_path.read_text(encoding="utf-8")
    browser = browser_path.read_text(encoding="utf-8")
    augmenter = augmenter_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")
    polish = polish_path.read_text(encoding="utf-8")

    required_model_safety = {
        "private static void ApplyModelTreeVirtualizationSafety(WorkspacePanel panel)": "ModelTree safety helper missing",
        "VirtualizingPanel.SetVirtualizationMode(panel.ModelTree, VirtualizationMode.Standard);": "ModelTree must pin Standard mode before first host layout",
        "VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);": "ModelTree must opt out of recycling virtualization locally",
        "ScrollViewer.SetCanContentScroll(panel.ModelTree, false);": "ModelTree must use physical scrolling so a virtualizing items host is not selected",
        "panel.EnsureProjectBrowserSurface();": "ModelTree must enter its final Project Browser host before first layout",
        "ApplyPropertyListVirtualizationSafety(panel);": "constructor-owned ModelTree boundary must also contain PropertyList before BindViewModel grouping",
    }
    for token, message in required_model_safety.items():
        if token not in model_safety:
            errors.append(message)

    required_property_safety = {
        "private static void ApplyPropertyListVirtualizationSafety(WorkspacePanel panel)": "PropertyList safety helper missing",
        "VirtualizingPanel.SetVirtualizationMode(panel.PropertyList, VirtualizationMode.Standard);": "PropertyList must pin Standard mode before grouping/first host layout",
        "VirtualizingPanel.SetIsVirtualizing(panel.PropertyList, false);": "PropertyList must opt out of virtualization locally before grouping",
        "ScrollViewer.SetCanContentScroll(panel.PropertyList, false);": "PropertyList must use physical scrolling before grouping",
    }
    for token, message in required_property_safety.items():
        if token not in property_safety:
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
        expected_order = re.search(
            r"InitializeComponent\(\);\s*"
            r"ApplyModelTreeVirtualizationSafety\(this\);\s*"
            r"BindViewModel\(\);",
            constructor_body,
        )
        if not expected_order:
            errors.append("Workspace constructor must invoke the combined virtualization boundary before BindViewModel/grouping")
        if constructor_body.count("ApplyModelTreeVirtualizationSafety(this);") != 1:
            errors.append("combined Workspace virtualization boundary must be applied exactly once from the constructor")

    forbidden_loaded_tokens = (
        "FrameworkElement.LoadedEvent",
        "OnModelTreeVirtualizationSafetyLoaded",
        "OnPropertyListVirtualizationSafetyLoaded",
        "RegisterClassHandler",
        "RoutedEventHandler",
        "ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty)",
    )
    for source_name, source in (("ModelTree", model_safety), ("PropertyList", property_safety)):
        for token in forbidden_loaded_tokens:
            if token in source:
                errors.append(source_name + " safety must remain constructor-only; forbidden Loaded-time token: " + token)

    if model_safety.count("ApplyModelTreeVirtualizationSafety(") != 1:
        errors.append("ModelTree safety helper must have no call path other than the constructor-owned call")
    if property_safety.count("ApplyPropertyListVirtualizationSafety(") != 1:
        errors.append("PropertyList safety helper must only be invoked through the constructor-owned ModelTree boundary")

    model_mode = model_safety.find("VirtualizingPanel.SetVirtualizationMode(panel.ModelTree, VirtualizationMode.Standard);")
    model_virtual = model_safety.find("VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);")
    model_scroll = model_safety.find("ScrollViewer.SetCanContentScroll(panel.ModelTree, false);")
    model_surface = model_safety.find("panel.EnsureProjectBrowserSurface();")
    property_call = model_safety.find("ApplyPropertyListVirtualizationSafety(panel);")
    if min(model_mode, model_virtual, model_scroll, model_surface, property_call) >= 0 and not (model_mode < model_virtual < model_scroll < model_surface < property_call):
        errors.append("constructor-owned boundary must contain ModelTree, reparent it, then contain PropertyList before BindViewModel")

    property_mode = property_safety.find("VirtualizingPanel.SetVirtualizationMode(panel.PropertyList, VirtualizationMode.Standard);")
    property_virtual = property_safety.find("VirtualizingPanel.SetIsVirtualizing(panel.PropertyList, false);")
    property_scroll = property_safety.find("ScrollViewer.SetCanContentScroll(panel.PropertyList, false);")
    if min(property_mode, property_virtual, property_scroll) >= 0 and not (property_mode < property_virtual < property_scroll):
        errors.append("PropertyList contract must pin Standard mode, disable virtualization, then select physical scrolling")

    if "ApplyVirtualizationDefaults(root)" in polish:
        errors.append("ProductionUiPolish Loaded path must not traverse item controls to apply virtualization defaults")
    if "VirtualizingPanel.VirtualizationModeProperty" in polish:
        errors.append("ProductionUiPolish Loaded path must not mutate VirtualizationMode after ItemsHost Measure")

    browser_tokens = (
        "if (_browserTabs != null || !(ModelTree.Parent is DockPanel modelDock)) return;",
        "modelDock.Children.Remove(ModelTree);",
        'tabs.Items.Add(new TabItem { Header = "Mô hình", Content = ModelTree });',
    )
    if any(token not in browser for token in browser_tokens):
        errors.append("Project Browser must keep one idempotent canonical ModelTree reparenting path for constructor-time containment")

    attach_match = re.search(
        r"private\s+void\s+AttachProjectBrowser\s*\(\s*\)\s*\{(.*?)\n\s*\}",
        browser,
        flags=re.DOTALL,
    )
    if not attach_match or "EnsureProjectBrowserSurface();" not in attach_match.group(1):
        errors.append("Loaded Project Browser attachment must retain only the idempotent surface ensure before event wiring")

    if not re.search(r"\btree\.Items\.Remove\(", augmenter) or not re.search(r"\.Items\.Insert\(", augmenter):
        errors.append("reference-tree registry must retain explicit container reordering evidence covered by the containment")

    # Keep the global data-heavy virtualization policy intact. The BricsCAD V25 containment is
    # intentionally local to the two small explicit/grouped Workspace controls above.
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
    print("V25 Workspace virtualization containment guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("V25 Workspace virtualization containment guard passed")
