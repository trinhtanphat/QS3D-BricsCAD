#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
OPENAI = SRC / "McpOpenAiSecureTunnel.cs"
CENTER = SRC / "McpAgentControlCenter.cs"
AUGMENTER = SRC / "McpTransportAgentCenterAugmenter.cs"
BOOTSTRAP = SRC / "McpCloudflaredBootstrapper.cs"
CLOUDFLARE_FALLBACK = SRC / "McpCloudflareOnboarding.cs"
FIRST_RUN = SRC / "McpFirstRunExperience.cs"
V25_ENTRY = SRC / "PluginEntry.cs"
V26_ENTRY = ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs"
DOC = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"
RECOVERY_DOC = ROOT / "docs" / "MCP-TRANSPORT-DIAGNOSTICS-RECOVERY.md"


def need(text: str, token: str, label: str, errors: list[str]) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str, errors: list[str]) -> None:
    if token in text:
        errors.append(f"forbidden {label}: {token}")


def main() -> int:
    errors: list[str] = []
    for path in (OPENAI, CENTER, AUGMENTER, BOOTSTRAP, CLOUDFLARE_FALLBACK, FIRST_RUN, V25_ENTRY, V26_ENTRY, DOC, RECOVERY_DOC):
        if not path.is_file():
            errors.append(f"missing file: {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    openai = OPENAI.read_text(encoding="utf-8")
    center = CENTER.read_text(encoding="utf-8")
    augmenter = AUGMENTER.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    cloudflare_fallback = CLOUDFLARE_FALLBACK.read_text(encoding="utf-8")
    first_run = FIRST_RUN.read_text(encoding="utf-8")
    v25 = V25_ENTRY.read_text(encoding="utf-8")
    v26 = V26_ENTRY.read_text(encoding="utf-8")
    canonical_doc = DOC.read_text(encoding="utf-8")
    recovery_doc = RECOVERY_DOC.read_text(encoding="utf-8")
    doc = canonical_doc + "\n" + recovery_doc

    for token, label in {
        "enum McpTransportProvider": "transport provider enum",
        "OpenAiSecureTunnel": "OpenAI provider",
        "CloudflareNamedTunnel": "Cloudflare Named provider",
        "CloudflareQuickTunnel": "Cloudflare Quick provider",
        "McpTransportCoordinator": "transport coordinator",
        "McpOpenAiSecureTunnelManager": "OpenAI tunnel supervisor",
        "https://platform.openai.com/settings/organization/tunnels": "OpenAI Tunnels setup page",
        "https://github.com/openai/tunnel-client/releases/latest": "official tunnel-client release page",
        "https://chatgpt.com/#settings/Connectors": "ChatGPT connector settings",
        "^tunnel_[0-9a-f]{32}$": "strict Tunnel ID format",
        'api_key: env:CONTROL_PLANE_API_KEY': "runtime key env reference",
        'Authorization: env:': "local bearer env reference",
        'McpEmbeddedServer.Endpoint': "dynamic loopback endpoint binding",
        'McpEmbeddedServer.GetBearerToken()': "existing local bearer boundary",
        'HEALTH_LISTEN_ADDR': "loopback health endpoint",
        '/readyz': "readiness probe",
        'CreateNoWindow = true': "bounded child process launch",
        'UseShellExecute = false': "non-shell tunnel-client launch",
        'WriteText(AutoStartFile, "1")': "non-secret auto-start metadata",
        'if (previous != provider) ForgetChatGptRegistrationAcknowledgement();': "idempotent provider selection registration preservation",
        'QS3D_OPENAI_TUNNEL_CLIENT_SHA256': "pinned unsigned-release SHA-256 fallback",
        'TryVerifyClientTrust': "pre-launch tunnel-client trust verification",
        'VerifyAuthenticode': "tunnel-client Authenticode verification",
        'WinVerifyTrust': "OS trust-provider verification",
        'ComputeSha256': "tunnel-client SHA-256 verification",
        'RedirectStandardOutput = true': "tunnel-client stdout capture",
        'RedirectStandardError = true': "tunnel-client stderr capture",
        'HandleDiagnosticLine': "bounded tunnel-client diagnostic capture",
        'MaxDiagnosticLines': "bounded tunnel-client diagnostic history",
        'SanitizeDiagnosticLine': "tunnel-client diagnostic secret redaction",
        'private static string SanitizeDiagnosticLine(string? value)': "nullable-safe diagnostic sanitizer",
        'GetDiagnosticBundle': "copyable tunnel diagnostic bundle",
        'LastExitCode': "tunnel-client exit-code diagnostics",
    }.items():
        need(openai, token, label, errors)

    for token, label in {
        'OpenAI Secure Tunnel': "OpenAI selector",
        'Cloudflare Named': "Named selector",
        'Cloudflare Quick · test': "Quick selector",
        'Connection = Tunnel': "ChatGPT tunnel guidance",
        'Runtime API key · chỉ giữ trong RAM': "runtime-key memory-only guidance",
        'SelectOpenAiTunnelClient': "user-selected tunnel-client path",
        'StartOpenAiSecureTunnel': "OpenAI start action",
        'McpOpenAiSecureTunnelManager.IsReady': "OpenAI ready status",
        'Không cần public URL': "no-public-URL status",
        'Cloudflare Tunnel đang được tải/cài. Vui lòng chờ; đây không phải lỗi.': "busy install informational UX",
    }.items():
        need(center, token, label, errors)

    for token, label in {
        'PresentationSource.CurrentSources': "host-safe WPF Agent Center discovery",
        'AgentCenterTitle': "Agent Center-only augmentation boundary",
        'Đang cài Cloudflare... ': "Agent Center live install progress",
        'Copy WinGet recovery command': "Agent Center WinGet recovery action",
        'winget install --id Cloudflare.cloudflared --source winget': "exact WinGet recovery command",
        'BuildCloudflareBinaryStatus': "visible cloudflared binary provenance status",
        'Trust=VERIFIED': "visible cloudflared trust status",
        'TryResolveTrustedInstalledBinary': "visible cloudflared source/path resolution",
        'Copy tunnel diagnostics': "Agent Center tunnel diagnostic copy action",
        'Open tunnel logs': "Agent Center sanitized log action",
        'OpenOpenAiLogs': "on-demand sanitized log materialization",
        'tunnel-diagnostics.log': "bounded support log destination",
        'McpOpenAiSecureTunnelManager.GetDiagnosticBundle()': "Agent Center sanitized diagnostic bundle",
        'Restart tunnel · env key': "Agent Center environment-key-only restart action",
        'CONTROL_PLANE_API_KEY': "restart environment-key presence check",
        'OPENAI_API_KEY': "restart fallback environment-key presence check",
        'QS3D không lưu key đã nhập trong UI': "restart secret non-persistence guidance",
        'ClientTrustSummary': "visible tunnel trust summary",
        'LastExitCode': "visible tunnel exit code",
        'LastError': "visible tunnel last error",
        'DispatcherTimer': "bounded UI augmenter refresh",
    }.items():
        need(augmenter, token, label, errors)

    # The single-cancel-owner invariant is structural: the augmenter must not create or wire its
    # own installer-cancel identity/handler, while the bootstrapper below must own the dynamic one.
    # Do not gate on an arbitrary prose comment because wording changes must not break CI.
    forbid(augmenter, 'QS3D_MCP_CLOUDFLARED_CANCEL', "second Agent Center cloudflared cancel owner", errors)
    forbid(augmenter, 'McpCloudflaredBootstrapper.CancelInstall', "second Agent Center cloudflared cancel handler", errors)

    for token, label in {
        'public static bool BeginInstall': "busy-aware installer return",
        'if (_installing) return false;': "single-flight installer guard",
        'public static bool CancelInstall': "user cancellation",
        'DownloadTimeoutMilliseconds = 120000': "bounded download timeout",
        'ReadWriteTimeoutMilliseconds = 30000': "bounded download read/write timeout",
        'DownloadFileAsync': "non-blocking cloudflared download",
        'InstallProgressPercent': "download progress state",
        'InstallStatus': "download status state",
        'TryResolveTrustedInstalledBinary': "trusted installed-binary discovery",
        '"WinGet"': "WinGet installation discovery",
        'winget install --id Cloudflare.cloudflared': "WinGet recovery hint",
        'VerifyCloudflareBinary': "cloudflared trust verification",
        'WinVerifyTrust': "cloudflared OS trust verification",
        'signer.IndexOf("Cloudflare"': "Cloudflare signer restriction",
        'PublishInstallerUiState': "installer UI refresh",
        'button.IsEnabled = !busy': "installer button disabled while busy",
        'EnsureDynamicCancelButton': "single bootstrapper-owned fallback cancel action",
        'DynamicCancelTag': "bootstrapper-owned cancel identity",
        'File.Move(temporary, destination)': "atomic managed install replacement",
    }.items():
        need(bootstrap, token, label, errors)
    forbid(bootstrap, 'completed(false, "Cloudflare Tunnel đang được tải/cài. Vui lòng chờ.")', "busy state reported as failure callback", errors)
    forbid(bootstrap, 'client.DownloadFile(', "unbounded synchronous cloudflared download", errors)

    for token, label in {
        'TryResolveTrustedInstalledBinary': "trusted cloudflared launch resolution",
        'Hủy cài Cloudflare Tunnel': "advanced installer cancel button",
        '_installButton.IsEnabled = !busy': "advanced installer busy disable",
        '_cancelInstallButton.IsEnabled = busy': "advanced cancel enable state",
        'InstallProgressPercent': "advanced installer progress display",
        'CancelCloudflaredInstall': "advanced cancellation action",
        'cài bằng WinGet rồi Refresh': "trusted WinGet recovery guidance",
    }.items():
        need(cloudflare_fallback, token, label, errors)

    for token, label in {
        'McpTransportCoordinator.SelectedProvider': "selected-provider first-run routing",
        'IsSelectedTransportReady(': "provider-aware first-run completion",
        'McpOpenAiSecureTunnelManager.IsReady': "Secure Tunnel first-run readiness",
        'Connection = Tunnel': "Secure Tunnel first-run ChatGPT guidance",
        'không cần tài khoản/domain Cloudflare do người dùng quản lý hoặc public MCP URL': "accurate no-user-managed-Cloudflare guidance",
        'Cloudflare Quick Tunnel': "Quick Tunnel first-run guidance",
    }.items():
        need(first_run, token, label, errors)

    for entry_text, name in ((v25, "V25"), (v26, "V26")):
        need(entry_text, "McpTransportAgentCenterAugmenter.Start()", f"{name} transport UI augmenter startup", errors)
        need(entry_text, "McpTransportCoordinator.TryAutoStartPreferred()", f"{name} preferred transport startup", errors)
        need(entry_text, "McpTransportAgentCenterAugmenter.Stop", f"{name} transport UI augmenter teardown", errors)
        need(entry_text, "McpTransportCoordinator.StopAllForHostShutdown", f"{name} transport teardown", errors)

    for token, label in {
        "OpenAI Secure MCP Tunnel": "canonical OpenAI transport docs",
        "Cloudflare Named Tunnel": "canonical Named Tunnel docs",
        "Quick Tunnel": "canonical Quick Tunnel docs",
        "Connection = Tunnel": "canonical ChatGPT tunnel onboarding",
        "Runtime API key": "canonical runtime-key guidance",
        "LOCAL_ONLY": "runtime qualification boundary",
        "QS3D_OPENAI_TUNNEL_CLIENT_SHA256": "canonical unsigned tunnel-client hash pinning",
        "winget install --id Cloudflare.cloudflared": "canonical WinGet recovery command",
        "Copy WinGet recovery command": "documented WinGet recovery action",
        "Trust=VERIFIED": "documented cloudflared provenance status",
        "120 seconds": "canonical cloudflared download timeout",
        "Cancel": "canonical installer cancellation contract",
        "stdout/stderr": "canonical tunnel diagnostic capture",
        "Authenticode": "canonical transport binary trust policy",
        "Copy tunnel diagnostics": "documented Agent Center diagnostic action",
        "Open tunnel logs": "documented sanitized support log action",
        "Restart tunnel": "documented Agent Center restart action",
        "does not require the user to own/configure a Cloudflare account": "precise Secure Tunnel Cloudflare wording",
    }.items():
        need(doc, token, label, errors)

    # Secrets may enter only through process environment. They must never be serialized as values
    # into QS3D-owned config/settings files or copied into the diagnostic bundle.
    forbid(openai, "WriteText(ControlPlaneApiKeyEnvironment", "persisted Runtime API key", errors)
    forbid(openai, "File.WriteAllText(ControlPlaneApiKeyEnvironment", "persisted Runtime API key", errors)
    forbid(openai, "WriteText(LocalBearerEnvironment", "persisted local bearer", errors)
    forbid(openai, "builder.AppendLine(key", "Runtime API key copied into diagnostics", errors)

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP transport providers / binary trust / bounded installer recovery / single cancel owner / Agent Center diagnostics / first-run contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
