#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs"

if not SOURCE.is_file():
    raise SystemExit("MCP account onboarding UX source is missing")

source = SOURCE.read_text(encoding="utf-8")

required = (
    'Button("Kết nối ChatGPT"',
    "private void ConnectChatGpt()",
    'Header = "Kết nối cố định (tùy chọn)"',
    "McpToastWindow.Show(",
    "sealed class McpToastWindow : Window",
    "DispatcherTimer",
    "OpenFileDialog",
    "CertificateImportNeeded",
    "ImportDownloadedCertificate",
    '"Failed to write the certificate"',
    '"download the certificate instead"',
    '"-----BEGIN ARGO TUNNEL TOKEN-----"',
    '"-----END ARGO TUNNEL TOKEN-----"',
    "private void ShowTechnicalDetails()",
    "private string BuildTechnicalDetails()",
    'Button("Sao chép cấu hình ChatGPT"',
    'Button("Ngắt kết nối"',
)
missing = [token for token in required if token not in source]
if missing:
    raise SystemExit("MCP account onboarding one-click/toast contract missing: " + repr(missing))

setup_start = source.index("internal sealed class McpCloudflareAccountSetupWindow : Window")
setup = source[setup_start:]

forbidden = (
    "panel.Children.Add(_status)",
    "private readonly TextBlock _status",
)
found = [token for token in forbidden if token in setup]
if found:
    raise SystemExit("MCP onboarding must not render raw status at the bottom: " + repr(found))

connect_start = setup.index("private void ConnectChatGpt()")
connect_end = setup.index("private void", connect_start + len("private void ConnectChatGpt()"))
connect = setup[connect_start:connect_end]
for token in (
    "McpEmbeddedServer.EnsureStarted();",
    "McpCloudflareAccountTunnelManager.StartSaved(out",
    "McpCloudflareAccountTunnelManager.StartQuickTunnel(out",
):
    if token not in connect:
        raise SystemExit("One-click ChatGPT connection path missing: " + token)

login_start = setup.index("private void Login()")
login_end = setup.index("private void", login_start + len("private void Login()"))
login = setup[login_start:login_end]
if "CertificateImportNeeded" not in login or "ImportDownloadedCertificate" not in login:
    raise SystemExit("Recoverable browser cert.pem login fallback is not wired into UI")

print("PASS MCP account onboarding one-click/toast/cert fallback source guard")
