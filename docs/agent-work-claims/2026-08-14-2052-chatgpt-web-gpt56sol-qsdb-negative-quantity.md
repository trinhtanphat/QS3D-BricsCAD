# Work claim — QSDB negative element quantity persistence integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-20260814-qsdb-negative-quantity`
- Registered: `2026-08-14T20:52:00+07:00`
- Last Updated: `2026-08-14T20:52:00+07:00`
- Baseline main SHA: `0ea20bdc09359a286270f97a567eea9b180b2a6e`
- Priority: Core P1 persistence integrity defect found during owner-requested full-project audit
- Task Key: `PERSISTENCE-QSDB-NEGATIVE-ELEMENT-QUANTITY`
- Implementation branch: `agent/chatgpt-web-gpt56sol-20260814-qsdb-negative-quantity/qsdb-negative-quantity-persistence`
- Integration branch: `integration/chatgpt-web-gpt56sol-qsdb-negative-quantity-persistence-20260814`

## Confirmed defect

`ProjectElement.SetQuantity(...)` rejects negative physical quantities, but the public `Quantities` dictionary can still be mutated directly. `QsdbProjectStore.ValidateProject(...)` currently validates only canonical quantity names and finite values, so a directly inserted negative value can pass `Save(...)` and be serialized. On a later `Load(...)`, the persisted negative value reaches `SetQuantity(...)` and throws `ArgumentOutOfRangeException`; `LoadWithBackupFallback(...)` intentionally treats data-shape failures such as `InvalidDataException` as recoverable, so this domain exception can bypass an otherwise valid `.bak` recovery path.

The current-schema XML validator also validates quantity shape/name but not the numeric value domain, so an already-corrupt QSDB is not rejected at the persistence semantic boundary before domain materialization.

## Reserved scope

Fail closed on negative persisted element quantities at both persistence boundaries without changing the domain model or schema version:

- reject negative in-memory element quantities from `QsdbProjectStore.ValidateProject(...)` before save publication;
- reject negative current-schema `<q value="...">` entries from `QsdbProjectXmlSchemaValidator.ValidateCurrent(...)` with `InvalidDataException` before materialization;
- add focused deterministic Core smoke coverage for save preflight and backup fallback recovery while preserving zero/positive quantity behavior.

## Owned surfaces

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs`
- this claim file

## Explicit exclusions / concurrency protection

- Do **not** modify `ProjectElement` or quantity calculation/rule semantics.
- No schema-version bump or migration format change.
- No reporting, interchange, Undo, Grid, Slab/Open, Release, Health, BricsCAD adapter/runtime or workflow changes.
- Do not overlap active claims on unrelated persistence/semantic surfaces.
- No force-push and no manual GitHub Actions dispatch/rerun.

## Acceptance / validation plan

- Direct `element.Quantities[...] = -1d` followed by QSDB save fails with `InvalidDataException` before publishing invalid state.
- A current-schema QSDB containing a negative persisted element quantity fails as `InvalidDataException` at the XML semantic boundary, allowing `LoadWithBackupFallback(...)` to recover from a valid `.bak`.
- Zero and positive element quantities retain existing round-trip behavior.
- Review final diff against refreshed `main` and read back landed source.
- Source-safe smoke/build execution is reported only if actually available; no native BricsCAD PASS will be claimed from this connector-only environment.

## Completion condition

One reconciled source landing reaches current `main`, focused regression source is present, remote ancestry/readback is verified, and this claim is closed with exact commit evidence.
