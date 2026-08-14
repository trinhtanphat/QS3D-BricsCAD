# Work claim — ProjectState active-context persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-active-context-persistability-20260814-1513`
- Registered: `2026-08-14T15:13:00+07:00`
- Completed: `2026-08-14T15:18:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline main SHA: `79a9706c49ef84beea8efb5d1bf5fe09472713b9`
- Claim commit: `c1413daca35dfd611d1ba4d24b015fa4b68bc5c3`
- Claim reconciliation: `855ecc7868d8b1455908a96af7b5c44b511896c6`
- Pre-write source blob: `0332848f6afc48871c95fbec962fe1129aa4ec27`
- Source: `0a06a9e61929d80866bd7f48021a46f0b1dde7fb`
- Regression: `eb283eda155a05aee8047da4340e07cf37ec6eaf`

## Confirmed defect

`ProjectState.ActiveZoneId` and `ActiveFloorId` are persisted relation identities, but their public setters shared the free-text `SetPersistedScalar` path used by `DrawingPath` and `DrawingFingerprint`. Non-null values were therefore preserved exactly, including leading/trailing whitespace and embedded control characters.

That representation was not persistable: QSDB save validation requires active zone/floor identities to be optional canonical values and the current XML schema validates the same relation tokens before load. Supported domain mutation could therefore succeed and advance `ChangeVersion`, only for save/publication to fail later.

The completed ProjectState null-scalar lane explicitly kept trimming/casing/reference validation for non-null strings out of scope. This was a distinct lexical-persistability boundary and preserved the exact-text contract for drawing path/fingerprint.

## Completed change

- Routed only `ActiveZoneId` and `ActiveFloorId` through a dedicated `SetActiveContextId` helper.
- Runtime null remains canonical `string.Empty`.
- Non-null active identity input is trimmed before equality/version mutation.
- Embedded control characters are rejected before field, `ChangeVersion`, or `UpdatedUtc` mutation.
- Canonically equivalent padded assignments become semantic no-ops through the existing `SetPersistedScalar` equality/version path.
- `DrawingPath` and `DrawingFingerprint` remain on the exact-text `SetPersistedScalar` contract.
- No referential-existence check was added to the setters; QSDB save remains responsible for orphan-reference validation.

## Regression coverage

Added self-registering `ProjectStateActiveContextPersistabilitySmoke` which pins:

1. padded ActiveZoneId/ActiveFloorId normalize to canonical stored identities;
2. padded equivalent re-assignment is a no-op for `ChangeVersion` and `UpdatedUtc`;
3. embedded `U+0001` is rejected atomically for both setters;
4. null still clears each active identity to `string.Empty`;
5. DrawingPath/DrawingFingerprint continue preserving exact non-null text.

## Validation

Remote GitHub source diff for `0a06a9e61929d80866bd7f48021a46f0b1dde7fb` confirms exactly two setter callsite substitutions and one dedicated helper; no free-text scalar, Zone/Floor definition, persistence loader/schema, UI, or native surface was modified by the source commit.

Remote regression diff/readback for `eb283eda155a05aee8047da4340e07cf37ec6eaf` confirms the focused self-registering smoke with C# `\u0001` literals and no external/native fixture dependency. GitHub compare reports the regression SHA is ahead of source SHA `0a06a9e61929d80866bd7f48021a46f0b1dde7fb`, with the source SHA as merge base. At the final close gate remote `main` was exactly the regression SHA and source readback still contained the intended helper/setter contract.

A pre-existing V25 cloud workflow run #160 had succeeded earlier on commit `6d834dbadc4c13ce4f7966fbaea00cf1ec8499bb`; that is historical cloud evidence only and predates this source change. This lane did not dispatch or rerun GitHub Actions.

Executable .NET smoke/build and licensed BricsCAD/native validation were **not run by this lane** in the connector-only environment, so no fresh managed/native PASS is claimed.

## Explicit non-scope

- no DrawingPath/DrawingFingerprint normalization or validation changes;
- no active zone/floor referential-existence enforcement in setters;
- no ZoneDefinition/FloorDefinition changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes.

## Completion condition

Satisfied: claim-first reservation, live baseline reconciliation, isolated supported-writer fix, focused regression source, remote readback/ancestry verification, explicit validation limits, and completed claim metadata are present on `main`.
