# MCP OpenAI tunnel trust pin restart persistence

Issue: `#6431`
Lane-Key: `mcp-openai-tunnel-trust-env-scope`
Ownership-Key: `mcp.openai-tunnel-trust-env-scope-v1`

## Problem

The official tunnel-client may be unsigned, so QS3D keeps Authenticode verification first and permits an explicit pinned SHA-256 fallback. A persisted `QS3D_OPENAI_TUNNEL_CLIENT_SHA256` can exist at Windows User scope while a newly launched BricsCAD process inherits a stale parent environment block that does not contain that variable.

Before this fix, `TryVerifyClientTrust()` read only the Process environment. The correct User-scope pin was therefore ignored after some BricsCAD restarts and Agent Center reported `WinVerifyTrust` failure even though the binary hash matched the saved pin.

## Contract

Expected SHA lookup precedence is `Process -> User -> Machine`.

- Process scope remains authoritative for explicit per-process overrides.
- User scope provides restart-safe persistence for the current Windows user.
- Machine scope is the final administrative fallback.
- Authenticode verification remains first.
- SHA fallback still requires exactly 64 hexadecimal characters and exact hash equality.
- A mismatching or malformed higher-precedence value fails closed; QS3D does not silently skip to a lower-precedence value after selection.

## Verification

Static preflight is not runtime qualification. `LOCAL_ONLY` requires an exact-SHA V25 build, deploy, BricsCAD restart from a parent process that lacks the Process-scope pin, successful reuse of the existing User-scope SHA pin, and tunnel-client READY without re-entering the Runtime API key or hash.
