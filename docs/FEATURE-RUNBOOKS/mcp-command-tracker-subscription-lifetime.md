# MCP command tracker native subscription lifetime

Issue: #6419
Lane: C04 — MCP / CAD Agent / Automation / Runtime Integration

## Failure mode
McpCadViewStatusRuntime.CommandTracker subscribes to four native BricsCAD Document command lifecycle events. Native event accessors are fallible native-wrapper boundaries: an add/remove may throw after the host has already changed subscription state. Losing the tracker object after partial attach/detach can therefore retain callbacks on a stale/disposed document with no cleanup owner.

## Contract
- Publish the tracker in the authoritative registry before the first fallible native add.
- Publish each handler's may-be-subscribed bit before its native +=.
- Enable callbacks only after the complete attach sequence succeeds.
- Callback delivery must match the exact authoritative tracker object and document generation.
- Native remove failure retains ownership so a later bounded cleanup attempt can retry.
- Registry removal is permitted only after full detach is proven.
- Never retry or replay CAD mutation as part of repair.

## Validation boundary
REMOTE_SAFE: deterministic source guard, MCP production/capability guards, Reservation-v2, diff hygiene and admitted-reference compilation when available.

LOCAL_ONLY / NO_RESULT until exercised in licensed BricsCAD V25/V26: partial event add/remove fault timing, disposed-wrapper behavior, MDI teardown/replacement timing and native callback delivery.
