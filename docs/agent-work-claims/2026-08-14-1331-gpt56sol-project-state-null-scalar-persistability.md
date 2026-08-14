# Work claim — ProjectState null persisted-scalar persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-state-null-scalar-persistability-20260814-1331`
- Registered: `2026-08-14T13:31:00+07:00`
- Completed: `2026-08-14T13:33:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `2f5f370c826d440fb60444c165a17b8119f7ac16`

## Confirmed defect

`ProjectState.DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` all delegate to `SetPersistedScalar(ref string field, string value)`. At runtime, callers can pass `null` despite the non-nullable annotation; the helper stored that null unchanged and advanced persistence state. `QsdbProjectStore.Serialize(...)` then writes each field with `?? string.Empty`, while load returns a non-null string. The accepted in-memory state therefore silently changed representation during Save/Load.

The completed 2026-08-12 persisted-scalar versioning lane explicitly preserved exact non-null string storage and did not address null canonicalization. This correction preserves that contract for every non-null value.

## Implemented correction

- `863555daf2f92a005f5c75728f454b4ccb5c6353` — `fix(core): canonicalize null ProjectState scalars`
  - `SetPersistedScalar` canonicalizes runtime null to `string.Empty` before equality/version logic;
  - exact contents of every non-null input remain unchanged;
  - null assigned to an already-empty scalar remains a persistence no-op.
- `0789af1d2bd5f2d1b36177a40c46484de798e7e1` — `test(core): guard null ProjectState scalar persistability`
  - covers all four null assignments;
  - protects ChangeVersion/UpdatedUtc no-op behavior;
  - protects exact non-null text semantics;
  - covers QSDB SaveNew -> Load canonical empty round-trip for all four fields.

## Validation

- Live `main` source read-back confirms `var normalizedValue = value ?? string.Empty;` precedes ordinal equality, version calculation and assignment.
- Live `main` smoke read-back confirms the self-registering null/no-op/non-null/QSDB round-trip coverage.
- Executable Core smoke: `NOT_RUN` in this connector-only lane.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD runtime: `NOT_RUN` / not applicable to this Core persistence correction.

## Non-scope preserved

- no trimming/casing/reference validation for non-null strings;
- no ProjectState name/id changes;
- no QSDB schema/migration changes;
- no snapshot algorithm changes;
- no UI/native changes.
