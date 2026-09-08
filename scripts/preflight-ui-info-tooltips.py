from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UiInfoTooltipBootstrap.cs"


def fail(message: str) -> int:
    print(f"ERROR: UI info tooltip preflight failed: {message}")
    return 1


def main() -> int:
    if not SOURCE.is_file():
        return fail(f"missing {SOURCE.relative_to(ROOT)}")

    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "ModuleInitializer",
        "EventManager.RegisterClassHandler",
        "typeof(Window)",
        '"McpAgentControlCenter"',
        '"UpdateCenterWindow"',
        "ButtonBase.ClickEvent",
        "Dispatcher.BeginInvoke",
        "CreateInfoButton",
        "ToolTip",
        "ToolTipService.InitialShowDelayProperty",
        "ToolTipService.ShowDurationProperty",
        "AutomationProperties.NameProperty",
        "AutomationProperties.HelpTextProperty",
        "GotKeyboardFocus",
        "LostKeyboardFocus",
        "IsOpen = true",
        "IsOpen = false",
        "VisualTreeHelper",
        '"Kết nối, Agent desktop, backup/recovery"',
        '"Chọn một transport."',
        '"Chỉ hiển thị trạng thái thuộc transport"',
        '"Runtime API key ·"',
        '"Secure Tunnel: ChatGPT chọn Connection = Tunnel"',
        '"Quick Tunnel có hostname thay đổi"',
        '"Tiếp theo: "',
        '"DLL đang chạy:"',
        "IsUpdateDetail",
        "LineHeight",
        '"Mặc định tắt:"',
        '"Bật: bản tải cài đặt"',
        '"Consolas"',
    ]
    missing = [token for token in required if token not in text]
    if missing:
        return fail("missing production tokens: " + ", ".join(missing))

    forbidden = [
        "McpTransportCoordinator.Select",
        "UpdateCoordinator.Instance.RefreshAsync",
        "Process.Start",
        "File.Copy",
    ]
    present = [token for token in forbidden if token in text]
    if present:
        return fail("UI-only helper contains forbidden behavior tokens: " + ", ".join(present))

    print("PASS: Agent Center and Update Center verbose explanatory copy is compacted behind keyboard-focusable hover info tooltips without transport/update mutations.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
