# MCP admission overload response

Lane-Key: `issue-5166`
Reservation-Protocol: `v2`
Runtime scope: embedded loopback MCP admission only.

## Defect boundary

The embedded listener accepts a TCP client before acquiring one of the bounded `MaxConcurrentClients` slots. Historically, a saturated admission closed that accepted socket without writing an HTTP response. A tunnel or upstream caller can classify that reset/EOF as a raw 502 even though the embedded MCP server and BricsCAD host remain healthy.

## Contract

When the client-slot semaphore is saturated, the listener writes a best-effort bounded HTTP `503 Service Unavailable` response and closes the connection. The reject write has a 1000 ms write timeout and reuses the normal response serializer, which includes `Connection: close` and bounded headers/body.

A rejected overload client does not acquire a semaphore slot, create an MCP session, or enter the worker pool. Failure to write the 503 remains fail-closed and must not block the accept loop indefinitely.

This changes only overload transport semantics. It does not increase concurrency, broaden listener reachability, alter authentication/session contracts, or retry CAD mutations.

## Validation

`scripts/preflight-mcp-admission-overload-response.py` pins the bounded 503 rejection contract and prevents regression to a silent socket reset. Shared exact-head CI must pass auto-discovered preflights, Core checks, and locked BricsCAD V25 compile before merge. Licensed saturation testing through the real tunnel remains separate local/runtime evidence.
