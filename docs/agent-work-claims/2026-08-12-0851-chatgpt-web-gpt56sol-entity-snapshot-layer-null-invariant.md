# Work claim — EntitySnapshot Layer null invariant

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-layer-null-invariant-20260812-0851`
- Registered: `2026-08-12T08:51:00+07:00`
- Baseline main SHA: `78a298e7e509c2de65f3efb638016f0a5adc448a`
- Priority: P2 Core model invariant hardening

## Confirmed defect

`EntitySnapshot` normalizes a null constructor `layer` to `string.Empty`, but its public `Layer` auto-property setter can later accept a runtime null value and leave the snapshot in a state the constructor explicitly prevents. This creates an inconsistent public invariant for downstream snapshot consumers.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotLayerNullInvariantSmoke.cs`
- this claim file

## Excluded scope

No Handle/EntityType changes, metric semantics, Metadata redesign, CAD adapter/UI/runtime changes, build/release changes, or GitHub Actions.

## Plan

1. Preserve the constructor contract by normalizing every `Layer` assignment through a backing field (`null` to `string.Empty`) without trimming valid layer text.
2. Add focused Core smoke coverage for constructor null normalization, runtime null reassignment, and ordinary layer assignment.
3. Re-fetch moving `main`, verify no overlap, merge with head locking, and mark this claim `COMPLETED` with exact integration evidence.

No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed by this connector-only lane.
