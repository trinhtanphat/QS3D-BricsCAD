#!/usr/bin/env python3
"""Fail closed unless Cloudflare automatic startup cannot block BricsCAD initialization."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
text = SOURCE.read_text(encoding="utf-8")

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

# Automatic startup may enqueue the existing truthful StartSaved path, but it must never
# invoke it synchronously before the queue boundary.
queue_at = body.find("ThreadPool.QueueUserWorkItem")
start_saved_at = body.find("StartSaved(out ignored)")
if queue_at < 0 or start_saved_at < queue_at:
    raise SystemExit("Cloudflare automatic StartSaved must execute only inside background work")

# Keep the public-readiness safety contract intact; this fix changes scheduling, not truthfulness.
for token in [
    "private const int PublicReadinessTimeoutMs",
    "ProbePublicDns(hostname, out dnsState)",
    "ProbePublicHttps(hostname, out httpsState)",
    "if (!WaitForPublicReadiness(hostname, out error))",
]:
    if token not in text:
        raise SystemExit(f"Cloudflare readiness fail-closed contract drifted: {token}")

print("PASS Cloudflare automatic startup is background/coalesced while readiness remains fail closed")
