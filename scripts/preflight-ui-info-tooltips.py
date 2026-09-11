from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UiInfoTooltipBootstrap.cs"
AGENT_CENTER_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpAgentControlCenter.cs"


def fail(message: str) -> int:
    print(f"ERROR: UI info tooltip preflight failed: {message}")
    return 1


def main() -> int:
    if not SOURCE.is_file():
        return fail(f"missing {SOURCE.relative_to(ROOT)}")

    agent_center_text = AGENT_CENTER_SOURCE.read_text(encoding="utf-8")
    if "internal sealed class McpAgentControlCenterWindow : Window" not in agent_center_text:
        return fail("Agent Center concrete window type changed; update tooltip target and WPF regression together")

    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "internal static void EnsureRegistered()",
        "Interlocked.CompareExchange(ref _registered, 1, 0)",
        "Interlocked.Exchange(ref _registered, 0)",
        "EventManager.RegisterClassHandler",
        "typeof(Window)",
        '"McpAgentControlCenterWindow"',
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
        "ModuleInitializer",
        "McpTransportCoordinator.Select",
        "UpdateCoordinator.Instance.RefreshAsync",
        "Process.Start",
        "File.Copy",
    ]
    present = [token for token in forbidden if token in text]
    if present:
        return fail("UI-only helper contains forbidden behavior tokens: " + ", ".join(present))

    polish = (SOURCE.parent / "UI" / "ProductionUiPolish.cs").read_text(encoding="utf-8")
    hook = polish.find("UiInfoTooltipBootstrap.EnsureRegistered();")
    latch = polish.find("Interlocked.CompareExchange(ref _registered, 1, 0)")
    if hook < 0 or latch < 0 or hook > latch:
        return fail("tooltip registration must precede the independent UI-polish latch")
    for host in ("QS3D.BricsCAD.V25", "QS3D.BricsCAD.V26"):
        entry = (ROOT / "src" / host / "PluginEntry.cs").read_text(encoding="utf-8")
        register = entry.find("ProductionUiPolish.EnsureRegistered();")
        start_ui = entry.find("RibbonInitializationCoordinator.Start();")
        if register < 0 or start_ui < 0 or register > start_ui:
            return fail(f"{host} must register tooltip polish before starting host UI")

    if sys.platform == "win32":
        probe = ROOT / "tests" / "test_ui_info_tooltip_bootstrap.py"
        result = subprocess.run([sys.executable, "-B", str(probe)], cwd=ROOT, timeout=150)
        if result.returncode:
            return fail("explicit WPF bootstrap behavior regression")
    else:
        print("SKIP: WPF bootstrap execution requires Windows; source checks only.")

    print("PASS: Agent Center and Update Center tooltip source contracts; no licensed-host runtime claim.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
