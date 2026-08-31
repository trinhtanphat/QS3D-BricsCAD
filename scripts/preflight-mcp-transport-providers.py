#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
OPENAI = SRC / "McpOpenAiSecureTunnel.cs"
CENTER = SRC / "McpAgentControlCenter.cs"
BOOTSTRAP = SRC / "McpCloudflaredBootstrapper.cs"
V25_ENTRY = SRC / "PluginEntry.cs"
V26_ENTRY = ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs"
DOC = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"


def need(text: str, token: str, label: str, errors: list[str]) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str, errors: list[str]) -> None:
    if token in text:
        errors.append(f"forbidden {label}: {token}")


def main() -> int:
    errors: list[str] = []
    for path in (OPENAI, CENTER, BOOTSTRAP, V25_ENTRY, V26_ENTRY, DOC):
        if not path.is_file():
            errors.append(f"missing file: {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    openai = OPENAI.read_text(encoding="utf-8")
    center = CENTER.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    v25 = V25_ENTRY.read_text(encoding="utf-8")
    v26 = V26_ENTRY.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

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
        'public static bool BeginInstall': "busy-aware installer return",
        'if (_installing) return false;': "single-flight installer guard",
        'return true;': "installer started result",
    }.items():
        need(bootstrap, token, label, errors)
    forbid(bootstrap, 'completed(false, "Cloudflare Tunnel đang được tải/cài. Vui lòng chờ.")', "busy state reported as failure callback", errors)

    for entry_text, name in ((v25, "V25"), (v26, "V26")):
        need(entry_text, "McpTransportCoordinator.TryAutoStartPreferred()", f"{name} preferred transport startup", errors)
        need(entry_text, "McpTransportCoordinator.StopAllForHostShutdown", f"{name} transport teardown", errors)

    for token, label in {
        "OpenAI Secure MCP Tunnel": "canonical OpenAI transport docs",
        "Cloudflare Named Tunnel": "canonical Named Tunnel docs",
        "Quick Tunnel": "canonical Quick Tunnel docs",
        "Connection = Tunnel": "canonical ChatGPT tunnel onboarding",
        "Runtime API key": "canonical runtime-key guidance",
        "LOCAL_ONLY": "runtime qualification boundary",
    }.items():
        need(doc, token, label, errors)

    # Secrets may enter only through process environment. They must never be serialized as values
    # into QS3D-owned config/settings files.
    forbid(openai, "WriteText(ControlPlaneApiKeyEnvironment", "persisted Runtime API key", errors)
    forbid(openai, "File.WriteAllText(ControlPlaneApiKeyEnvironment", "persisted Runtime API key", errors)
    forbid(openai, "WriteText(LocalBearerEnvironment", "persisted local bearer", errors)

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP transport providers / Secure Tunnel / Cloudflare busy-install contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
