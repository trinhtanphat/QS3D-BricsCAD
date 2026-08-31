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

    command_start = text.find("public sealed class McpAgentControlCenterCommands")
    command_end = text.find("internal sealed class McpAgentControlCenterWindow", command_start)
    command_block = text[command_start:command_end] if command_start >= 0 and command_end > command_start else ""
    if not command_block:
        errors.append("Agent Center UI missing canonical QS3DMCPAGENTCENTER command block")

    required = {
        "modeless Agent Center command": "new McpAgentControlCenterWindow().Show();",
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
        "active page dispatcher": "CreateActivePage(",
        "connection page": "CreateConnectionPage(",
        "agent page": "CreateAgentPage(",
        "recovery page": "CreateRecoveryPage(",
        "advanced page": "CreateAdvancedPage(",
        "connection tab label": 'CreateNavigationButton("Kết nối", 0)',
        "agent tab label": 'CreateNavigationButton("Agent", 1)',
        "recovery tab label": 'CreateNavigationButton("Backup & khôi phục", 2)',
        "advanced tab label": 'CreateNavigationButton("Nâng cao", 3)',
        "four-tab upper bound": "index > 3",
        "toast host": "_toastHost",
        "toast kinds": "enum ToastKind",
        "toast presenter": "ShowToast(",
        "toast dismissal": "DismissToast(",
        "activity history": "AddActivityEntry(",
        "bounded activity history": "MaxActivityEntries = 50",
        "bounded visible toasts": "MaxVisibleToasts = 4",
        "toast timers": "DispatcherTimer",
        "toast retained Tick handler": "TimerHandler",
        "toast timer stop": "visual.Timer.Stop()",
        "toast Tick handler detach": "visual.Timer.Tick -= visual.TimerHandler",
        "custom button template": "new ControlTemplate(typeof(Button))",
        "button hover trigger": "Button.IsMouseOverProperty",
        "button pressed trigger": "Button.IsPressedProperty",
        "button keyboard focus trigger": "Button.IsKeyboardFocusedProperty",
        "button disabled trigger": "Button.IsEnabledProperty",
        "focus owns background": "focus.Setters.Add(new Setter(Control.BackgroundProperty, background))",
        "focus owns foreground": "focus.Setters.Add(new Setter(Control.ForegroundProperty, foreground))",
        "focus owns border": "focus.Setters.Add(new Setter(Control.BorderBrushProperty, _palette.FocusBorder))",
        "focus owns thickness": "focus.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)))",
        "button trigger precedence": "Trigger precedence is intentional: focus -> hover -> pressed -> disabled.",
        "responsive scrolling": "HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled",
        "compact footer": "CreateFooter()",
        "transport readiness state": '"Transport sẵn sàng"',
        "ChatGPT registration state": '"ChatGPT đăng ký"',
        "OAuth MCP traffic state": '"OAuth MCP traffic"',
        "recent OAuth activity classifier": "HasRecentOAuthMcpActivity(",
        "OAuth activity formatting": "FormatOAuthMcpActivity(",
        "OAuth public URL binding": "McpEmbeddedServer.LastOAuthMcpPublicUrl",
        "OAuth request timestamp": "McpEmbeddedServer.LastOAuthMcpActivityUtc",
        "registration acknowledgement is distinct": "Đây là xác nhận cài đặt, chưa phải bằng chứng traffic",
        "Quick Tunnel polling cadence": "TimeSpan.FromMilliseconds(1500)",
        "Quick Tunnel bounded poll cap": "_quickUrlPollTicks >= 20",
    }
    for label, token in required.items():
        haystack = command_block if label == "modeless Agent Center command" else text
        if token not in haystack:
            errors.append(f"Agent Center UI missing {label}: {token}")

    if "ShowDialog()" in command_block:
        errors.append("Agent Center command must return immediately after modeless Show(); ShowDialog() keeps BricsCAD CMDACTIVE non-idle")

    preserved = {
        "install flow": "McpCloudflaredBootstrapper.BeginInstall",
        "browser account setup": "McpCloudflareAccountSetupWindow",
        "canonical public endpoint": "McpPublicEndpointResolver.Resolve()",
        "protocol probe": "McpProtocolProbe.Check",
        "read-only self test": "RunReadOnlySelfTest",
        "emergency stop wrapper": "EmergencyStop()",
        "emergency stop tool": 'InvokeControlTool("cad_agent_stop"',
        "cancel command": 'InvokeControlTool("cad_cancel_command"',
        "local-only desktop resume": "McpDesktopControlSession.ResumeFromLocalUser()",
        "local-only desktop pause": "McpDesktopControlSession.PauseFromLocalUser(",
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
        "legacy overview page": "CreateOverviewPage(",
        "legacy standalone Cloudflare page": "CreateCloudflarePage(",
        "legacy standalone connector page": "CreateConnectorPage(",
        "legacy standalone Agent control page": "CreateAgentControlPage(",
        "legacy standalone logs page": "CreateLogsPage(",
        "remote/direct UI desktop resume tool": 'InvokeControlTool("cad_agent_resume"',
    }
    for label, token in forbidden.items():
        if token in text:
            errors.append(f"Agent Center UI contains forbidden {label}: {token}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP Agent Center UIUX four-tab/toast/theme/local-consent/connectivity/modeless-command contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())