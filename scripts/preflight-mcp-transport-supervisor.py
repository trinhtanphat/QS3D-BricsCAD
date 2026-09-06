#!/usr/bin/env python3
"""Fail-closed source guard for issue #5457/#5917 multi-transport supervision."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SUPERVISOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTransportSupervisor.cs"
OPENAI = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
CLOUDFLARE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"

errors = []

def read(path: Path) -> str:
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label}: missing contract token {token!r}")

def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label}: forbidden contract token {token!r}")

supervisor = read(SUPERVISOR)
openai = read(OPENAI)
cloudflare = read(CLOUDFLARE)

for token in (
    "internal static class McpTransportSupervisor",
    "MaxRestartAttempts",
    "ComputeRestartBackoff",
    "TryGetFallbackProvider",
    "TryCleanupStaleOwnedProcess",
    "RegisterOwnedProcess",
    "ClearOwnedProcess",
    "McpTransportSupervisorSnapshot",
    "ActiveProvider",
    "FailoverReason",
    "LOCAL_ONLY",
):
    require(supervisor, token, "supervisor")

for token in (
    "McpTransportSupervisor.TryAutoStartPreferred",
    "McpTransportSupervisor.StopForHostShutdown",
):
    require(openai, token, "coordinator")

for token in (
    "StartForSupervisor",
    "McpTransportSupervisor.TryCleanupStaleOwnedProcess",
    "McpTransportSupervisor.RegisterOwnedProcess",
    "McpTransportSupervisor.ClearOwnedProcess",
):
    require(openai, token, "openai")
    require(cloudflare, token, "cloudflare")

# Cloudflare health must prove the public MCP route, not merely a live cloudflared child. Keep the
# bounded re-probe in the supervisor so watchdog/failover health and process ownership remain one
# state machine without reopening the large onboarding surface.
cloudflare_health = re.search(
    r"private static bool IsCloudflarePublicHealthy\(\)\s*\{(?P<body>.*?)\n        \}\n\n        private static bool IsReachableMcpStatus",
    supervisor,
    re.DOTALL,
)
if not cloudflare_health:
    errors.append("cloudflare public health: missing bounded supervisor re-probe")
else:
    health_body = cloudflare_health.group("body")
    require(health_body, "McpCloudflareAccountTunnelManager.SavedHostname", "cloudflare public health")
    require(health_body, "McpCloudflareTunnelManager.NormalizeHostname", "cloudflare public health")
    require(health_body, "Dns.GetHostAddressesAsync(hostname)", "cloudflare public health")
    require(health_body, "dnsTask.Wait(CloudflarePublicProbeTimeoutMilliseconds)", "cloudflare public health")
    require(health_body, 'WebRequest.Create("https://" + hostname + "/mcp")', "cloudflare public health")
    require(health_body, "request.AllowAutoRedirect = false", "cloudflare public health")
    require(health_body, "request.Timeout = CloudflarePublicProbeTimeoutMilliseconds", "cloudflare public health")
    require(health_body, "IsReachableMcpStatus", "cloudflare public health")

status_helper = re.search(
    r"private static bool IsReachableMcpStatus\(HttpStatusCode status\)\s*\{(?P<body>.*?)\n        \}",
    supervisor,
    re.DOTALL,
)
if not status_helper:
    errors.append("cloudflare public health: missing bounded HTTP status classifier")
else:
    status_body = status_helper.group("body")
    for token in ("200", "400", "401", "403", "404", "405"):
        require(status_body, token, "cloudflare public health status")

provider_health = re.search(
    r"private static bool IsProviderHealthy\(McpTransportProvider provider\)\s*\{(?P<body>.*?)\n        \}\n\n        private static bool IsCloudflarePublicHealthy",
    supervisor,
    re.DOTALL,
)
if not provider_health:
    errors.append("supervisor: cannot locate IsProviderHealthy body before public-health probe")
else:
    provider_health_body = provider_health.group("body")
    require(
        provider_health_body,
        "McpCloudflareAccountTunnelManager.IsRunning && IsCloudflarePublicHealthy()",
        "cloudflare supervisor health",
    )
    forbid(
        provider_health_body,
        "if (provider == McpTransportProvider.CloudflareNamedTunnel)\n                return McpCloudflareAccountTunnelManager.IsRunning;",
        "cloudflare supervisor health",
    )

# Never regress to broad process enumeration: stale cleanup may terminate only a PID recorded by QS3D
# and revalidated by process start-time + executable identity.
for forbidden in (
    "Process.GetProcessesByName(\"cloudflared\")",
    "Process.GetProcessesByName(\"tunnel-client\")",
    "Process.GetProcesses()",
):
    if forbidden in supervisor or forbidden in cloudflare or forbidden in openai:
        errors.append(f"unsafe broad process cleanup detected: {forbidden}")

for token in ("StartTime", "MainModule", "Path.GetFullPath"):
    require(supervisor, token, "owned-process identity")

register_owned = re.search(
    r"internal static bool RegisterOwnedProcess\(.*?\)\s*\{(?P<body>.*?)\n        \}\n\n        internal static bool TryCleanupStaleOwnedProcess",
    supervisor,
    re.DOTALL,
)
if not register_owned:
    errors.append("owned-process registration: cannot locate RegisterOwnedProcess body")
else:
    register_body = register_owned.group("body")
    forbid(register_body, "MainModule", "fresh owned-process registration")
    require(register_body, "NormalizePath(expectedExecutable)", "fresh owned-process registration")
    require(register_body, "Executable = expected", "fresh owned-process registration")

cleanup_owned = re.search(
    r"internal static bool TryCleanupStaleOwnedProcess\(.*?\)\s*\{(?P<body>.*?)\n        \}\n\n        internal static void ClearOwnedProcess",
    supervisor,
    re.DOTALL,
)
if not cleanup_owned:
    errors.append("owned-process cleanup: cannot locate TryCleanupStaleOwnedProcess body")
else:
    cleanup_body = cleanup_owned.group("body")
    require(cleanup_body, "process.StartTime", "stale owned-process cleanup")
    require(cleanup_body, "process.MainModule", "stale owned-process cleanup")
    require(cleanup_body, "process.Kill()", "stale owned-process cleanup")

stop_provider = re.search(
    r"private static void StopProvider\(McpTransportProvider provider\)\s*\{(?P<body>.*?)\n        \}\n\n        internal static bool TryGetFallbackProvider",
    supervisor,
    re.DOTALL,
)
if not stop_provider:
    errors.append("supervisor: cannot locate StopProvider body")
elif "ClearOwnedProcess(provider)" in stop_provider.group("body"):
    errors.append("supervisor: StopProvider must preserve crash-surviving ownership sidecar")

for text, provider_token, label in (
    (openai, "McpTransportProvider.OpenAiSecureTunnel", "openai owned-process stop"),
    (cloudflare, "McpTransportProvider.CloudflareNamedTunnel", "cloudflare owned-process stop"),
):
    require(text, "var exitConfirmed = false;", label)
    require(text, "if (exitConfirmed)", label)
    require(text, "McpTransportSupervisor.ClearOwnedProcess(" + provider_token + ");", label)

forbid(
    openai,
    "try { process.Dispose(); } catch { }\n                McpTransportSupervisor.ClearOwnedProcess(McpTransportProvider.OpenAiSecureTunnel);",
    "openai owned-process stop",
)
forbid(
    cloudflare,
    "try { process.Dispose(); } catch { }\n            McpTransportSupervisor.ClearOwnedProcess(McpTransportProvider.CloudflareNamedTunnel);",
    "cloudflare owned-process stop",
)

if errors:
    print("MCP transport supervisor preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("MCP transport supervisor preflight PASS")
print("NOTE: concurrent live Cloudflare/OpenAI qualification remains LOCAL_ONLY")
