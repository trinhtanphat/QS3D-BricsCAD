# Work claim — Wall Mesh generated-count canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:33:00+07:00`
- Baseline main SHA: `4aa5fd4d092411573321ad0e7d74c9d0d74325fc`
- Priority: P2 — generated metadata health integrity

## Confirmed defect

`GeneratedWallMeshHealthService.Inspect(...)` accepts any `NumberStyles.Integer` spelling for `GeneratedWallMeshCount` when the parsed number matches valid handles. `StructuralWallMeshSolidBuilder.CommitSemanticUpdate(...)` always writes `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`, so `+2`, `02`, or padded text can be reported healthy even though production never emits them.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs` — count-token canonicality only
- one focused auto-registered Core smoke
- `docs/plans/2026-08-12-wall-mesh-count-canonicality.md`
- this claim file

## Contract

Canonical invariant integer counts remain accepted; missing/invalid/negative/mismatched counts retain the existing mismatch issue; numerically matching aliases become fail-visible with a dedicated warning. No other Wall Mesh health/generation behavior changes.

## Validation boundary

Source-safe regression + exact diff/readback + moving-main ancestry. No Actions/build/release/runtime PASS unless actually executed.
