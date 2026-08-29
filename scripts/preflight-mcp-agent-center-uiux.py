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
        "dashboard background": "Background = SurfaceBrush",
        "dashboard shell": "CreateDashboardShell()",
        "card component": "CreateCard(",
        "status chip component": "CreateStatusChip(",
        "status row component": "CreateStatusRow(",
        "primary action hierarchy": "ActionKind.Primary",
        "secondary action hierarchy": "ActionKind.Secondary",
        "danger action hierarchy": "ActionKind.Danger",
        "setup card": 'CreateCard("Kết nối Cloudflare"',
        "connector card": 'CreateCard("ChatGPT Connector"',
        "agent control card": 'CreateCard("Điều khiển Agent"',
        "system status card": 'CreateCard("Trạng thái hệ thống"',
        "recent activity panel": 'Text = "Hoạt động gần nhất"',
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
    }
    for label, token in preserved.items():
        if token not in text:
            errors.append(f"Agent Center UI regression removed {label}: {token}")

    forbidden = (
        "powershell.exe",
        "cmd.exe",
        "System.Windows.Forms",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"Agent Center UI introduced forbidden dependency: {token}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP Agent Center UIUX contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
