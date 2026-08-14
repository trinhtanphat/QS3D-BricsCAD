# Work claim — ProjectState null persisted-scalar persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-state-null-scalar-persistability-20260814-1331`
- Registered: `2026-08-14T13:31:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `2f5f370c826d440fb60444c165a17b8119f7ac16`

## Confirmed defect

`ProjectState.DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` all delegate to `SetPersistedScalar(ref string field, string value)`. At runtime, callers can pass `null` despite the non-nullable annotation; the helper stores that null unchanged and advances persistence state. `QsdbProjectStore.Serialize(...)` then writes each field with `?? string.Empty`, while load returns a non-null string. The accepted in-memory state therefore silently changes representation during Save/Load.

The completed 2026-08-12 persisted-scalar versioning lane explicitly preserved exact non-null string storage and did not address null canonicalization. This lane preserves that contract for every non-null value.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `SetPersistedScalar` null canonicalization.
- new `tests/QS3D.Core.SmokeTests/ProjectStateNullScalarPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Canonicalize runtime null to `string.Empty` before equality comparison, version calculation and field assignment. Preserve exact contents of every non-null input, same-value no-op behavior, one-step `ChangeVersion`/`UpdatedUtc` advancement, snapshot hydration and all Floor/Zone/reference policies.

## Regression plan

Self-registering Core smoke will prove:

1. all four scalar setters expose `string.Empty` immediately after null assignment;
2. assigning null to an already-empty scalar is a no-op for ChangeVersion/UpdatedUtc;
3. non-null padded/cased text remains exact as before;
4. SaveNew -> Load keeps the canonical empty representation for all four fields.

## Non-scope

- no trimming/casing/reference validation for non-null strings;
- no ProjectState name/id changes;
- no QSDB schema/migration changes;
- no snapshot algorithm changes;
- no UI/native/Actions/runtime changes.

## Validation boundary

Remote source/read-back only; executable Core smoke, GitHub Actions and licensed BricsCAD runtime will not be claimed as PASS unless independently executed.
