#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml"
SETUP = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltProjectSetup.cs"
ROUTING = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltButtonRouting.cs"


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


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"{label}: forbidden source contract is still present: {token}")


def main() -> int:
    xaml = read(XAML)
    setup = read(SETUP)
    routing = read(ROUTING)

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

    # BLT3D reference parity: the Project Info and Project Properties entries stay inside the
    # same bounded Project Setup shell and display the reference placeholder. They must not
    # spawn a second modeless Project Tools/Properties surface from the top-nav route.
    for token, label in (
        ('case "Thông tin dự án":', "Project Info routed action"),
        ('case "Cài đặt tầng":', "Floor Settings routed action"),
        ('case "Thuộc tính dự án":', "Project Properties routed action"),
        ('window.ShowBltProjectPlaceholder("Thông tin dự án")', "Project Info placeholder route"),
        ('window.ShowBltFloorSettingsSurface()', "Floor Settings in-window route"),
        ('window.ShowBltProjectPlaceholder("Thuộc tính dự án")', "Project Properties placeholder route"),
        ('e.Handled = true;', "legacy modeless route suppression"),
        ('(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)', "BLT3D placeholder text"),
        ('Grid.SetRow(surface, 1);', "placeholder workspace row"),
        ('Panel.SetZIndex(surface, 100);', "placeholder overlay ownership"),
        ('ApplyBltProjectNavSelection', "top-nav active-state parity"),
        ('OpenDedicatedBltProjectProperties()', "canonical Project Properties compatibility method"),
        ('ShowBltProjectPlaceholder("Thuộc tính dự án")', "canonical Project Properties placeholder fallback"),
    ):
        require(routing, token, label)

    forbid(
        routing,
        '_document.SendStringToExecute("QS3DPROJECTPROPERTIES "',
        "Project Properties top-nav must not launch a second modeless surface",
    )

    # The legacy instance handlers remain source-compatible, but the class route above owns all
    # visible top-nav actions before those handlers can open duplicate windows.
    require(setup, 'OnBltProjectInfoClick(object sender, RoutedEventArgs e)', "Project Info legacy handler remains available")
    require(setup, 'OnBltFloorSettingsClick', "Floor Settings legacy handler remains available")
    require(setup, 'OnBltProjectPropertiesClick(object sender, RoutedEventArgs e) => OpenDedicatedBltProjectProperties()', "Project Properties canonical handler")
    require(setup, 'RefreshBltSetup();', "Floor Settings refresh")

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

    print("PASS: BLT3D Project Setup top-nav stays in one bounded surface and all floor/zone actions remain production-backed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
