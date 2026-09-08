# MCP OpenAI supervisor warm-up stability

Issue: #6063  
Runtime boundary: REMOTE_SAFE source/static/V25 compile; real OpenAI Secure MCP Tunnel + ChatGPT traffic remains LOCAL_ONLY.

## Defect

Historical live V25 MCP evidence in #5969/#5972 preserved intermittent `tunnel_client_not_connected` / polling failures after successful calls as a separate transport residual.

On the source boundary, `McpTransportSupervisor.RunOneIteration` previously mapped a successful process launch directly to `Ready`, even though OpenAI readiness is separately defined by `McpOpenAiSecureTunnelManager.IsReady`. On the next supervisor tick, a still-running child that had not yet reached `/readyz` was treated as unhealthy and sent back through `TryStartProvider`, whose manager start path replaces the current child. The manager's own watchdog is intentionally disabled while the process-global supervisor is managing, so this could create supervisor-driven restart churn during normal tunnel warm-up.

The official `openai/tunnel-client` operator contract exposes `/healthz`, `/readyz`, `/metrics`, and `/ui`; therefore this fix does not change the canonical `/readyz` probe path merely because an older live session observed a 404/polling symptom.

## Source invariant

- `OpenAiUnreadyProbeThreshold = 3` bounds the running-but-unready warm-up window to three supervisor probes.
- The first two running-but-unready probes keep health at `Starting` and do not invoke provider restart.
- The third consecutive unready probe consumes one existing restart-budget attempt before recovery restart.
- A successful `Process.Start`/provider start never publishes `Ready` by itself. `PublishStartedProviderState` rechecks `IsProviderHealthy`; unproven health remains `Starting`.
- Repeated unready generations therefore progress through the existing restart/failover budget instead of resetting the budget on every successful relaunch.
- A failover launch is also health-gated; an OpenAI fallback can remain `Starting` until readiness is proven.
- Healthy publication resets both restart and OpenAI-unready counters.
- Host stop, new autostart ownership, provider switch/failover reset the OpenAI warm-up counter.

## Preserved boundaries

- one durable transport at a time;
- Quick Tunnel remains test-only and not supervisor-failover eligible;
- Cloudflare Named Tunnel still requires the bounded public DNS + HTTPS `/mcp` health proof;
- stale owned-process cleanup remains PID + start-time + executable identity bound;
- no broad process enumeration or generic process-launch surface is added;
- no CAD mutation, command dispatch, document lock, transaction, mutation writer, OAuth credential, or MCP protocol semantics are changed.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-openai-supervisor-warmup.py
python scripts/preflight-mcp-transport-supervisor.py
```

The first guard rejects the historical launch-is-ready topology and requires running-but-unready admission before any restart. Aggregate discovered feature guards, protected `preflight`, protected `core`, and the locked V25 compile are authoritative hosted evidence for the exact candidate.

## LOCAL_ONLY follow-up

For licensed runtime qualification, use the exact merged/release descendant and verify:

1. start OpenAI Secure MCP Tunnel from Agent Center;
2. observe process remains stable while readiness transitions from RUNNING to READY;
3. confirm no 5-second relaunch loop during ordinary warm-up;
4. perform repeated ChatGPT MCP tool calls across the warm-up/steady-state boundary;
5. force or reproduce a genuinely persistent unready generation and verify bounded restart/backoff/failover behavior without claiming a remote/static PASS as licensed runtime evidence.
