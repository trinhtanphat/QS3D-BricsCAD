#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ribbon = (ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs").read_text(encoding="utf-8")
commands = (ROOT / "src/QS3D.BricsCAD.V25/ReferenceUiCommands.cs").read_text(encoding="utf-8")
tree = (ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs").read_text(encoding="utf-8")
registration = (ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs").read_text(encoding="utf-8")

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
for label, command in required_ribbon.items():
    if label not in ribbon or command not in ribbon:
        errors.append(f"missing Ribbon mapping: {label} -> {command}")
    if command not in commands and command not in {"QS3DDRAWCIRCLE"}:
        errors.append(f"missing adapter command implementation: {command}")

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
