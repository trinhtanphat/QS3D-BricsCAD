#!/usr/bin/env python3
"""Fail closed unless Cloudflare automatic startup cannot block BricsCAD initialization."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
SUPERVISOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTransportSupervisor.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
text = SOURCE.read_text(encoding="utf-8")
supervisor = SUPERVISOR.read_text(encoding="utf-8")
plugin = PLUGIN.read_text(encoding="utf-8")

start = text.find("public static void TryAutoStart()")
end = text.find("public static void StopForHostShutdown()", start)
if start < 0 or end <= start:
    raise SystemExit("could not locate Cloudflare TryAutoStart boundary")
body = text[start:end]

required = [
    "_autoStartWorkerActive",
    "Interlocked.CompareExchange(ref _autoStartWorkerActive, 1, 0)",
    "ThreadPool.QueueUserWorkItem",
    "StartSaved(out ignored)",
    "Interlocked.Exchange(ref _autoStartWorkerActive, 0)",
]
for token in required:
    if token not in body and token != "_autoStartWorkerActive":
        raise SystemExit(f"Cloudflare TryAutoStart must be asynchronous/coalesced: missing {token}")
if "private static int _autoStartWorkerActive;" not in text:
    raise SystemExit("Cloudflare auto-start must own a process-local coalescing fence")

queue_at = body.find("ThreadPool.QueueUserWorkItem")
start_saved_at = body.find("StartSaved(out ignored)")
if queue_at < 0 or start_saved_at < queue_at:
    raise SystemExit("Cloudflare automatic StartSaved must execute only inside background work")

for token in [
    "private const int PublicReadinessTimeoutMs",
    "ProbePublicDns(hostname, out dnsState)",
    "ProbePublicHttps(hostname, out httpsState)",
    "if (!WaitForPublicReadiness(hostname, out error))",
]:
    if token not in text:
        raise SystemExit(f"Cloudflare readiness fail-closed contract drifted: {token}")

# Production startup no longer calls the Cloudflare helper directly. Pin the actual route from
# PluginEntry -> coordinator -> supervisor so a future supervisor cannot reintroduce synchronous
# network/DNS/public-readiness work on BricsCAD's initialization thread while this helper stays green.
if "McpTransportCoordinator.TryAutoStartPreferred();" not in plugin:
    raise SystemExit("PluginEntry must keep transport autostart behind the coordinator")

supervisor_start = supervisor.find("internal static void TryAutoStartPreferred(McpTransportProvider preferredProvider)")
supervisor_end = supervisor.find("internal static void StopForHostShutdown()", supervisor_start)
if supervisor_start < 0 or supervisor_end <= supervisor_start:
    raise SystemExit("could not locate supervisor TryAutoStartPreferred boundary")
supervisor_body = supervisor[supervisor_start:supervisor_end]
supervisor_queue_at = supervisor_body.find("ThreadPool.QueueUserWorkItem")
supervisor_run_at = supervisor_body.find('RunOneIteration("autostart")')
if supervisor_queue_at < 0:
    raise SystemExit("production supervisor autostart must queue the first iteration off the BricsCAD startup thread")
if supervisor_run_at < 0 or supervisor_run_at < supervisor_queue_at:
    raise SystemExit("production supervisor autostart may run only inside queued background work")
if "Interlocked.CompareExchange(ref _busy, 1, 0)" not in supervisor_body:
    raise SystemExit("queued supervisor autostart must share the watchdog coalescing fence")

print("PASS Cloudflare automatic startup is background/coalesced on both helper and production supervisor routes")
