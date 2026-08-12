# Work claim — Slab Mesh generated-count canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:30:00+07:00`
- Baseline main SHA: `e1aca3ee993164a6264b6c84837c34671b8c949e`
- Priority: P2 — generated metadata health integrity

## Confirmed defect

`GeneratedSlabMeshHealthService.Inspect(...)` parses `GeneratedSlabMeshCount` with `NumberStyles.Integer` and reports a mismatch only when parsing fails, the value is negative, or the numeric value differs from the valid handle count. `SlabMeshSolidBuilder.CommitSemanticUpdate(...)` always emits `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`, so numerically matching aliases such as `+2`, `02`, or padded text can be false-clean even though production never writes them.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs` — count-token canonicality only
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-slab-mesh-count-canonicality.md`
- this claim file

## Contract

1. Canonical non-negative invariant integer count text remains accepted.
2. Invalid/missing/count-mismatch behavior remains unchanged.
3. Numerically matching but noncanonical count text becomes fail-visible with a dedicated warning.
4. All handle, ownership, liveness, geometry metadata, footprint, category and stale semantics remain unchanged.
5. No CAD runtime, release, build, or GitHub Actions changes.

## Validation boundary

Focused source smoke, exact diff/readback, and moving-main ancestry verification. No executable smoke, build, Actions, or licensed BricsCAD runtime PASS unless actually executed.
