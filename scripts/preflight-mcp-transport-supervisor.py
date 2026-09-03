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

if errors:
    print("MCP transport supervisor preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("MCP transport supervisor preflight PASS")
print("NOTE: concurrent live Cloudflare/OpenAI qualification remains LOCAL_ONLY")
