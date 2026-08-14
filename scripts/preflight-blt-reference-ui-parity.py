#!/usr/bin/env python3
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ribbon = (ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs").read_text(encoding="utf-8")
bootstrap = (ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs").read_text(encoding="utf-8")
commands = (ROOT / "src/QS3D.BricsCAD.V25/ReferenceUiCommands.cs").read_text(encoding="utf-8")
project_tools = (ROOT / "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs").read_text(encoding="utf-8")
tree = (ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs").read_text(encoding="utf-8")
registration = (ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs").read_text(encoding="utf-8")
command_doc = (ROOT / "docs/COMMANDS.md").read_text(encoding="utf-8")

required_home = {
    "Mở…": "_.OPEN",
    "Lưu bản vẽ": "_.QSAVE",
    "Lưu thành…": "_.SAVEAS",
    "Cài đặt": "QS3DPROJECTTOOLS",
}

required_primary_tabs = [
    ("QS3D_HOME", "KHỞI ĐẦU"),
    ("QS3D_PROJECT", "THIẾT LẬP DỰ ÁN"),
    ("QS3D_BIM", "MÔ HÌNH BIM"),
    ("QS3D_RECOGNIZE", "NHẬN DẠNG"),
    ("QS3D_DRAW", "VẼ"),
    ("QS3D_TOOL", "TOOL"),
    ("QS3D_MODELING", "MODELING"),
    ("QS3D_VIEW", "XEM"),
    ("QS3D_QTY", "ĐỊNH LƯỢNG"),
    ("QS3D_REV", "BẢN SỬA ĐỔI"),
]

required_ribbon = {
    "Theo nét CAD": "QS3DDRAWBYCAD",
    "Đường tròn": "QS3DDRAWCIRCLE",
    "Biên dạng": "QS3DDRAWPROFILE",
    "Dốc sàn": "QS3DFLOORSLOPE",
    "Cắt sàn": "QS3DSLABCUT",
    "Nối góc": "QS3DJOINCORNER",
    "Nối chữ T": "QS3DJOINTEE",
    "Nhập IFC": "QS3DIFCIMPORT",
    "Nhập IFC (nhẹ)": "QS3DIFCIMPORTLIGHT",
    "Xóa IFC": "QS3DIFCREMOVE",
    "Xuất IFC": "QS3DIFCEXPORT",
}

errors = []
for label, command in required_home.items():
    if label not in ribbon or command not in ribbon:
        errors.append(f"missing Home Ribbon mapping: {label} -> {command}")

for tab_id, title in required_primary_tabs:
    pattern = rf'new\s+RibbonTabSpec\(\s*"{re.escape(tab_id)}"\s*,\s*"{re.escape(title)}"\s*,'
    matches = re.findall(pattern, bootstrap)
    if len(matches) != 1:
        errors.append(
            f"BLT primary Ribbon tab must exist exactly once with stable id/title: {tab_id} -> {title} (found {len(matches)})"
        )

for token in [
    'private const string HomeTabId = "QS3D_HOME";',
    'private const string HomeFilePanelSourceId = "QS3D_HOME_FILE_PANEL_SOURCE";',
    'var homeTab = FindById(tabItems, HomeTabId);',
    'CreatePanel(homePanels, HomeFilePanelSourceId, "Tệp")',
]:
    if token not in ribbon:
        errors.append("Home file/settings parity must augment the existing QS3D Home tab idempotently: " + token)

if "QS3DSAVE" not in bootstrap:
    errors.append("native drawing Save parity must not replace existing QS3DSAVE semantic-project persistence")
if '[CommandMethod("QS3DPROJECTTOOLS", CommandFlags.Modal)]' not in project_tools:
    errors.append("Home Cài đặt must remain backed by the existing QS3DPROJECTTOOLS configuration surface")

for label, command in required_ribbon.items():
    if label not in ribbon or command not in ribbon:
        errors.append(f"missing Ribbon mapping: {label} -> {command}")
    if command not in commands and command not in {"QS3DDRAWCIRCLE"}:
        errors.append(f"missing adapter command implementation: {command}")
    if f"`{command}`" not in command_doc:
        errors.append(f"missing command reference documentation: {command}")

for command in ["QS3DDRAWLINE", "QS3DDRAWRECT", "QS3DDRAWCIRCLE"]:
    if f"`{command}`" not in command_doc:
        errors.append(f"missing context-aware basic drawing documentation: {command}")

for label in [
    "Lưới Thẳng", "Lưới Cong", "Dầm HCN", "Giằng Tường", "Lanh Tô",
    "Sàn Đặc", "Đường Dốc", "Lỗ Mở Sàn", "Mái Hắt Diện Tích",
    "Mái Hắt Biên Dạng", "Cọc", "Đài Cọc", "Dầm Móng", "Móng Băng",
    "Móng Bè", "Bê Tông Lót", "Khối giao đào", "Khối đất sau trừ",
    "KL Chiều dài", "KL Diện tích", "KL Thể tích", "KL Biên dạng",
    "KL Mặt phẳng", "Modeling",
]:
    if label not in tree:
        errors.append(f"missing Workspace tree label: {label}")

if "ReferenceWorkspaceTreeAugmenter.EnsureRegistered()" not in registration:
    errors.append("Workspace reference tree augmenter is orphaned: no WorkspacePanel type-initialization registration call")
if "static readonly bool ReferenceWorkspaceTreeRegistrationReady" not in registration:
    errors.append("Workspace reference tree registration must remain a type initializer, not an instance/load side effect")
if "EventManager.RegisterClassHandler" not in tree or "typeof(WorkspacePanel)" not in tree:
    errors.append("Workspace reference tree augmenter must remain a WorkspacePanel Loaded class handler")

for forbidden in ["RibbonInitializationCoordinator.cs", "PaletteCoordinator.cs"]:
    if forbidden in ribbon:
        errors.append(f"UI parity augmenter must not own startup lifecycle surface: {forbidden}")

if errors:
    print("BLT-reference UI parity preflight FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)

print("BLT-reference UI parity preflight PASSED")
