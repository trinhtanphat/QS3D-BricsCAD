#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpAgentControlCenter.cs"


def main() -> int:
    if not CENTER.is_file():
        print("ERROR: missing", CENTER.relative_to(ROOT))
        return 1

    text = CENTER.read_text(encoding="utf-8")
    errors: list[str] = []

    required = {
        "layout rounding": "UseLayoutRounding = true",
        "dashboard shell": "CreateDashboardShell()",
        "section card component": "CreateSectionCard(",
        "status chip component": "CreateStatusChip(",
        "status row component": "CreateStatusRow(",
        "primary action hierarchy": "ActionKind.Primary",
        "secondary action hierarchy": "ActionKind.Secondary",
        "danger action hierarchy": "ActionKind.Danger",
        "utility action hierarchy": "ActionKind.Utility",
        "navigation action hierarchy": "ActionKind.Navigation",
        "theme action hierarchy": "ActionKind.ThemeChoice",
        "theme mode enum": "enum ThemeMode",
        "system theme mode": "ThemeMode.System",
        "dark theme mode": "ThemeMode.Dark",
        "light theme mode": "ThemeMode.Light",
        "Windows app theme registry": "AppsUseLightTheme",
        "Windows theme change event": "SystemEvents.UserPreferenceChanged",
        "semantic theme palette": "ThemePalette",
        "theme selector": "CreateThemeSelector(",
        "tab navigation": "CreateTabNavigation(",
        "overview page": "CreateOverviewPage(",
        "cloudflare page": "CreateCloudflarePage(",
        "connector page": "CreateConnectorPage(",
        "agent control page": "CreateAgentControlPage(",
        "logs page": "CreateLogsPage(",
        "overview label": '"Tổng quan"',
        "cloudflare label": '"Cloudflare"',
        "connector label": '"ChatGPT Connector"',
        "agent label": '"Điều khiển Agent"',
        "logs label": '"Logs"',
        "toast host": "_toastHost",
        "toast kinds": "enum ToastKind",
        "toast presenter": "ShowToast(",
        "activity history": "AddActivityEntry(",
        "bounded activity history": "MaxActivityEntries = 50",
        "bounded visible toasts": "MaxVisibleToasts = 4",
        "toast timers": "DispatcherTimer",
        "custom button template": "new ControlTemplate(typeof(Button))",
        "button hover trigger": "Button.IsMouseOverProperty",
        "button pressed trigger": "Button.IsPressedProperty",
        "button keyboard focus trigger": "Button.IsKeyboardFocusedProperty",
        "button focus background ownership": "focus.Setters.Add(new Setter(Control.BackgroundProperty, background))",
        "button focus foreground ownership": "focus.Setters.Add(new Setter(Control.ForegroundProperty, foreground))",
        "button focus border ownership": "focus.Setters.Add(new Setter(Control.BorderBrushProperty, _palette.FocusBorder))",
        "button disabled trigger": "Button.IsEnabledProperty",
        "responsive scrolling": "HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled",
        "compact footer": "CreateFooter()",
    }
    for label, token in required.items():
        if token not in text:
            errors.append(f"Agent Center UI missing {label}: {token}")

    preserved = {
        "install flow": "McpCloudflaredBootstrapper.BeginInstall",
        "browser account setup": "McpCloudflareAccountSetupWindow",
        "canonical public endpoint": "McpPublicEndpointResolver.Resolve()",
        "protocol probe": "McpProtocolProbe.Check",
        "read-only self test": "RunReadOnlySelfTest",
        "emergency stop": 'InvokeControlTool("cad_agent_stop"',
        "cancel command": 'InvokeControlTool("cad_cancel_command"',
        "resume agent": 'InvokeControlTool("cad_agent_resume"',
        "worker-thread operations": "ThreadPool.QueueUserWorkItem",
        "serialized local checks": "Interlocked.CompareExchange(ref _localOperationActive, 1, 0)",
    }
    for label, token in preserved.items():
        if token not in text:
            errors.append(f"Agent Center UI regression removed {label}: {token}")

    forbidden = {
        "terminal dependency powershell": "powershell.exe",
        "terminal dependency cmd": "cmd.exe",
        "WinForms dependency": "System.Windows.Forms",
        "legacy fixed activity panel": "CreateActivityPanel()",
        "legacy activity text field": "_activity.Text",
        "blocking Agent Center message box": "MessageBox.Show(",
    }
    for label, token in forbidden.items():
        if token in text:
            errors.append(f"Agent Center UI contains forbidden {label}: {token}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP Agent Center UIUX tabs/toast/theme contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())