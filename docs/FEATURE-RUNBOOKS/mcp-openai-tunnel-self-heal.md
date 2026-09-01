# OpenAI MCP tunnel runtime self-heal

Lane-Key: `issue-5156`
Reservation-Protocol: `v2`
Runtime scope: MCP transport/runtime only.

## Defect boundary

A raw upstream `502` for `connector_info` means the request did not reach the embedded MCP tool dispatcher. The source gap addressed here is the OpenAI Secure MCP Tunnel supervisor: startup/autostart existed, but an unexpected process exit or a persistent unready tunnel after startup had no ongoing recovery loop. This is independent of A00/master-drawing work and does not change QS3D business-domain behavior.

## Recovery contract

The OpenAI transport owns one bounded watchdog only while all of these remain true: OpenAI Secure MCP Tunnel is the selected provider, persisted autostart is ON, the tunnel remains configured, and the host has not entered an explicit stop path.

The watchdog probes at a bounded cadence. One transient failed readiness probe never restarts the child. A persistent unready state must cross the configured consecutive-failure threshold before recovery is attempted. An unexpected process exit is eligible for recovery on the next watchdog pass. Recovery is single-flight and uses bounded exponential restart backoff so a prolonged control-plane or network outage cannot create a tight restart loop.

Recovery always goes back through `Start(SavedTunnelId, string.Empty, out message)`. That preserves tunnel-client trust verification, reconstructs the local MCP endpoint/config, and resolves the Runtime API key from the existing verified environment / Windows Credential Manager path. The watchdog never serializes a Runtime API key into config, diagnostics, or source state.

## Stop semantics

An explicit Stop disables the watchdog before stopping the tunnel process and persists autostart OFF. The host shutdown path also disables the in-process watchdog before terminating the child, while preserving the existing persisted autostart choice for the next normal launch. Recovery re-checks the selected provider/autostart/configuration before restart, so revocation or a provider switch fails closed instead of resurrecting the OpenAI tunnel.

## Validation

`scripts/preflight-mcp-openai-tunnel-self-heal.py` pins the watchdog lifecycle, single-flight gate, consecutive-unready threshold, retry backoff, secure restart path, and stop semantics. Shared CI must pass the discovered feature guards, Core checks, and locked BricsCAD V25 compile on the exact PR head.

Licensed interactive proof that killing the real tunnel-client causes the public ChatGPT MCP route to recover, and that `connector_info` becomes reachable again without restarting BricsCAD, remains `LOCAL_ONLY` evidence and is not claimed by hosted CI.
