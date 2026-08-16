from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterCommands.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterPaletteCoordinator.cs"
PANEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterPanel.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: embedded Start Center {label} contract missing: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: embedded Start Center {label} contract forbids: {needle}")


def main() -> int:
    commands = COMMANDS.read_text(encoding="utf-8")
    host = HOST.read_text(encoding="utf-8")
    panel = PANEL.read_text(encoding="utf-8")
    plugin = PLUGIN.read_text(encoding="utf-8")

    require(commands, 'CommandMethod("QS3DSTART"', "command")
    require(commands, "StartCenterPaletteCoordinator.Show();", "command")
    forbid(commands, "ShowModelessWindow", "command")
    forbid(commands, "new BltStartCenterWindow", "command")
    forbid(commands, "new StartCenterWindow", "command")

    require(host, "new PaletteSet(\"BLT3D — Khởi đầu\"", "host")
    require(host, "Dock = DockSides.Left", "host")
    require(host, '_palette.AddVisual("Khởi đầu", _panel, true);', "host")
    require(host, "Application.DocumentManager.DocumentActivated += OnDocumentActivated;", "host")

    require(panel, "internal sealed class BltStartCenterPanel : UserControl", "panel")
    require(panel, 'Text = "BLT3D"', "panel")
    require(panel, 'Text = "DỰ ÁN GẦN ĐÂY"', "panel")
    require(panel, '"Tạo dự án mới"', "panel")
    require(panel, '"Mở tệp dự án..."', "panel")
    forbid(panel, " : Window", "panel")
    forbid(panel, "ShowModelessWindow", "panel")

    require(plugin, "TryCleanup(StartCenterPaletteCoordinator.Dispose);", "lifecycle")

    print(
        "PASS: QS3DSTART opens the BLT3D Start Center as a docked BricsCAD PaletteSet, "
        "keeps document refresh in-host, and no command path creates a top-level Start Center window."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
