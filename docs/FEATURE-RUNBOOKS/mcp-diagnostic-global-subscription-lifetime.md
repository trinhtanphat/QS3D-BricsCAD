# MCP diagnostic global subscription lifetime

Issue: #6449
Lane: C04 — MCP / CAD Agent / Automation / Runtime Integration
Ownership-Key: `mcp.diagnostic-global-subscription-lifetime-v1`

## Contract

`McpDiagnosticHub` owns three process/global subscriptions: AppDomain unhandled exceptions, TaskScheduler unobserved task exceptions, and BricsCAD `DocumentBecameCurrent`. Each fallible native/managed event add must publish conservative may-be-subscribed ownership before `+=`. `Start` is authoritative only after all required subscriptions attach.

If startup partially fails, bounded cleanup may detach only those handlers whose ownership was published. A failed `-=` retains ownership and prevents a later `Start` from duplicating callbacks. `Stop` remains retryable until all global ownership is released. This cleanup retry never replays a CAD mutation.

## Validation

REMOTE_SAFE: deterministic source guard, existing diagnostic document-subscription lifetime guard, MCP production-correctness, capability-lanes, Reservation-v2, diff hygiene.

LOCAL_ONLY / NO_RESULT until actually exercised: licensed BricsCAD V25/V26 partial event add/remove behavior, disposed native wrapper timing, MDI transition timing, and visible host callback behavior. Hosted CI must not be reported as native runtime PASS.
