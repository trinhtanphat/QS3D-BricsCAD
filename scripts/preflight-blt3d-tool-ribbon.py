#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TOOL = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltToolRibbonAugmenter.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"


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


def main() -> int:
    errors: list[str] = []
    if not TOOL.is_file():
        print(f"ERROR: missing {TOOL.relative_to(ROOT)}")
        return 1
    if not COORDINATOR.is_file():
        print(f"ERROR: missing {COORDINATOR.relative_to(ROOT)}")
        return 1

    tool = TOOL.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")

    require(tool, 'private const string ToolTabId = "QS3D_TOOL";', errors, "TOOL ownership")
    require(tool, 'Create("Bricscad.Windows.RibbonTextBox")', errors, "pile embed text box")
    require(tool, 'SetProperty(embed, "Text", "Ngàm vào đài a (mm)")', errors, "pile embed label")
    require(tool, 'SetProperty(embed, "TextValue", "1000")', errors, "pile embed default")

    panel_anchors = [
        'BuildPilePanel()',
        'BuildFoundationPanel()',
        'BuildSlabPanel()',
        'BuildMcpPanel()',
        'BuildAutocadPanel()',
    ]
    require_order(tool, panel_anchors, errors, "BLT3D TOOL panel order")

    for title in ("Cọc", "Móng", "Sàn", "MCP (AI)", "AutoCAD"):
        require(tool, f'\"{title}\"', errors, f"panel title {title}")

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
        require(tool, f'\"{caption}\"', errors, f"button caption {caption}")

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
        require(tool, f"IconKind.{icon}", errors, f"semantic vector icon {icon}")

    for legacy in ("INSPECT", "FOCUS", "VIEW", "QUALITY"):
        require(tool, f'"QS3D_TOOL_{legacy}_PANEL_SOURCE"', errors, f"legacy TOOL cleanup {legacy}")

    require(tool, 'SetProperty(button, "ShowImage", true);', errors, "visible button icons")
    require(tool, 'SetProperty(button, "Image", icon);', errors, "standard image assignment")
    require(tool, 'SetProperty(button, "LargeImage", icon);', errors, "large image assignment")

    require(coordinator, "BltToolRibbonAugmenter.TryInitialize()", errors, "TOOL augmenter initialization")
    require(coordinator, "BltToolRibbonAugmenter.Reset()", errors, "TOOL augmenter reset")

    if errors:
        print("BLT3D TOOL ribbon preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: BLT3D TOOL topbar contract is pinned: 5 panels, 9 icon-bearing actions, pile embed input, and coordinator wiring.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
