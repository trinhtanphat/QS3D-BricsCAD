#!/usr/bin/env python3
"""Fail closed unless consumed OAuth credentials remain replay-blocked until expiry."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOAuthAuthorizationServer.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP OAuth consumed replay-cache preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

for needle in [
    "private const int MaxConsumedCredentialEntries =",
    "private static readonly object ConsumedCredentialSync = new object();",
    "TryRememberConsumedCredential(ConsumedAuthorizationCodes",
    "TryRememberConsumedCredential(ConsumedRefreshTokens",
    "private static ConsumedCredentialAdmission TryRememberConsumedCredential(",
    "lock (ConsumedCredentialSync)",
    "pair.Value <= now",
    "cache.Count >= MaxConsumedCredentialEntries",
    "ConsumedCredentialAdmission.CapacityExceeded",
    "ConsumedCredentialAdmission.Replay",
]:
    if needle not in source:
        fail(f"missing fail-closed replay-cache contract: {needle}")

# The historical pressure cleanup deliberately removed arbitrary live entries after the
# cache crossed 1024, reopening replay for still-valid one-time credentials.
for forbidden in [
    "if (ConsumedAuthorizationCodes.Count <= 1024) return;",
    "if (ConsumedRefreshTokens.Count <= 1024) return;",
    "if (ConsumedAuthorizationCodes.Count <= 768) break;",
    "if (ConsumedRefreshTokens.Count <= 768) break;",
]:
    if forbidden in source:
        fail(f"live consumed credentials can still be evicted under pressure: {forbidden}")

helper_start = source.find("private static ConsumedCredentialAdmission TryRememberConsumedCredential(")
helper_end = source.find("private static long UnixNow()", helper_start)
if helper_start < 0 or helper_end < 0:
    fail("bounded consumed-credential admission helper is missing")
helper = source[helper_start:helper_end]

# Only expired entries may be forgotten. Capacity must fail closed before adding a new
# consumed hash, and the whole cleanup/check/add sequence must be serialized.
if helper.count("TryRemove(") != 1:
    fail("replay cache helper may remove entries outside expired-entry cleanup")
for needle in [
    "if (pair.Value <= now)",
    "if (cache.ContainsKey(hash)) return ConsumedCredentialAdmission.Replay;",
    "if (cache.Count >= MaxConsumedCredentialEntries) return ConsumedCredentialAdmission.CapacityExceeded;",
    "if (!cache.TryAdd(hash, expiry)) return ConsumedCredentialAdmission.Replay;",
    "return ConsumedCredentialAdmission.Added;",
]:
    if needle not in helper:
        fail(f"bounded replay admission ordering drifted: {needle}")

# Exchange paths must distinguish replay from saturation rather than treating cache
# pressure as permission to forget prior one-time use.
for diagnostic in [
    "authorization code replay cache is at capacity",
    "refresh token replay cache is at capacity",
    "authorization code was already used",
    "refresh token was already used",
]:
    if diagnostic not in source:
        fail(f"missing stable replay/capacity diagnostic: {diagnostic}")

print("MCP OAuth consumed replay-cache preflight passed.")
sys.exit(0)
