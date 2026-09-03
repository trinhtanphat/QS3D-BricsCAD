#!/usr/bin/env python3
"""Fail closed unless the local MCP client keeps the bearer on the exact embedded loopback endpoint."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpLocalAgentClient.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP local client loopback preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

for needle in [
    "private static void ValidateLocalEndpoint(Uri endpoint)",
    "var expected = McpEmbeddedServer.Endpoint;",
    "endpoint == null",
    "!endpoint.IsAbsoluteUri",
    "!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)",
    "!endpoint.IsLoopback",
    "!string.IsNullOrEmpty(endpoint.UserInfo)",
    "!string.Equals(endpoint.Host, expected.Host, StringComparison.OrdinalIgnoreCase)",
    "endpoint.Port != expected.Port",
    "!string.Equals(endpoint.AbsolutePath, \"/mcp\", StringComparison.Ordinal)",
    "!string.IsNullOrEmpty(endpoint.Query)",
    "!string.IsNullOrEmpty(endpoint.Fragment)",
    "Local MCP endpoint must match the current embedded loopback http://.../mcp endpoint.",
    "request.AllowAutoRedirect = false;",
]:
    if needle not in source:
        fail(f"missing local credential-boundary contract: {needle}")

send_start = source.find("private static LocalHttpResult Send(")
if send_start < 0:
    fail("Send transport method is missing")
send_end = source.find("private static string ReadBoundedResponseBody(", send_start)
if send_end < 0:
    fail("bounded-response helper boundary is missing")
send = source[send_start:send_end]

validation = send.find("ValidateLocalEndpoint(endpoint);")
request_create = send.find("WebRequest.Create(endpoint)")
bearer = send.find("McpEmbeddedServer.GetBearerToken()")
redirect = send.find("request.AllowAutoRedirect = false;")
if min(validation, request_create, bearer, redirect) < 0:
    fail("Send is missing validation/request/bearer/redirect boundary")
if not (validation < request_create < redirect < bearer):
    fail("exact-endpoint validation and redirect refusal must precede local bearer attachment")

# Preserve the already-hardened timeout, response-size and strict UTF-8 boundaries.
for needle in [
    "request.Timeout = timeoutMilliseconds;",
    "request.ReadWriteTimeout = timeoutMilliseconds;",
    "private const int MaxResponseBytes = 4 * 1024 * 1024;",
    "ReadBoundedResponseBody(stream, response.ContentLength)",
    "private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);",
]:
    if needle not in source:
        fail(f"existing bounded local-client safety contract drifted: {needle}")

print("MCP local client exact-loopback auth boundary preflight passed.")
sys.exit(0)
