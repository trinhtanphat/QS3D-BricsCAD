# MCP direct layout active-document affinity

## Scope
C04 direct `LAYOUT/-LAYOUT NEW|SET|DELETE` execution in `McpCadDirectModelRuntime`.

## Failure boundary
`LayoutManager.Current` is process-global/current-document state. A captured `Document` and `DocumentLock` are not sufficient if the active document changes before a `LayoutManager.Current` call or before result capture.

## Required contract
- Resolve the target document in CAD application context.
- Require idle + automation-running before native mutation.
- Hold the captured document lock for the complete layout operation.
- Revalidate exact active-document identity inside the lock before the first `LayoutManager.Current` call.
- Revalidate again after mutation before reading `CurrentLayout`.
- Capture `CurrentLayout` while the same document affinity and lock are still owned.
- Fail closed on document replacement; never retry or replay the layout mutation automatically.

## Validation
Run `python scripts/preflight-mcp-direct-layout-active-document-affinity.py` plus save/layout, production-correctness, capability-lanes and Reservation-v2 preflights.

## Runtime classification
Source/static validation is `REMOTE_SAFE`. Modeless multi-DWG switch timing requires licensed BricsCAD V25/V26 and remains `LOCAL_ONLY / NO_RESULT` until executed on a real host.
