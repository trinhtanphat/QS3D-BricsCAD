#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml"
SETUP = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltProjectSetup.cs"
ROUTING = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltButtonRouting.cs"
PROPERTIES_COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectPropertiesCommands.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def read(path: Path) -> str:
    if not path.is_file():
        fail(f"missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: expected source contract not found: {token}")


def main() -> int:
    xaml = read(XAML)
    setup = read(SETUP)
    routing = read(ROUTING)
    properties_command = read(PROPERTIES_COMMAND)

    # Every visible Project Setup button in the supplied BLT3D reference must retain an
    # explicit production action rather than becoming decorative while visual parity evolves.
    xaml_routes = (
        ('Click="OnBltProjectInfoClick"', "Thông tin dự án"),
        ('Click="OnBltFloorSettingsClick"', "Cài đặt tầng"),
        ('Click="OnBltProjectPropertiesClick"', "Thuộc tính dự án canonical click route"),
        ('Click="OnBltAddZoneClick"', "Thêm vùng"),
        ('Click="OnBltDeleteZoneClick"', "Xóa vùng"),
        ('Click="OnBltInsertFloorClick"', "Chèn sàn"),
        ('Click="OnBltInsertFloorBelowClick"', "Chèn sàn xuống dưới"),
        ('Click="OnBltDeleteFloorClick"', "Xóa sàn"),
        ('Click="OnBltApplyChangesClick"', "Áp dụng thay đổi"),
        ('Click="OnBltReferenceClick"', "Tầng tham chiếu"),
    )
    for token, label in xaml_routes:
        require(xaml, token, f"XAML action wiring for {label}")

    # Project Info stays on the read-only Project Tools surface and Floor Settings refreshes
    # the current bounded surface rather than opening a duplicate window.
    require(setup, 'OnBltProjectInfoClick(object sender, RoutedEventArgs e) => OpenProjectTools("Thông tin dự án")', "Project Info action")
    require(setup, 'var window = new ProjectToolsWindow(_document);', "Project Info Project Tools route")
    require(setup, 'OnBltFloorSettingsClick', "Floor Settings action")
    require(setup, 'RefreshBltSetup();', "Floor Settings refresh")

    # Project Properties must route correctly from the canonical XAML handler itself. The
    # early Button class route remains as a compatibility/safety layer, not as the only thing
    # preventing Project Properties from falling through to Project Tools.
    canonical_properties_handler = 'OnBltProjectPropertiesClick(object sender, RoutedEventArgs e) => OpenDedicatedBltProjectProperties()'
    legacy_properties_handler = 'OnBltProjectPropertiesClick(object sender, RoutedEventArgs e) => OpenProjectTools("Thuộc tính dự án")'
    require(setup, canonical_properties_handler, "Project Properties canonical click action")
    if legacy_properties_handler in setup:
        fail("Project Properties canonical click handler must not alias the Project Info/Project Tools route")
    require(routing, 'typeof(Button)', "Project Properties early Button class handler")
    require(routing, 'e.Handled = true;', "Project Properties compatibility route suppression")
    require(routing, '"Thuộc tính dự án"', "Project Properties label match")
    require(routing, '_document.SendStringToExecute("QS3DPROJECTPROPERTIES "', "dedicated Project Properties action")
    require(properties_command, '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]', "dedicated Project Properties command")
    require(properties_command, 'var window = new ProjectPropertiesWindow();', "dedicated Project Properties surface")

    # Zone toolbar actions must mutate real ProjectState through the canonical service and keep
    # active-zone semantics consistent when selection/deletion changes.
    for token, label in (
        ('ProjectZoneService.Create(project,', "Add Zone service mutation"),
        ('ProjectZoneService.Delete(project, zone.Id)', "Delete Zone service mutation"),
        ('ProjectZoneService.SetActive(project, selected.Id)', "Zone selection activates real zone"),
        ('ProjectStateSnapshot.Capture(project)', "Zone/floor rollback boundary"),
    ):
        require(setup, token, label)

    # Floor toolbar actions must use canonical floor services, preserve metadata, and validate
    # editable grid data before applying it back to ProjectState.
    for token, label in (
        ('OnBltInsertFloorClick(object sender, RoutedEventArgs e) => InsertBltFloor(false)', "Insert Floor action"),
        ('OnBltInsertFloorBelowClick(object sender, RoutedEventArgs e) => InsertBltFloor(true)', "Insert Floor Below action"),
        ('ProjectFloorService.Create(project,', "Floor create service mutation"),
        ('ProjectFloorService.Delete(project, floor.Id)', "Floor delete service mutation"),
        ('RemoveFloorMetadata(project, floor.Id)', "Floor metadata cleanup"),
        ('ParseNonNegative(row.HeightText, "Chiều cao sàn")', "floor height validation"),
        ('ParseFinite(row.ElevationText, "Độ cao đáy")', "floor elevation validation"),
        ('ParseTypicalCount(row.TypicalCountText)', "typical-count validation"),
        ('ProjectFloorService.Update(project, floor.Id, item.Name, item.Elevation)', "Apply Changes floor update"),
        ('ProjectFloorService.SetActive(project, reference.Row.Id)', "Apply Changes reference-floor activation"),
    ):
        require(setup, token, label)

    # The visible reference checkbox remains radio-like even though BLT3D renders a CheckBox.
    require(setup, 'checkBox.IsChecked != true', "reference click only promotes checked row")

    print("PASS: all visible BLT3D Project Setup buttons retain distinct, production-backed actions.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
