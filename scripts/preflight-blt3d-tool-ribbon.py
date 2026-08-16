#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltToolRibbonAugmenter.cs"
BINDER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltToolRibbonCommandBinder.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "BltToolCommands.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
SLAB_OPENING = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawSlabOpeningCommands.cs"
REVIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReviewCommands.cs"


def require(text: str, needle: str, errors: list[str], label: str) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def require_order(text: str, needles: list[str], errors: list[str], label: str) -> None:
    positions = [text.find(needle) for needle in needles]
    if any(position < 0 for position in positions):
        errors.append(f"cannot validate {label}; at least one anchor is missing")
        return
    if positions != sorted(positions):
        errors.append(f"wrong {label}: {needles}")


def read_required(path: Path, errors: list[str]) -> str:
    if not path.is_file():
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def main() -> int:
    errors: list[str] = []
    ribbon = read_required(RIBBON, errors)
    binder = read_required(BINDER, errors)
    commands = read_required(COMMANDS, errors)
    coordinator = read_required(COORDINATOR, errors)
    slab_opening = read_required(SLAB_OPENING, errors)
    review = read_required(REVIEW, errors)
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    require(ribbon, 'private const string ToolTabId = "QS3D_TOOL";', errors, "TOOL ownership")
    require(ribbon, 'Create("Bricscad.Windows.RibbonTextBox")', errors, "pile embed text box")
    require(ribbon, 'SetProperty(embed, "Text", "Ngàm vào đài a (mm)")', errors, "pile embed label")
    require(ribbon, 'SetProperty(embed, "TextValue", "1000")', errors, "pile embed default")

    panel_anchors = [
        'BuildPilePanel()',
        'BuildFoundationPanel()',
        'BuildSlabPanel()',
        'BuildMcpPanel()',
        'BuildAutocadPanel()',
    ]
    require_order(ribbon, panel_anchors, errors, "BLT3D TOOL panel order")

    for title in ("Cọc", "Móng", "Sàn", "MCP (AI)", "AutoCAD"):
        require(ribbon, f'\"{title}\"', errors, f"panel title {title}")

    for caption in (
        "Hạ cọc xuống đáy đài",
        "Bê tông lót",
        "Đào hố móng",
        "Lỗ mở → Sàn",
        "Cài đặt MCP",
        "Tài liệu MCP",
        "Bảng điều khiển AI",
        "Kiểm tra kết nối",
        "CAD → BLT",
    ):
        require(ribbon, f'\"{caption}\"', errors, f"button caption {caption}")

    icon_kinds = (
        "PileDown",
        "LeanConcrete",
        "Excavation",
        "SlabOpening",
        "McpSettings",
        "McpDocs",
        "AiDashboard",
        "Connection",
        "CadToBlt",
    )
    for icon in icon_kinds:
        require(ribbon, f"IconKind.{icon}", errors, f"semantic vector icon {icon}")

    for legacy in ("INSPECT", "FOCUS", "VIEW", "QUALITY"):
        require(ribbon, f'"QS3D_TOOL_{legacy}_PANEL_SOURCE"', errors, f"legacy TOOL cleanup {legacy}")

    require(ribbon, 'SetProperty(button, "ShowImage", true);', errors, "visible button icons")
    require(ribbon, 'SetProperty(button, "Image", icon);', errors, "standard image assignment")
    require(ribbon, 'SetProperty(button, "LargeImage", icon);', errors, "large image assignment")

    expected_bindings = {
        'Prefix + "PILE_LOWER"': "QS3DBLTPILELOWER",
        'Prefix + "LEAN_CONCRETE"': "QS3DBLTLEANCONCRETE",
        'Prefix + "EXCAVATE_FOUNDATION"': "QS3DBLTFOUNDATIONEXCAVATE",
        'Prefix + "SLAB_OPENING"': "QS3DDRAWSLABOPEN",
        'Prefix + "MCP_SETTINGS"': "QS3DMCPSETTINGS",
        'Prefix + "MCP_DOCS"': "QS3DMCPDOCS",
        'Prefix + "AI_DASHBOARD"': "QS3DAIDASHBOARD",
        'Prefix + "MCP_CONNECTION"': "QS3DMCPCHECK",
        'Prefix + "CAD_TO_BLT"': "QS3DRECOGNIZE",
    }
    for button_id, command in expected_bindings.items():
        require(binder, f'[{button_id}] = "{command}"', errors, f"runtime binding {button_id}")

    require(binder, 'Prefix + "PILE_EMBED_MM"', errors, "pile embed textbox binding")
    require(binder, "BltToolRuntimeState.TrySetPileEmbedMillimeters", errors, "pile embed state update")

    local_commands = (
        "QS3DBLTPILELOWER",
        "QS3DBLTLEANCONCRETE",
        "QS3DBLTFOUNDATIONEXCAVATE",
        "QS3DMCPSETTINGS",
        "QS3DMCPDOCS",
        "QS3DAIDASHBOARD",
        "QS3DMCPCHECK",
    )
    for command in local_commands:
        require(commands, f'[CommandMethod("{command}"', errors, f"CommandMethod {command}")

    # Geometry buttons must do real guarded work, not route back to Workspace placeholders.
    require(commands, "EntitySnapshotReader.ReadCurrentSelection(document)", errors, "pile selection")
    require(commands, "CadUnitService.MetersToDrawingUnits", errors, "drawing-unit conversion")
    require(commands, "Matrix3d.Displacement(new Vector3d(0d, 0d, deltaZ))", errors, "pile Z translation")
    require(commands, "capExtents.MinPoint.Z + embedDrawing", errors, "pile-cap embed target")
    require(commands, "solid.CreateBox(width, depth, height);", errors, "generated native TOOL solids")
    require(commands, "TOOL 3D geometry chỉ tạo trong Model Space", errors, "Model Space mutation guard")
    require(commands, "không tự Boolean trừ địa hình", errors, "truthful excavation behavior")
    require(commands, "chưa tự gán semantic Family", errors, "truthful lean-concrete behavior")

    # MCP/AI buttons must expose real local configuration and transport diagnostics without
    # falsely claiming protocol health when only the socket path is reachable.
    require(commands, 'EnvironmentVariableName = "QS3D_MCP_ENDPOINT"', errors, "MCP environment configuration")
    require(commands, 'ConfigFileName = "mcp-endpoint.txt"', errors, "MCP saved configuration")
    require(commands, "new TcpClient()", errors, "MCP transport probe")
    require(commands, "không xác nhận MCP protocol/health", errors, "MCP health disclaimer")
    require(commands, "Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })", errors, "MCP docs launcher")
    require(commands, 'MessageBox.Show(text, "QS3D AI Dashboard"', errors, "AI dashboard")

    # Reused buttons are pinned to command implementations that already exist in product code.
    require(slab_opening, '[CommandMethod("QS3DDRAWSLABOPEN"', errors, "existing slab opening command")
    require(review, '[CommandMethod("QS3DRECOGNIZE"', errors, "existing CAD recognition command")

    require(coordinator, "BltToolRibbonAugmenter.TryInitialize()", errors, "TOOL augmenter initialization")
    require(coordinator, "BltToolRibbonCommandBinder.TryInitialize()", errors, "TOOL binding initialization")
    require(coordinator, "BltToolRibbonCommandBinder.Reset()", errors, "TOOL binding reset")
    require_order(
        coordinator,
        ["BltToolRibbonAugmenter.TryInitialize()", "BltToolRibbonCommandBinder.TryInitialize()", "RibbonCommandParameterFallback.TryInitialize()"],
        errors,
        "TOOL binding before command fallback",
    )

    if errors:
        print("BLT3D TOOL ribbon preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: BLT3D TOOL contract is pinned: 5 owner-reference panels, 9 icon-bearing actions, "
        "functional command bindings, guarded geometry mutations, real slab/recognition reuse, and truthful MCP diagnostics."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
