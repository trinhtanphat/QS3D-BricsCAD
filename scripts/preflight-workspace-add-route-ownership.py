#!/usr/bin/env python3
"""Guard final ownership of the shared Workspace Family + Add surface.

The shared button is reconfigured by Grid, BLT3D and Room workspace passes.  A
route left attached by an earlier pass can mark Click handled and prevent the
current subtype owner from seeing it.  This guard keeps the final hand-off
explicit for Grid, Room and Móng đơn before the licensed UI matrix exercises
the actual WPF route.
"""

from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
GRID = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.GridFamilySubtype.cs"
BLT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs"
ROOM = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RoomWorkspacePane.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def method_body(text: str, marker: str) -> str:
    start = text.find(marker)
    if start < 0:
        raise AssertionError(f"missing method: {marker}")
    brace = text.find("{", start)
    if brace < 0:
        raise AssertionError(f"missing method body: {marker}")
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise AssertionError(f"unterminated method: {marker}")


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise AssertionError(message + f" (missing: {needle})")


def require_before(text: str, first: str, second: str, message: str) -> None:
    first_at = text.find(first)
    second_at = text.find(second)
    if first_at < 0 or second_at < 0 or first_at >= second_at:
        raise AssertionError(message)


def main() -> int:
    grid = read(GRID)
    blt = read(BLT)
    room = read(ROOM)

    grid_add = method_body(grid, "private void OnGridAwareFamilyAddModeClick")
    require(
        grid_add,
        "CreateGridFamilyFromWorkspaceSubtype(false);",
        "Grid direct Family creation contract disappeared",
    )

    blt_rewire = method_body(blt, "private void RewireBlt3dFamilyAddActions")
    require_before(
        blt_rewire,
        "button.Click -= OnGridAwareFamilyAddModeClick;",
        "button.Click += OnBlt3dFamilyAddClick;",
        "BLT3D final Add route must detach the earlier Grid button handler before attaching its owner",
    )
    require_before(
        blt_rewire,
        "item.Click -= OnGridAwareFamilyAddModeClick;",
        "item.Click += OnBlt3dFamilyAddClick;",
        "BLT3D final Add route must detach the earlier Grid menu handler before attaching its owner",
    )

    blt_add = method_body(blt, "private void OnBlt3dFamilyAddClick")
    require_before(
        blt_add,
        "if (IsSingleFootingSelected())",
        "ShowBlt3dFamilyModeChooser();",
        "Móng đơn dimensions route must win before the generic Family chooser",
    )
    require(blt_add, "HandleSingleFootingAdd(e);", "Móng đơn dimensions route disappeared")

    room_rewire = method_body(room, "private void RewireBlt3dRoomAwareAddActions")
    require_before(
        room_rewire,
        "button.Click -= OnGridAwareFamilyAddModeClick;",
        "button.Click += OnBlt3dRoomAwareAddClick;",
        "Room final Add route must detach the earlier Grid button handler before attaching its owner",
    )
    require_before(
        room_rewire,
        "item.Click -= OnGridAwareFamilyAddModeClick;",
        "item.Click += OnBlt3dRoomAwareAddClick;",
        "Room final Add route must detach the earlier Grid menu handler before attaching its owner",
    )

    room_add = method_body(room, "private void OnBlt3dRoomAwareAddClick")
    require_before(
        room_add,
        "if (IsGridSubtype(_familySubtypeFilter))",
        "OnBlt3dFamilyAddClick(sender, e);",
        "final Room route must preserve Grid direct creation before delegating non-Room subtypes",
    )
    require(room_add, "CreateGridFamilyFromWorkspaceSubtype(false);", "final Room route lost Grid direct creation")
    require(room_add, "CreateRoomFromWorkspace();", "Room direct creation route disappeared")

    print("PASS: shared Workspace + Add has one final owner per Grid, Room, and SingleFooting route")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as error:
        print("ERROR:", error, file=sys.stderr)
        raise SystemExit(1)
