#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.RoomWorkspacePane.cs"
ROOM_FINISH_SAFETY = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.RoomFinishTreeVirtualizationSafety.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text: str, needle: str, message: str) -> None:
    if needle in text:
        raise SystemExit("ERROR: " + message + " (forbidden: " + needle + ")")


def method_body(text: str, marker: str) -> str:
    start = text.find(marker)
    if start < 0:
        raise SystemExit("ERROR: missing method: " + marker)
    end = text.find("\n        private ", start + len(marker))
    return text[start:] if end < 0 else text[start:end]


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    room_finish_safety = ROOM_FINISH_SAFETY.read_text(encoding="utf-8")
    xaml = XAML.read_text(encoding="utf-8")

    rewire = method_body(source, "private void RewireBlt3dRoomAwareAddActions")
    require(
        rewire,
        ".Where(IsBlt3dFamilyAddButton)",
        "Room-aware Add rewiring must target only the established generic Family Add control",
    )
    forbid(
        source,
        "IsBlt3dRoomAwareAddButton",
        "Room-specific broad button matching can steal the room-pane + Thêm finish action",
    )
    require(
        rewire,
        "OnBlt3dRoomAwareAddClick",
        "generic Family Add must be redirected through the Room-aware handler",
    )

    add_handler = method_body(source, "private void OnBlt3dRoomAwareAddClick")
    add_compact = " ".join(add_handler.split())
    require(
        add_compact,
        "if (!IsBlt3dRoomWorkspace())",
        "Room-aware Add must preserve the generic chooser outside Room workspace",
    )
    require(
        add_handler,
        "CreateRoomFromWorkspace();",
        "Room + Add must create a Room directly",
    )
    require(
        add_handler,
        "OnBlt3dFamilyAddClick(sender, e);",
        "non-Room Add must keep the established Tham số/Solid3D chooser",
    )
    forbid(
        add_handler.split("CreateRoomFromWorkspace();", 1)[-1],
        "ShowBlt3dFamilyModeChooser",
        "Room direct Add must not open the generic Family chooser after creation",
    )

    create_room = method_body(source, "private void CreateRoomFromWorkspace")
    require(create_room, "ElementCategory.Room", "direct Room creation must be pinned to Room category")
    require(create_room, "NextRoomWorkspaceFamilyName", "Room creation must use sequential room naming")
    require(source, 'var candidate = "Phòng-" + index;', "Room names must follow Phòng-N")
    require(source, "SeedRoomFamilyDefaults(family);", "direct Room creation must retain Room defaults")

    layout = method_body(source, "private void ApplyBlt3dRoomWorkspaceLayout")
    require(layout, "columns[3]", "Room layout must restore the right splitter column")
    require(layout, "columns[4]", "Room layout must restore the Room detail column")
    require(layout, "ReferenceEquals(child, roomPane)", "Room detail pane must be made visible")
    require(
        source,
        "WorkspaceOverflow.LayoutUpdated += OnBlt3dRoomWorkspaceLayoutUpdated;",
        "runtime repair must defend the Room pane from later compact-layout passes",
    )

    presentation = method_body(source, "private void ApplyBlt3dRoomPanePresentation")
    for label in (
        "Bỏ",
        "Tạo hoàn thiện",
        "Thuộc tính",
        "Chưa chọn",
    ):
        require(presentation, label, "Room detail pane must expose owner-reference label: " + label)
    require(presentation, 'new Binding("SelectedFamilyName")', "Room detail header must track the selected Room")
    require(
        presentation,
        "createFinish.Click += OnAddFinishClick;",
        "Tạo hoàn thiện must keep the established finish action",
    )
    forbid(
        presentation,
        "finishTree.Items.Add",
        "Loaded/SystemIdle Room presentation must not mutate the finish TreeView after first layout",
    )

    require(
        room_finish_safety,
        "EnsureRoomFinishStaticItemsPreLayout(tree);",
        "Room finish static items must be materialized by the pre-layout safety path",
    )
    require(
        room_finish_safety,
        'Header = "Trát Trần"',
        "Trát Trần must remain in the complete pre-layout Room finish item set",
    )
    require(
        room_finish_safety,
        "Tag = ElementCategory.CeilingFinish.ToString()",
        "Trát Trần must retain the CeilingFinish semantic category",
    )

    for anchor in (
        'Grid Grid.Column="4"',
        'x:Name="SelectionCount"',
        'x:Name="InspectionList"',
        'Content="+ Thêm"',
        'Click="OnAddFinishClick"',
        'Header="Sàn Hoàn Thiện"',
        'Header="Chống Thấm"',
        'Header="Chân Tường"',
        'Header="Hoàn Thiện Tường"',
        'Header="Trần Hoàn Thiện"',
    ):
        require(xaml, anchor, "Room contract depends on the existing docked XAML surface: " + anchor)

    print(
        "PASS: Room + Add is direct, the room-pane + Thêm remains a finish action, "
        "the generic Family chooser is preserved elsewhere, and the docked Room/finish pane is runtime-defended."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
