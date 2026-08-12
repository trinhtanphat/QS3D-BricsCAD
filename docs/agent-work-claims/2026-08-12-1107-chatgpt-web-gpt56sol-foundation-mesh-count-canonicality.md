# Work claim — Foundation Mesh generated-count canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:07:00+07:00`
- Baseline main SHA: `bef7b5a543d3ec799076de3a2510a89329d161c8`
- Priority: P2 — generated metadata health integrity

## Confirmed defect

`GeneratedFoundationMeshHealthService.Inspect(...)` parses `GeneratedFoundationMeshCount` with `NumberStyles.Integer` and only reports a mismatch when parsing fails, the value is negative, or the numeric value differs from the valid handle count. Writer-owned Foundation Mesh metadata is always emitted as `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`, so aliases such as `+2`, `02`, or surrounding whitespace can be accepted as healthy even though production never writes them.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs` — count-token canonicality only
- one focused auto-registered Core smoke under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-foundation-mesh-count-canonicality.md`
- this claim file

## Contract

1. Canonical non-negative invariant integer count text remains accepted.
2. Invalid/missing/count-mismatch behavior remains unchanged.
3. A numerically matching but noncanonical count token is fail-visible with a dedicated health issue.
4. Handles, ownership, liveness, dimensions, spacing, cover, faces, mode, footprint, category and stale semantics remain unchanged.
5. No BricsCAD runtime, build/release, or GitHub Actions changes.

## Validation boundary

Focused source smoke, exact diff/readback, and moving-main ancestry verification. No executable smoke, full build, Actions, or licensed BricsCAD runtime PASS is claimed unless actually executed.
