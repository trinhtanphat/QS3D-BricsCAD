#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs").read_text(encoding="utf-8")

required = [
    "private const int PublicReadinessTimeoutMs",
    "private const int PublicProbeTimeoutMs",
    "private const int PublicProbeRetryDelayMs",
    "private static bool _publicReady;",
    "public static bool IsPublicReady",
    "PublicMcpUrl => IsRunning && IsPublicReady",
    "private static bool EnsureDnsRoute(string executable, string tunnelId, string hostname, out string error)",
    "private static bool WaitForPublicReadiness(string hostname, out string error)",
    "private static bool ProbePublicDns(string hostname, out string state)",
    "private static bool ProbePublicHttps(string hostname, out string state)",
    "Dns.GetHostAddresses(hostname)",
    "new Uri(\"https://\" + hostname + \"/mcp\"",
    "request.Timeout = PublicProbeTimeoutMs;",
    "request.ReadWriteTimeout = PublicProbeTimeoutMs;",
    "statusCode < 500",
    "StopProcess();",
]
for token in required:
    if token not in source:
        raise SystemExit(f"missing Cloudflare public-readiness contract: {token}")

if "--overwrite-dns" in source:
    raise SystemExit("Cloudflare saved-route recovery must never blindly overwrite an existing DNS record")

start_saved_start = source.index("public static bool StartSaved(out string error)")
start_saved_end = source.index("internal static bool StartForSupervisor", start_saved_start)
start_saved = source[start_saved_start:start_saved_end]
if "EnsureDnsRoute(executable, id, hostname, out error)" not in start_saved:
    raise SystemExit("saved Named Tunnel start must re-assert the expected DNS route before launch")
if start_saved.index("EnsureDnsRoute(executable, id, hostname, out error)") > start_saved.index("StartProcess("):
    raise SystemExit("saved Named Tunnel DNS route must be checked before cloudflared starts")

ensure_start = source.index("private static bool EnsureDnsRoute(string executable, string tunnelId, string hostname, out string error)")
ensure_end = source.index("private static bool WaitForPublicReadiness", ensure_start)
ensure = source[ensure_start:ensure_end]
for token in [
    "tunnel route dns ",
    "LooksLikeExistingRouteConflict",
    "route=conflict",
    "route=ready",
]:
    if token not in ensure:
        raise SystemExit(f"DNS route re-assertion must fail closed: {token}")

for forbidden in [
    "HasExpectedLocalRouteProof",
    "hadExpectedLocalProof",
    "route=ready(existing-local-proof)",
]:
    if forbidden in ensure:
        raise SystemExit(
            "DNS route conflict must fail closed even when stale local route/config proof exists: " + forbidden
        )

wait_start = source.index("private static bool WaitForPublicReadiness(string hostname, out string error)")
wait_end = source.index("private static bool ProbePublicDns", wait_start)
wait = source[wait_start:wait_end]
for token in [
    "Stopwatch.StartNew()",
    "ProbePublicDns(hostname, out dnsState)",
    "ProbePublicHttps(hostname, out httpsState)",
    "PublicReadinessTimeoutMs",
    "PublicProbeRetryDelayMs",
    "_publicReady = true;",
]:
    if token not in wait:
        raise SystemExit(f"public readiness must be bounded and require DNS plus HTTPS: {token}")

https_start = source.index("private static bool ProbePublicHttps(string hostname, out string state)")
https_end = source.index("private static bool LooksLikeExistingRouteConflict", https_start)
https_probe = source[https_start:https_end]
if "https://" not in https_probe or "/mcp" not in https_probe:
    raise SystemExit("HTTPS readiness probe must target the exact https://<hostname>/mcp endpoint")
if "WebException" not in https_probe or "HttpWebResponse" not in https_probe:
    raise SystemExit("HTTPS readiness must distinguish HTTP responses from TLS/network failures")
if "statusCode < 500" not in https_probe:
    raise SystemExit("HTTP auth/client responses may prove reachability, but server 5xx must fail readiness")

for state_token in ["route=", "dns=", "https="]:
    if state_token not in source:
        raise SystemExit(f"sanitized readiness diagnostics must distinguish {state_token[:-1]} state")

print("PASS: Cloudflare Named Tunnel public readiness is DNS/HTTPS bounded and fails closed")
