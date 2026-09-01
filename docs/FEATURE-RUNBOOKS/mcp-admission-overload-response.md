# MCP admission overload response

Lane-Key: `issue-5166`
Reservation-Protocol: `v2`
Runtime scope: embedded MCP transport/admission only.

## Defect boundary

The loopback MCP server accepts at most `MaxConcurrentClients` active client handlers. Before this fix, a client arriving while all slots were occupied was silently closed before an HTTP response was written. A supervised tunnel or upstream caller can translate that reset/EOF into a raw 502 even for a lightweight transport-level call such as `connector_info`.

## Contract

A saturated listener must reject the new connection explicitly with bounded HTTP `503 Service Unavailable`, `Connection: close`, and a short `Retry-After: 1` hint. The rejection path has a short write timeout and never waits for an MCP client slot, creates an MCP session, parses a request body, or reaches CAD/QS3D execution.

The server remains loopback-only. Overload handling is fail-closed: failure to write the bounded 503 may close the accepted socket, but the normal saturation path must not intentionally use a silent reset as its protocol response.

## Validation

`scripts/preflight-mcp-admission-overload-response.py` pins the explicit overload response and requires it to occur in the `ClientSlots.Wait(0)` saturation branch before the request is queued to a worker.

Hosted CI verifies source/build contracts. A local stress test that occupies all client slots through the actual tunnel and confirms excess calls receive explicit overload semantics rather than raw 502 remains `LOCAL_ONLY` evidence.
