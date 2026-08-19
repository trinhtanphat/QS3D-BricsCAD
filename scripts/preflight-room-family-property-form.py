#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtype.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "ApplyRoomFamilyPropertyForm", "Room must own an explicit family property form")
    require(text, 'Group = "Information"', "Room Information group is missing")
    require(text, '"Loại cấu kiện"', "Room component-type row is missing")
    require(text, 'categoryRow.Value = "Phòng"', "Room type must be localized as Phòng")
    require(text, 'Name = "Tầng"', "Room active-floor row is missing")
    require(text, '"Cao độ đầu"', "Room top-level row is missing")
    require(text, '"Cao độ đáy"', "Room bottom-level row is missing")
    require(text, '"Màu sắc"', "Room color row is missing")
    require(text, '"Độ trong suốt"', "Room transparency row is missing")
    require(text, '"Mark"', "Room Mark metadata row is missing")
    require(text, '"Comment"', "Room Comment metadata row is missing")
    require(text, '"WBS"', "Room WBS metadata row is missing")
    require(text, '"Vật liệu"', "Room material row is missing")

    require(text, 'SeedRoomDefault(family, RoomTopLevelKey, "bottom_level")', "Room top-level default must match the approved UI")
    require(text, 'SeedRoomDefault(family, RoomBottomLevelKey, "bottom_level")', "Room bottom-level default must match the approved UI")
    require(text, 'SeedRoomDefault(family, RoomColorModeKey, "Theo loại (mặc định)")', "Room color default is missing")
    require(text, 'SeedRoomDefault(family, RoomTransparencyKey, "70")', "Room transparency default must be 70 percent")
    require(text, 'SeedRoomDefault(family, RoomMaterialKey, "Khác")', "Room material default must be Khác")

    require(text, "percent < 0d || percent > 100d", "Room transparency must be constrained to 0-100 percent")
    require(text, "ProjectFamilyService.SetProperty", "Room edits must use the canonical Family property service")
    require(text, "ExistingProjectMutationContext.Require", "Room edits must bind to the current project mutation context")
    require(text, "SeedRoomFamilyDefaults(family);", "new/duplicated Room families must receive the Room schema defaults")
    require(text, "FloorCombo.SelectionChanged += OnRoomFloorContextChanged", "Room floor display must refresh when the active floor changes")

    print("PASS: Room family property form contract is explicit, persisted through ProjectFamilyService, and matches the approved defaults.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
