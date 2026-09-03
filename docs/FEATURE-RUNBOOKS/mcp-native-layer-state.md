# MCP native layer-state API

Issue: #5455  
Lane-Key: `issue-5455`

## Contract

The native MCP layer-state surface exposes four bounded tools through the existing direct CAD runtime:

- `cad_layer_state` reads one existing layer's ON/OFF, frozen, locked and current-layer flags.
- `cad_layer_set_state` atomically changes any supplied subset of ON/OFF, frozen and locked state.
- `cad_layer_snapshot` captures an opaque, versioned, bounded token for all current layer states plus the current-layer identity.
- `cad_layer_restore` validates the complete token and atomically restores every captured layer state.

Read tools are classified as non-mutating. Write tools remain behind the existing `McpCadAgentRuntime.Mutation(...)` admission path, so `confirmMutation=true`, durable action identity, process-global `McpCadMutationCoordinator` writer ownership and emergency-stop epoch checks remain authoritative before native work starts.

## Native safety invariants

Writes use the active BricsCAD document lock and one native database transaction. Direct set rejects unknown layer names. Restore validates every captured layer before any record is opened for write, rejects duplicate or missing layers, and commits only after the complete restore set has applied.

The current layer may not be turned OFF or frozen. A restore token is bound to the captured current-layer identity; if the active current layer changed after capture, restore fails closed instead of applying a stale visibility/freeze policy to a different current layer.

Snapshots are opaque `QS3D-LAYER-STATE-V1` tokens. Layer names are individually base64 encoded before the complete snapshot is encoded, avoiding delimiter ambiguity for unusual legal names. Capture/restore is bounded to 4096 layers and a 512 KiB token.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-native-layer-state.py
```

The guard checks direct-runtime publication/routing, mutation classification, native document-lock/transaction use, validate-before-write restore ordering, current-layer fail-closed behavior, unknown/stale layer rejection and snapshot bounds/versioning.

Then run the repository protected validation surface (`preflight` and `core`). V26 consumes the V25 linked source architecture; do not create a duplicate V26 implementation.

## Licensed runtime acceptance

No remote CI result is a licensed BricsCAD `LOCAL_PASS`. A licensed local agent may optionally validate the source-ready tools by fetching the exact merged SHA, loading the matching V25/V26 build, creating at least two non-current layers, exercising read/set/snapshot/restore, changing the current layer between capture/restore to confirm fail-closed behavior, and cold-reopening the drawing if persistence evidence is required by the consuming scenario. Record exact host/version/SHA and do not infer runtime acceptance from source guards alone.
