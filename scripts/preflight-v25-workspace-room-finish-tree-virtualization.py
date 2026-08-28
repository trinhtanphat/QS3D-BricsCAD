#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

retired_safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomFinishTreeVirtualizationSafety.cs"
room_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomWorkspacePane.cs"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"
workspace_xaml_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"

for path in (room_path, theme_path, workspace_xaml_path):
    if not path.is_file():
        errors.append("missing Room finish virtualization contract file: " + str(path.relative_to(ROOT)))

if retired_safety_path.exists():
    errors.append(
        "retired ContentProperty/LogicalTree Room finish mutation hook must stay deleted; "
        "the static RoomFinishTree contract belongs in XAML before any host layout"
    )

if not errors:
    room = room_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")
    workspace_xaml = workspace_xaml_path.read_text(encoding="utf-8")

    room_tree_match = re.search(
        r'<TreeView\s+x:Name="RoomFinishTree"(?P<attrs>[^>]*)>(?P<body>.*?)</TreeView>',
        workspace_xaml,
        flags=re.DOTALL,
    )
    if not room_tree_match:
        errors.append("Workspace.xaml must declare one explicit RoomFinishTree")
    else:
        attrs = room_tree_match.group("attrs")
        body = room_tree_match.group("body")
        for token, message in (
            ('VirtualizingPanel.VirtualizationMode="Standard"',
             "RoomFinishTree must pin Standard directly in XAML before first Measure"),
            ('VirtualizingPanel.IsVirtualizing="False"',
             "RoomFinishTree must disable virtualization directly in XAML"),
            ('ScrollViewer.CanContentScroll="False"',
             "RoomFinishTree must use physical scrolling directly in XAML"),
        ):
            if token not in attrs:
                errors.append(message)

        required_items = (
            ('Header="Sàn Hoàn Thiện" Tag="FloorFinish"', "FloorFinish"),
            ('Header="Chống Thấm" Tag="Waterproofing"', "Waterproofing"),
            ('Header="Chân Tường" Tag="Skirting"', "Skirting"),
            ('Header="Hoàn Thiện Tường" Tag="WallFinish"', "WallFinish"),
            ('Header="Trần Hoàn Thiện" Tag="CeilingFinish"', "CeilingFinish"),
            ('Header="Trát Trần" Tag="CeilingFinish"', "Trát Trần"),
        )
        for token, label in required_items:
            if token not in body:
                errors.append("RoomFinishTree final static item set missing: " + label)

        if body.count('Header="Trát Trần"') != 1:
            errors.append("RoomFinishTree must contain exactly one static Trát Trần item")

    tree_count = len(re.findall(r"<TreeView(?:\s|>)", workspace_xaml))
    if tree_count != 2:
        errors.append(
            "Workspace TreeView inventory changed: expected exactly 2, found %d; "
            "review both explicit pre-layout virtualization contracts" % tree_count
        )

    workspace_partial_text = "\n".join(
        path.read_text(encoding="utf-8")
        for path in (ROOT / "src/QS3D.BricsCAD.V25/UI").glob("WorkspacePanel*.cs")
        if path.is_file()
    )
    for forbidden in (
        "ApplyRoomFinishTreeVirtualizationSafetyPreLayout",
        "FindRoomFinishTree(",
        "EnsureRoomFinishStaticItemsPreLayout",
    ):
        if forbidden in workspace_partial_text:
            errors.append("retired structural Room finish pre-layout mutation returned: " + forbidden)

    if "VirtualizingPanel.SetVirtualizationMode" in room:
        errors.append("Room Workspace Loaded/SystemIdle path must never mutate VirtualizationMode")
    if 'string.Equals(item.Header as string, "Trát Trần"' not in room:
        errors.append(
            "Room Workspace presentation must retain the duplicate guard; with the XAML item present, "
            "the historical SystemIdle add branch is guaranteed to be a no-op"
        )

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

if errors:
    print("V25 Workspace Room finish TreeView virtualization guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print(
    "V25 Workspace Room finish TreeView virtualization guard passed: explicit XAML identity, "
    "Standard/non-virtualized physical scrolling and the final static item set are fixed before "
    "host layout; the retired ContentProperty/LogicalTree mutation hook stays absent."
)
