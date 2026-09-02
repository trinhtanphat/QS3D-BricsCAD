#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs"

if not SOURCE.is_file():
    raise SystemExit("MCP onboarding source is missing")

source = SOURCE.read_text(encoding="utf-8")
start = source.index("internal sealed class McpCloudflareAccountSetupWindow : Window")
end = source.index("internal sealed class McpToastWindow : Window", start)
setup = source[start:end]

required = {
    "dark canvas": 'Color.FromRgb(13, 17, 23)',
    "raised card": 'Color.FromRgb(22, 27, 34)',
    "primary accent": 'Color.FromRgb(47, 129, 247)',
    "primary hover": 'Color.FromRgb(56, 139, 253)',
    "danger accent": 'Color.FromRgb(248, 81, 73)',
    "primary button kind": 'OnboardingButtonKind.Primary',
    "danger button kind": 'OnboardingButtonKind.Danger',
    "utility button kind": 'OnboardingButtonKind.Utility',
    "button template": 'CreateButtonTemplate()',
    "focus trigger": 'IsKeyboardFocusedProperty',
    "hover trigger": 'IsMouseOverProperty',
    "pressed trigger": 'Button.IsPressedProperty',
    "disabled trigger": 'IsEnabledProperty',
    "status badge": 'CreateStatusBadge()',
    "advanced card": 'CreateAdvancedCard(advancedPanel)',
    "modern window chrome": 'WindowStyle = WindowStyle.SingleBorderWindow',
}
missing = [f"{label}: {token}" for label, token in required.items() if token not in setup]
if missing:
    raise SystemExit("MCP onboarding Pro Dark UI contract missing: " + repr(missing))

# Connection semantics must stay wired to the same handlers.
for token in (
    'Button("Kết nối ChatGPT", (_, __) => ConnectChatGpt()',
    'Button("Sao chép cấu hình ChatGPT", (_, __) => CopyConfig()',
    'Button("Mở ChatGPT", (_, __) => McpCloudflareAccountTunnelManager.OpenChatGpt()',
    'Button("Ngắt kết nối", (_, __) => Disconnect()',
    'Button("Kiểm tra MCP local", (_, __) => Probe()',
    'Button("Chi tiết kỹ thuật", (_, __) => ShowTechnicalDetails()',
):
    if token not in setup:
        raise SystemExit("MCP onboarding behavior routing changed unexpectedly: " + token)

print("PASS MCP onboarding Pro Dark contrast/action hierarchy source guard")
