#!/usr/bin/env python3
"""Fail closed unless OAuth one-time credentials retain bounded replay protection."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOAuthAuthorizationServer.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP OAuth consumed replay-cache preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

# Authorization codes are short-lived one-time credentials. Their consumed hashes may
# be forgotten only after expiry, and cache pressure must fail closed rather than evict
# a still-live replay marker.
for needle in [
    "private const int MaxConsumedCredentialEntries =",
    "private static readonly object ConsumedCredentialSync = new object();",
    "TryRememberConsumedCredential(ConsumedAuthorizationCodes",
    "private static ConsumedCredentialAdmission TryRememberConsumedCredential(",
    "lock (ConsumedCredentialSync)",
    "pair.Value <= now",
    "cache.Count >= MaxConsumedCredentialEntries",
    "ConsumedCredentialAdmission.CapacityExceeded",
    "ConsumedCredentialAdmission.Replay",
    "authorization code replay cache is at capacity",
    "authorization code was already used",
]:
    if needle not in source:
        fail(f"missing fail-closed authorization-code replay contract: {needle}")

# The historical pressure cleanup deliberately removed arbitrary live entries after the
# cache crossed 1024, reopening replay for still-valid one-time credentials.
for forbidden in [
    "if (ConsumedAuthorizationCodes.Count <= 1024) return;",
    "if (ConsumedAuthorizationCodes.Count <= 768) break;",
]:
    if forbidden in source:
        fail(f"live consumed authorization codes can still be evicted under pressure: {forbidden}")

helper_start = source.find("private static ConsumedCredentialAdmission TryRememberConsumedCredential(")
helper_end = source.find("private static long UnixNow()", helper_start)
if helper_start < 0 or helper_end < 0:
    fail("bounded authorization-code replay admission helper is missing")
helper = source[helper_start:helper_end]
if helper.count("TryRemove(") != 1:
    fail("authorization-code replay cache may remove entries outside expired-entry cleanup")
for needle in [
    "if (pair.Value <= now)",
    "if (cache.ContainsKey(hash)) return ConsumedCredentialAdmission.Replay;",
    "if (cache.Count >= MaxConsumedCredentialEntries) return ConsumedCredentialAdmission.CapacityExceeded;",
    "if (!cache.TryAdd(hash, expiry)) return ConsumedCredentialAdmission.Replay;",
    "return ConsumedCredentialAdmission.Added;",
]:
    if needle not in helper:
        fail(f"bounded authorization-code admission ordering drifted: {needle}")

# Refresh rotation must not retain one cache entry per consumed token for the full
# 30-day token lifetime: that self-DoSes after 1024 legitimate rotations. Instead,
# signed refresh tokens carry a stable random family id and monotonic generation, while
# one process-global map stores only the next accepted generation for each active family.
for needle in [
    "private const int MaxRefreshTokenFamilies = 1024;",
    "private static readonly Dictionary<string, RefreshFamilyState> RefreshTokenFamilies",
    "private static readonly object RefreshFamilySync = new object();",
    "private sealed class RefreshFamilyState",
    "internal long NextGeneration;",
    "internal long Expiry;",
    "fields.Length != 9",
    "TryDecodeField(fields[7], out refreshFamilyId)",
    "long.TryParse(fields[8], NumberStyles.None, CultureInfo.InvariantCulture, out refreshGeneration)",
    "TryAdvanceRefreshFamily(refreshFamilyId, refreshGeneration",
    "private static RefreshFamilyAdmission TryAdvanceRefreshFamily(",
    "lock (RefreshFamilySync)",
    "refreshGeneration != 0",
    "state.NextGeneration != refreshGeneration",
    "state.NextGeneration = checked(refreshGeneration + 1)",
    "RefreshTokenFamilies.Count >= MaxRefreshTokenFamilies",
    "refresh token family capacity is exhausted",
    "refresh token was already used",
]:
    if needle not in source:
        fail(f"missing bounded refresh-family rotation contract: {needle}")

for forbidden in [
    "ConsumedRefreshTokens",
    "TryRememberConsumedCredential(ConsumedRefreshTokens",
    "refresh token replay cache is at capacity",
]:
    if forbidden in source:
        fail(f"refresh rotation still depends on per-consumed-token retention: {forbidden}")

# Initial offline grants mint generation zero with a fresh family. Successors preserve
# the same family and carry the exact generation admitted under the process-global lock.
for needle in [
    "CreateRandomToken(24)",
    "refreshFamilyId ?? CreateRandomToken(24)",
    "refreshGeneration.ToString(CultureInfo.InvariantCulture)",
    "EncodeField(refreshFamily)",
]:
    if needle not in source:
        fail(f"refresh token issuance lost family/generation binding: {needle}")

# A refresh generation must not be consumed before the successor response has been
# constructed successfully. Otherwise an exception while signing/serializing N+1 burns
# generation N without returning N+1, and the client's only safe retry is rejected as a
# replay. Compute the checked successor generation and prepare the successor response
# before the single process-global family admission/advance.
exchange_start = source.find("private static McpOAuthHttpResponse ExchangeRefreshToken(")
exchange_end = source.find("private static McpOAuthHttpResponse IssueTokenPair(", exchange_start)
if exchange_start < 0 or exchange_end < 0:
    fail("refresh-token exchange method is missing")
exchange = source[exchange_start:exchange_end]
checked_pos = exchange.find("checked(refreshGeneration + 1)")
issue_pos = exchange.find("IssueTokenPair(")
admit_pos = exchange.find("TryAdvanceRefreshFamily(")
if checked_pos < 0:
    fail("successor generation is not checked before refresh-family advancement")
if issue_pos < 0:
    fail("successor token response is not prepared in the refresh exchange")
if admit_pos < 0:
    fail("refresh-family admission is missing from the refresh exchange")
if not (checked_pos < issue_pos < admit_pos):
    fail("refresh family advances before successor response preparation, which can strand the client on issuance failure")
if "return successorResponse;" not in exchange:
    fail("prepared successor response is not returned after successful refresh-family admission")

# Hostile but correctly signed refresh payloads can carry Int64.MaxValue as the generation.
# Never let the checked successor increment escape as OverflowException through the MCP
# HTTP boundary: reject the exhausted generation deterministically before arithmetic.
max_generation_pos = exchange.find("if (refreshGeneration == long.MaxValue)")
if max_generation_pos < 0:
    fail("refresh generation exhaustion is not rejected before checked successor arithmetic")
if max_generation_pos > checked_pos:
    fail("refresh generation exhaustion guard runs after checked successor arithmetic")
if "refresh token generation is exhausted" not in exchange:
    fail("refresh generation exhaustion does not return a stable non-sensitive OAuth error")

# Family retention must expire at exactly the same instant encoded in the successor
# refresh token. Independent clock reads can straddle a one-second boundary, prune the
# family state before the still-valid signed token expires, and turn a valid generation
# into a false replay. Capture one rotation timestamp and propagate it into token issue.
if exchange.count("UnixNow()") != 1:
    fail("refresh rotation must capture exactly one timestamp for successor issuance and family retention")
for needle in [
    "var rotationIssuedAt = UnixNow();",
    "var familyExpiry = rotationIssuedAt + (long)RefreshTokenLifetime.TotalSeconds;",
    "successorGeneration,\n                rotationIssuedAt);",
]:
    if needle not in exchange:
        fail(f"refresh family expiry is not bound to successor token issuance time: {needle}")

issue_pair_start = exchange_end
issue_pair_end = source.find("private static RefreshFamilyAdmission TryAdvanceRefreshFamily(", issue_pair_start)
if issue_pair_end < 0:
    fail("refresh-family admission helper is missing after token issuance")
issue_pair = source[issue_pair_start:issue_pair_end]
for needle in [
    "long? issuedAt = null",
    "var now = issuedAt ?? UnixNow();",
]:
    if needle not in issue_pair:
        fail(f"token issuance cannot consume the rotation timestamp: {needle}")

print("MCP OAuth consumed replay-cache preflight passed.")
sys.exit(0)
