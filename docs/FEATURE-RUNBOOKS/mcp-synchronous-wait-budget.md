# MCP synchronous wait response budget

Lane-Key: `issue-5168`
Reservation-Protocol: `v2`
Runtime scope: synchronous MCP desktop and diagnostic waits only.

## Defect boundary

Some MCP calls were intentionally allowed to stay synchronous longer than the external request budget: `diagnostics_wait` and `desktop_wait_for_window` allowed 15 seconds, while `desktop_sequence` allowed 30 seconds. A healthy local runtime can therefore still appear as raw 502 when the tunnel/edge abandons the request before QS3D serializes the result.

## Contract

Every synchronous wait covered by this lane has a maximum of `7000 ms`. Existing 5000 ms defaults remain 5000 ms. The descriptors/schema advertise the same maximum so callers do not plan requests that the transport cannot safely finish.

`desktop_sequence` remains fail-fast, retains local desktop-consent and mutation/emergency-stop revalidation, and does not roll back already completed steps. Workflows requiring more than one response-budget window must be chunked into multiple MCP calls with state re-inspection between calls rather than one 15–30 second request.

`diagnostics_wait` remains a bounded long-poll and may return normally with `timedOut=true`; it is not converted into an unbounded event stream.

## Validation

`scripts/preflight-mcp-synchronous-wait-budget.py` pins the 7000 ms maxima, 5000 ms defaults, schemas, and sequence fail-fast contract. Shared CI must pass feature guards, Core checks, and locked BricsCAD V25 compile on the exact head.

A live ChatGPT/tunnel test that requests each maximum wait and confirms a normal MCP response rather than raw 502 remains `LOCAL_ONLY` evidence.
