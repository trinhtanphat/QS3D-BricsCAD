# Work claim — Wall-junction fingerprint signed-zero canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:23:00+07:00`
- Baseline main SHA: `44b1fec832766d04530c235b7ba7185d9c111477`
- Priority: evidence-driven remote-safe generated ownership fingerprint integrity

## Reason

`WallJunctionOwnershipPlanner.BuildFingerprint` serializes junction coordinates and owner/profile elevations with `double.ToString("R", InvariantCulture)`. IEEE-754 `+0.0` and `-0.0` compare equal throughout physical matching/ordering but can retain different textual representations, so semantically identical physical junction inputs can produce different `WJF1:` rebuild fingerprints solely from the sign bit of zero.

## Intended scope

Canonicalize signed zero only at wall-junction fingerprint numeric serialization while preserving all non-zero values, group/owner identity (`WJP1`/`WJX1`), occurrence assignment, geometric tolerances, physical validation, packed-key layout and SHA-256 format.

## Changed surfaces

- `src/QS3D.Core/Geometry/WallJunctionOwnershipPlanner.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
