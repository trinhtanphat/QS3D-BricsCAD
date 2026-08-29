#!/usr/bin/env python3
"""Fail closed if the grouped Workspace PropertyList can virtualize during first host layout."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
errors = []

safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.PropertyListVirtualizationSafety.cs"
workspace_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
workspace_xaml_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"

for path in (safety_path, workspace_path, workspace_xaml_path, theme_path):
    if not path.is_file():
        errors.append("missing PropertyList first-layout contract file: " + str(path.relative_to(ROOT)))

if not errors:
    safety = safety_path.read_text(encoding="utf-8")
    workspace = workspace_path.read_text(encoding="utf-8")
    workspace_xaml = workspace_xaml_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")

    property_list = re.search(
        r'<ListView\s+x:Name="PropertyList"(?P<attrs>[^>]*)>(?P<body>.*?)</ListView>',
        workspace_xaml,
        flags=re.DOTALL,
    )
    if not property_list:
        errors.append("Workspace.xaml must declare the grouped PropertyList")
    elif "<ListView.GroupStyle>" not in property_list.group("body"):
        errors.append("PropertyList grouping contract disappeared; review first-layout containment")

    required_safety = {
        "private static void ApplyPropertyListVirtualizationSafety(WorkspacePanel panel)":
            "PropertyList safety helper missing",
        "VirtualizingPanel.SetVirtualizationMode(panel.PropertyList, VirtualizationMode.Standard);":
            "PropertyList must pin Standard mode before grouping and first host layout",
        "VirtualizingPanel.SetIsVirtualizing(panel.PropertyList, false);":
            "PropertyList must opt out of recycling virtualization locally",
        "ScrollViewer.SetCanContentScroll(panel.PropertyList, false);":
            "PropertyList must use physical scrolling so a virtualizing items host is not selected",
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
        required_sequence = (
            r"InitializeComponent\(\);\s*"
            r"ApplyModelTreeVirtualizationSafety\(this\);\s*"
            r"ApplyPropertyListVirtualizationSafety\(this\);\s*"
            r"BindViewModel\(\);"
        )
        if not re.search(required_sequence, constructor_body):
            errors.append(
                "PropertyList safety must run exactly after ModelTree safety and before BindViewModel groups the view"
            )
        if constructor_body.count("ApplyPropertyListVirtualizationSafety(this);") != 1:
            errors.append("PropertyList safety must have exactly one constructor-owned call")

    forbidden_late_tokens = (
        "FrameworkElement.LoadedEvent",
        "RegisterClassHandler",
        "RoutedEventHandler",
        "Dispatcher.BeginInvoke",
        "ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty)",
    )
    for token in forbidden_late_tokens:
        if token in safety:
            errors.append("PropertyList safety must remain constructor-only; forbidden late-layout token: " + token)

    mode_index = safety.find("VirtualizingPanel.SetVirtualizationMode(panel.PropertyList, VirtualizationMode.Standard);")
    enabled_index = safety.find("VirtualizingPanel.SetIsVirtualizing(panel.PropertyList, false);")
    scroll_index = safety.find("ScrollViewer.SetCanContentScroll(panel.PropertyList, false);")
    if min(mode_index, enabled_index, scroll_index) >= 0 and not (mode_index < enabled_index < scroll_index):
        errors.append("PropertyList safety must pin mode, disable virtualization, then select physical scrolling")

    list_style = re.search(
        r'<Style\s+TargetType="\{x:Type\s+ListView\}"[^>]*>(.*?)</Style>',
        theme,
        flags=re.DOTALL,
    )
    if not list_style:
        errors.append("Theme.xaml missing implicit ListView style")
    else:
        style = list_style.group(1)
        if 'Property="VirtualizingPanel.IsVirtualizing" Value="True"' not in style:
            errors.append("Theme.xaml must retain global ListView virtualization outside the narrow PropertyList containment")
        if 'Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"' not in style:
            errors.append("Theme.xaml must retain global ListView Recycling outside the narrow PropertyList containment")

if errors:
    print("V25 Workspace PropertyList first-layout virtualization guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print(
    "V25 Workspace PropertyList first-layout virtualization guard passed: grouped list is pinned "
    "Standard/non-virtualized with physical scrolling before BindViewModel groups the view."
)
