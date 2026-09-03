#!/usr/bin/env python3
"""Fail-closed source guard for issue #5457 multi-transport supervision.

This guard is intentionally deterministic. It validates the lifecycle/failover contract in source
without claiming live concurrent Cloudflare/OpenAI qualification; that boundary remains LOCAL_ONLY.
"""
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

# Fresh child registration must not synchronously query Process.MainModule. On the BricsCAD/.NET
# runtime that accessor can fail transiently immediately after Process.Start(), which turns a
# successfully launched tunnel into an ownership-persistence failure. Registration already has the
# exact trust-verified executable path used to spawn the child; persist that expected path, then keep
# MainModule + start-time revalidation on the later stale-cleanup boundary before any Kill().
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

# StopProvider must not erase a crash-surviving sidecar unconditionally. The sidecar is the proof used
# on the next start to identify and safely terminate only a QS3D-owned orphan process.
stop_provider = re.search(
    r"private static void StopProvider\(McpTransportProvider provider\)\s*\{(?P<body>.*?)\n        \}\n\n        internal static bool TryGetFallbackProvider",
    supervisor,
    re.DOTALL,
)
if not stop_provider:
    errors.append("supervisor: cannot locate StopProvider body")
elif "ClearOwnedProcess(provider)" in stop_provider.group("body"):
    errors.append("supervisor: StopProvider must preserve crash-surviving ownership sidecar")

# Provider stop paths must clear ownership metadata only after the child is confirmed exited.
# A kill exception or WaitForExit timeout must retain the sidecar for the next identity-safe cleanup.
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
