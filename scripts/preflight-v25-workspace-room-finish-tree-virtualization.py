#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomFinishTreeVirtualizationSafety.cs"
room_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomWorkspacePane.cs"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"
workspace_xaml_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"

for path in (safety_path, room_path, theme_path, workspace_xaml_path):
    if not path.is_file():
        errors.append("missing Room finish virtualization contract file: " + str(path.relative_to(ROOT)))

if not errors:
    safety = safety_path.read_text(encoding="utf-8")
    room = room_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")
    workspace_xaml = workspace_xaml_path.read_text(encoding="utf-8")

    required_pre_layout = {
        "protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)":
            "Room finish safety must run from the Workspace construction/property lifecycle",
        "e.Property == ContentControl.ContentProperty":
            "Room finish safety must bind to root Content assignment inside InitializeComponent",
        "ApplyRoomFinishTreeVirtualizationSafetyPreLayout(content)":
            "Room finish safety must execute while root content is assigned pre-layout",
        "LogicalTreeHelper.GetChildren(root)":
            "Room finish safety must locate the static finish tree without waiting for visual realization",
        "ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty)":
            "Room finish tree must guard its local pre-layout virtualization-mode pin",
        "VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);":
            "Room finish tree must pin Standard before first Measure",
        "VirtualizingPanel.SetIsVirtualizing(tree, false);":
            "Room finish tree must disable virtualization locally",
        "ScrollViewer.SetCanContentScroll(tree, false);":
            "Room finish tree must use physical scrolling",
        "EnsureRoomFinishStaticItemsPreLayout(tree);":
            "Room finish final static item set must be materialized before first Measure",
        'Header = "Trát Trần"':
            "Trát Trần must be present in the pre-layout static item materialization",
    }
    for token, message in required_pre_layout.items():
        if token not in safety:
            errors.append(message)

    mode_index = safety.find("VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);")
    disable_index = safety.find("VirtualizingPanel.SetIsVirtualizing(tree, false);")
    static_items_index = safety.find("EnsureRoomFinishStaticItemsPreLayout(tree);")
    if mode_index < 0 or disable_index < 0 or mode_index > disable_index:
        errors.append("Room finish Standard mode must be established before virtualization is disabled")
    if static_items_index < disable_index:
        errors.append("Room finish static items must be materialized only after the local pre-layout mode/scroll contract is fixed")

    for category in ("FloorFinish", "Waterproofing", "WallFinish", "CeilingFinish"):
        if "ElementCategory.%s.ToString()" % category not in safety:
            errors.append("Room finish owner identification missing category: " + category)

    if "VirtualizingPanel.SetVirtualizationMode" in room:
        errors.append("Room Workspace Loaded/SystemIdle path must never mutate VirtualizationMode")
    if 'string.Equals(item.Header as string, "Trát Trần"' not in room:
        errors.append("Room Workspace presentation must retain the duplicate guard that makes the late add path a no-op")

    tree_style = re.search(
        r'<Style\s+TargetType="\{x:Type\s+TreeView\}"[^>]*>(.*?)</Style>',
        theme,
        flags=re.DOTALL,
    )
    if not tree_style:
        errors.append("Theme.xaml missing implicit TreeView style")
    else:
        style = tree_style.group(1)
        if 'Property="VirtualizingPanel.IsVirtualizing" Value="True"' not in style:
            errors.append("Theme.xaml must keep TreeView virtualization enabled globally")
        if 'Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"' not in style:
            errors.append("Theme.xaml must keep TreeView Recycling globally")

    tree_count = len(re.findall(r"<TreeView(?:\s|>)", workspace_xaml))
    if tree_count != 2:
        errors.append("Workspace TreeView inventory changed: expected exactly 2, found %d; review pre-layout virtualization contract" % tree_count)

if errors:
    print("V25 Workspace Room finish TreeView virtualization guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("V25 Workspace Room finish TreeView virtualization guard passed")