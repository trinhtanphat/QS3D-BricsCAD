#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden stale contract: {needle}")


def main():
    ribbon_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltHomeRibbonAugmenter.cs"
    activation_rel = "src/QS3D.BricsCAD.V25/Ribbon/HomeTabActivationCoordinator.cs"
    init_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
    shell_rel = "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
    command_rel = "src/QS3D.BricsCAD.V25/StartCenterCommands.cs"
    project_ui_rel = "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"
    result_rel = "src/QS3D.BricsCAD.V25/UI/ProjectOperationResultWindow.cs"
    icon_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonIconFactory.cs"

    ribbon = read(ribbon_rel)
    for needle in (
        '"Tệp"',
        '"Mở dự án..."',
        '"Lưu"',
        '"Lưu thành..."',
        '"Cấu hình"',
        '"Cài đặt"',
        '"Đối tượng\\nhệ thống"',
        'ProjectFileUiService.OpenProjectFromPicker',
        'ProjectFileUiService.SaveCurrentProject',
        'ProjectFileUiService.SaveCurrentProjectAs',
        'new ProjectToolsCommands().ShowProjectTools()',
        'new FamilyManagerCommands().ShowFamilyManager()',
        'SetProperty(button, "ShowImage", true)',
        'SetProperty(button, "Image", RibbonIconFactory.Create',
        'SetProperty(button, "LargeImage", RibbonIconFactory.Create',
        'DirectActionHandler',
        'FilePanelSourceId',
        'LegacyProjectPanelSourceId',
        'ConfigPanelSourceId',
        'RemoveOwnedPanel(panels, LegacyProjectPanelSourceId)',
    ):
        require(ribbon, needle, ribbon_rel)
    for stale in ('SendStringToExecute', '"_OPEN"', '"_QSAVE"', '"_SAVEAS"'):
        forbid(ribbon, stale, ribbon_rel)

    project_ui = read(project_ui_rel)
    for needle in (
        'ProjectFilter = "QS3D Project (*.blt3d;*.qsdb)',
        'public static void CreateNewDrawing()',
        'method.Name, "Add"',
        'new OpenFileDialog',
        'new SaveFileDialog',
        'Application.DocumentManager.Open(drawingPath, false)',
        'ProjectOperationResultWindow.ShowOpenSuccess',
        'ProjectOperationResultWindow.ShowSaveSuccess',
        'QsdbProjectStore',
    ):
        require(project_ui, needle, project_ui_rel)
    for stale in ('SendStringToExecute', '"_OPEN"', '"_QSAVE"', '"_SAVEAS"'):
        forbid(project_ui, stale, project_ui_rel)

    result = read(result_rel)
    for needle in ('var summary = "Đã mở', '+ fileName +', 'project.Zones.Count', 'project.Elements.Count', 'readMilliseconds', 'totalMilliseconds', 'Content = "OK"'):
        require(result, needle, result_rel)

    icons = read(icon_rel)
    for needle in ('RenderTargetBitmap', 'RibbonIconKind.OpenProject', 'RibbonIconKind.Save', 'RibbonIconKind.SaveAs', 'RibbonIconKind.Settings'):
        require(icons, needle, icon_rel)

    activation = read(activation_rel)
    for needle in ('HomeTabId = "QS3D_HOME"', 'new StartCenterCommands().ShowStartCenter()', 'DispatcherTimer'):
        require(activation, needle, activation_rel)
    forbid(activation, 'SendStringToExecute', activation_rel)
    forbid(activation, '"QS3DSTART "', activation_rel)

    init = read(init_rel)
    require(init, "BltHomeRibbonAugmenter.TryInitialize()", init_rel)
    require(init, "HomeTabActivationCoordinator.TryInitialize()", init_rel)
    require(init, "HomeTabActivationCoordinator.Stop()", init_rel)

    shell = read(shell_rel)
    for needle in (
        'Text = "QS3D"',
        'BIM Modeling & Quantity Application',
        'Text = "QUY TRÌNH NHANH"',
        '"Tạo dự án mới"',
        'ProjectFileUiService.CreateNewDrawing',
        '"Mở tệp dự án..."',
        '"Chọn tệp BLT3D/QS3D hiện có từ máy tính"',
        'ProjectFileUiService.OpenProjectFromPicker',
        'ProjectFileUiService.SaveCurrentProject',
        'ProjectFileUiService.SaveCurrentProjectAs',
        'Text = "DỰ ÁN GẦN ĐÂY"',
        'StatusButton("Mô hình", () => new Commands().ShowWorkspace())',
        'StatusButton("BQ", () => new Commands().ShowQuantitySummary())',
        'Application.DocumentManager.Open(normalized, false)',
        'StatusItem("○ Nền sáng")',
        'StatusItem("◐ Tương phản")',
        'StatusItem("⌞ Vuông góc")',
        'StatusItem("⌖ Bắt điểm", highlighted: true)',
        'StartCenterUserStateStore.GetSnapshot().RecentProjects',
    ):
        require(shell, needle, shell_rel)
    for stale in ('SendStringToExecute', '"_.OPEN', '"_.NEW', '"_.QSAVE', '"_.SAVEAS'):
        forbid(shell, stale, shell_rel)

    command = read(command_rel)
    require(command, "createdWindow = new BltStartCenterWindow();", command_rel)

    print("PASS: QS3D Home and Start Center use unique panels, rasterized icons and direct mouse-first project actions without host command dispatch.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
