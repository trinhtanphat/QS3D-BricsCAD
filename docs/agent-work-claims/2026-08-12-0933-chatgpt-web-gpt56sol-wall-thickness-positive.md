# Work claim — Wall property positive thickness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `d00993c683c183ff1ec0e35128f79c2eadaa59c8`
- Priority: evidence-driven remote-safe domain geometry invariant

## Reason

`WallPropertySet.ThicknessMm` currently requires only a finite value, so zero or negative physical thickness can be stored in the public wall property model. The CAD-independent `WallFootprintEngine.Build` contract rejects `thickness <= 0`, making those property values guaranteed-invalid downstream geometry inputs rather than meaningful signed offsets.

## Intended scope

Require `ThicknessMm > 0` while preserving finite signed semantics for axis offsets and vertical offsets, existing defaults, property names and downstream wall geometry behavior.

## Changed surfaces

- `src/QS3D.Core/Domain/WallPropertySet.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
