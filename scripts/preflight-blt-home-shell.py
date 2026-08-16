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
    shell_rel = "src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs"
    command_rel = "src/QS3D.BricsCAD.V25/StartCenterCommands.cs"
    host_rel = "src/QS3D.BricsCAD.V25/StartCenterPaletteCoordinator.cs"
    project_ui_rel = "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"
    mutation_rel = "src/QS3D.BricsCAD.V25/ExistingProjectMutationContext.cs"
    context_rel = "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
    result_rel = "src/QS3D.BricsCAD.V25/UI/ProjectOperationResultWindow.cs"
    icon_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonIconFactory.cs"

    ribbon = read(ribbon_rel)
    for needle in (
        '"Dự án"', '"Mở..."', '"Lưu"', '"Lưu thành..."', '"Cấu hình"', '"Cài đặt"',
        '"Đối tượng\\nhệ thống"', 'ProjectFileUiService.OpenProjectFromPicker',
        'ProjectFileUiService.SaveCurrentProject', 'ProjectFileUiService.SaveCurrentProjectAs',
        'new ProjectToolsCommands().ShowProjectTools()', 'new FamilyManagerCommands().ShowFamilyManager()',
        'SetProperty(button, "ShowImage", true)', 'spec.Icon == RibbonIconKind.Qs3dLogo',
        'Qs3dBrandIconFactory.Create(16)', 'Qs3dBrandIconFactory.Create(32)',
        'RibbonIconFactory.Create(spec.Icon, 16)', 'RibbonIconFactory.Create(spec.Icon, 32)',
        'SetProperty(button, "Image", smallImage)', 'SetProperty(button, "LargeImage", largeImage)',
        'DirectActionHandler', 'FilePanelSourceId', 'LegacyProjectPanelSourceId', 'UpdatePanelSourceId',
        'ConfigPanelSourceId', 'RemoveOwnedPanel(panels, LegacyProjectPanelSourceId)',
        'RemoveOwnedPanel(panels, UpdatePanelSourceId)',
    ):
        require(ribbon, needle, ribbon_rel)
    for stale in ('SendStringToExecute', '"_OPEN"', '"_QSAVE"', '"_SAVEAS"', '"Tệp"',
                  '"Mở dự án..."', 'TỆP → HỆ THỐNG → CẤU HÌNH',
                  'if (updatePanel != null) Add(panels, updatePanel);'):
        forbid(ribbon, stale, ribbon_rel)

    project_ui = read(project_ui_rel)
    for needle in (
        'ProjectFilter = "QS3D Project (*.blt3d;*.qsdb)', 'DrawingFilter = "BricsCAD Drawing (*.dwg)|*.dwg"',
        'public static void CreateNewDrawing()', 'method.Name, "Add"', 'ProjectContextCoordinator.Forget(document);',
        '_ = ProjectContextCoordinator.GetOrCreate(document);', 'new OpenFileDialog', 'new SaveFileDialog',
        'InvokeAcadDocumentMethod(document, "Save")',
        'InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing)',
        'Path.ChangeExtension(targetDrawingPath, ".qsdb")', 'var savedProjectPath = ProjectContextCoordinator.Save(document);',
        'document = Application.DocumentManager.Open(drawingPath, false);',
        'Application.DocumentManager.MdiActiveDocument = document;', 'ProjectOperationResultWindow.ShowOpenSuccess',
        'ProjectOperationResultWindow.ShowSaveSuccess', 'ProjectOperationResultWindow.ShowSaveAsSuccess', 'QsdbProjectStore',
    ):
        require(project_ui, needle, project_ui_rel)
    save_guard = 'ExistingProjectMutationContext.Require(document, ExistingProjectMutationContext.SaveProjectOperation);'
    if project_ui.count(save_guard) != 2:
        raise SystemExit(
            f"FAIL: {project_ui_rel} must route both Save and Save As through the invariant SaveProjectOperation guard")
    collision_guard = 'File.Exists(targetProjectPath) || File.Exists(targetProjectPath + ".bak")'
    save_as_call = 'InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing)'
    require(project_ui, collision_guard, project_ui_rel)
    if project_ui.index(collision_guard) > project_ui.index(save_as_call):
        raise SystemExit(
            f"FAIL: {project_ui_rel} must reject occupied project sidecars before mutating the DWG path")
    for stale in ('SendStringToExecute', '"_OPEN"', '"_QSAVE"', '"_SAVEAS"',
                  'File.Copy(canonicalPath, targetPath', 'savedAsCopy',
                  'ExistingProjectMutationContext.Require(document, "Lưu dự án")',
                  'ExistingProjectMutationContext.Require(document, "Lưu thành")'):
        forbid(project_ui, stale, project_ui_rel)

    mutation = read(mutation_rel)
    for needle in ('internal const string SaveProjectOperation = "Save Project";',
                   'string.Equals(operation, SaveProjectOperation, StringComparison.Ordinal)',
                   'ProjectContextCoordinator.TryGetCached(document, out var cached)',
                   '_ = ProjectContextCoordinator.HasPendingChanges(document);', 'if (!TryGet(document, out var project))',
                   'thao tác này không tạo project mới.'):
        require(mutation, needle, mutation_rel)
    for stale in ('IsMouseFirstUnsavedProjectSave', 'string.Equals(operation, "Lưu dự án"',
                  'string.Equals(operation, "Lưu thành"'):
        forbid(mutation, stale, mutation_rel)

    context = read(context_rel)
    require(context, 'var project = ExistingProjectMutationContext.Require(document, "Save Project");', context_rel)
    require(context, 'EnsureBackingStoreUnchanged(document, project, true, "QS3D save");', context_rel)
    require(context, 'Store.SaveNew(project, path);', context_rel)
    require(context, 'if (target.HasAnyFile)', context_rel)
    save_block = context.split('public static string Save(Document document)', 1)[1].split('public static ProjectState Reload(Document document)', 1)[0]
    forbid(save_block, 'GetOrCreate(', context_rel + '::Save')

    result = read(result_rel)
    for needle in ('var summary = "Đã mở', '+ fileName +', 'project.Zones.Count', 'project.Elements.Count',
                   'readMilliseconds', 'totalMilliseconds', 'ShowSaveAsSuccess',
                   'DWG hiện hành đã chuyển sang đường dẫn mới.', 'Content = "OK"'):
        require(result, needle, result_rel)
    for stale in ('savedAsCopy', 'project hiện hành vẫn giữ liên kết với DWG đang mở'):
        forbid(result, stale, result_rel)

    icons = read(icon_rel)
    for needle in ('RenderTargetBitmap', 'RibbonIconKind.OpenProject', 'RibbonIconKind.Save',
                   'RibbonIconKind.SaveAs', 'RibbonIconKind.Settings'):
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
        'using QS3D.BricsCAD.V25.Ribbon;', 'Text = "BLT3D"', 'Source = Qs3dBrandIconFactory.Create(36)',
        'BIM Modeling & Quantity Application',
        'Text = "Giải pháp mô hình hóa thông tin công trình BIM 3D trực quan và tối ưu hóa bóc tách khối lượng."',
        'Text = "QUY TRÌNH NHANH"', 'CreateNewProjectIcon(30)', '"Tạo dự án mới"',
        '"Bắt đầu bản vẽ trắng sạch hoàn toàn"', 'ProjectFileUiService.CreateNewDrawing',
        'RibbonIconFactory.Create(RibbonIconKind.OpenProject, 30)', '"Mở tệp dự án..."',
        '"Chọn tệp tin .blt3d hiện có từ máy tính"', 'ProjectFileUiService.OpenProjectFromPicker',
        'RibbonIconFactory.Create(RibbonIconKind.Save, 26)', '"Ctrl+S"', 'ProjectFileUiService.SaveCurrentProject',
        'RibbonIconFactory.Create(RibbonIconKind.SaveAs, 26)', '"Tạo bản sao mới"',
        'ProjectFileUiService.SaveCurrentProjectAs', 'Text = "Phiên bản " + DisplayVersion() + " • BLT3D Team"',
        'Text = "DỰ ÁN GẦN ĐÂY"', 'Text = "Nhấp vào dự án để mở trực tiếp và bắt đầu làm việc"',
        'StartCenterPaletteCoordinator.Hide();', 'new Commands().ShowWorkspace();', 'new Commands().ShowQuantitySummary();',
        'private Button CreateActionCard(ImageSource icon,', 'content.Children.Add(new Image', 'Source = icon,',
        'private static ImageSource CreateNewProjectIcon(int pixelSize)', 'new DrawingImage(group)',
        'private static readonly ControlTemplate ClickSurfaceTemplate = CreateClickSurfaceTemplate();',
        'Template = ClickSurfaceTemplate,', 'private static ControlTemplate CreateClickSurfaceTemplate()',
        'new FrameworkElementFactory(typeof(Border))', 'root.SetValue(Border.BackgroundProperty, Brushes.Transparent);',
        'new FrameworkElementFactory(typeof(ContentPresenter))',
        'presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");',
        'return new ControlTemplate(typeof(Button))', 'VisualTree = root',
        'button.MouseEnter += (_, __) => frame.Background = PanelHoverBrush;',
        'button.MouseLeave += (_, __) => frame.Background = PanelBrush;',
        'private static Button CreateClickSurface(UIElement content, Cursor cursor)',
        'button.Click += (_, __) => RunUiAction(action);', 'button.Click += (_, __) => OpenRecentProject(recent);',
        'Application.DocumentManager.Open(normalized, false)', 'StatusItem("○ Nền sáng")',
        'StatusItem("◐ Tương phản")', 'StatusItem("⌞ Vuông góc")', 'StatusItem("⌖ Bắt điểm", highlighted: true)',
        'StartCenterUserStateStore.GetSnapshot().RecentProjects',
        'RibbonIconFactory.Create(RibbonIconKind.OpenProject, 20)',
    ):
        require(shell, needle, shell_rel)
    for stale in ('SendStringToExecute', '"_.OPEN', '"_.NEW', '"_.QSAVE', '"_.SAVEAS',
                  'border.MouseLeftButtonUp', 'border.MouseLeftButtonDown', 'FocusVisualStyle = null',
                  'Focusable = false', 'Text = "Nhấp đúp vào dự án để mở trực tiếp và bắt đầu làm việc"',
                  ' : Window', 'ShowModelessWindow', 'using QS3D.BricsCAD.V25.Updates;', 'UpdateCenterWindowHost.Show()',
                  'CreateActionCard("↻", "Cập nhật"', 'CreateActionCard("＋"', 'CreateActionCard("▱"',
                  'CreateActionCard("▣"', 'CreateActionCard("▤"', 'var brandGlyph = new TextBlock', 'Text = "✦"',
                  '"Chọn tệp BLT3D/QS3D hiện có từ máy tính"', '"Lưu project QS3D"', '"Tạo bản sao BLT3D"',
                  'khối lượng trong BricsCAD.'):
        forbid(shell, stale, shell_rel)

    command = read(command_rel)
    require(command, "StartCenterPaletteCoordinator.Show();", command_rel)
    forbid(command, "Application.ShowModelessWindow", command_rel)
    forbid(command, "new BltStartCenterWindow", command_rel)
    forbid(command, "new StartCenterWindow", command_rel)

    host = read(host_rel)
    require(host, 'new PaletteSet("BLT3D — Khởi đầu"', host_rel)
    require(host, '_palette.AddVisual("Khởi đầu", _panel, true);', host_rel)
    require(host, 'Dock = DockSides.Left', host_rel)

    print("PASS: QS3D KHỞI ĐẦU matches the BLT3D reference surface with Dự án + Cấu hình ribbon groups, semantic vector icons, and exactly four embedded quick actions, while preserving branded/rasterized Ribbon icons, native WPF Button.Click and keyboard/focus semantics, responsive embedded PaletteSet layout, project/sidecar safety, truthful dynamic versioning, recent projects, and bottom status routing.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
