# Work claim — ProjectState active-context persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-active-context-persistability-20260814-1513`
- Registered: `2026-08-14T15:13:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `427d029ad834197a43ddfa302e36128334af5ae4`
- Pre-write source blob: `0332848f6afc48871c95fbec962fe1129aa4ec27`

## Confirmed defect

`ProjectState.ActiveZoneId` and `ActiveFloorId` are persisted relation identities, but their public setters currently share the free-text `SetPersistedScalar` path used by `DrawingPath` and `DrawingFingerprint`. Non-null values are therefore preserved exactly, including leading/trailing whitespace and embedded control characters.

That representation is not persistable: QSDB save validation requires active zone/floor identities to be optional canonical values and the current XML schema validates the same relation tokens before load. Supported domain mutation can therefore succeed and advance `ChangeVersion`, only for save/publication to fail later.

The completed ProjectState null-scalar lane explicitly kept trimming/casing/reference validation for non-null strings out of scope. This claim is a distinct lexical-persistability boundary and preserves the exact-text contract for drawing path/fingerprint.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `ActiveZoneId` / `ActiveFloorId` setter normalization and a dedicated helper.
- new `tests/QS3D.Core.SmokeTests/ProjectStateActiveContextPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Canonicalize active-context assignments before mutation: runtime null remains `string.Empty`; non-empty input is trimmed; embedded control characters are rejected before field/version/timestamp mutation. Assigning a padded representation of the already-canonical identity must remain a semantic no-op.

Keep `DrawingPath` and `DrawingFingerprint` on the existing exact `SetPersistedScalar` path. Do not add referential-existence validation to the setters because catalogs may be assembled in stages and QSDB save already owns orphan-reference validation.

## Regression plan

Focused self-registering Core smoke will prove:

1. padded ActiveZoneId/ActiveFloorId normalize to canonical stored identities;
2. padded equivalent re-assignment is a no-op for `ChangeVersion` and `UpdatedUtc`;
3. embedded `U+0001` is rejected atomically for both setters;
4. null still clears each active identity to `string.Empty`;
5. DrawingPath/DrawingFingerprint continue preserving exact non-null text.

## Explicit non-scope

- no DrawingPath/DrawingFingerprint normalization or validation changes;
- no active zone/floor referential-existence enforcement in setters;
- no ZoneDefinition/FloorDefinition changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes;
- no GitHub Actions dispatch or licensed BricsCAD qualification.

## Validation boundary

Use remote GitHub diff/readback and ancestry verification. A recent existing V25 cloud workflow may be cited only as cloud evidence; this lane will not dispatch Actions and will not claim executable .NET/native PASS without an independently executed runner.
