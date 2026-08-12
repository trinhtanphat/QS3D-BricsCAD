# Work claim — Material catalog empty-record integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:28:00+07:00`
- Baseline main SHA: `07f56571314e63d606b3c1348fd611ee01426abd`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectMaterialCatalog.WriteCustom()` serializes custom materials as exactly one non-empty Base64 record per line, joined with `\n`. `ReadCustom()` split with `StringSplitOptions.RemoveEmptyEntries`, so a tampered persisted catalog containing an empty record line or trailing blank record was silently repaired during read instead of being rejected as non-canonical/corrupted state.

## Reserved scope

Reject empty persisted material-catalog record lines while preserving valid one-record-per-line encoding, strict UTF-8 decoding, material limits, ordering, built-in shadowing checks, Unicode behavior, and all catalog mutation semantics. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogEmptyRecordSmoke.cs`
- this claim file

## Excluded scope

- No changes to Material Catalog UI, material reference rename/delete semantics, XLSX export, Family/Instance material behavior, or BricsCAD V25 runtime.
- No change to valid persisted material record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Create valid catalog metadata via public `UpsertCustom()`, inject an empty record line in the persisted metadata, and assert `GetCustom()` fails closed.
- Assert the untouched canonical metadata still round-trips the same custom material.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The prior strict UTF-8 material catalog claim is `COMPLETED`. No current Material Catalog claim was found; this lane only hardened empty persisted record handling.

## Completion

- Implementation commits:
  - `f1867abc6ce7f7aa06b2240d31229712aef16482` — preserve empty lines during split and reject empty/whitespace persisted catalog records explicitly.
  - `58eb62f880f18564d06a8d77a9d6038438de4ea1` — add injected-empty-record regression and canonical catalog round-trip coverage.
- Final observed `main` before claim close: `8d43cb9016699b39118a08fd9a1238ec21516eb7`.
- Validation actually performed:
  - re-fetched `ReadCustom()` from current `main` and confirmed `StringSplitOptions.None` plus explicit empty-record rejection are present;
  - re-fetched the smoke and confirmed canonical metadata created via `UpsertCustom()` fails only after `\n\n` corruption is injected, while the untouched catalog remains readable;
  - strict UTF-8 decoding from the prior completed material lane remains intact;
  - preserved a concurrent `FamilyId` canonicalization change already present on latest `main` when updating this file;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-catalog integrity hardening.

## Completion condition

Satisfied: current `main` rejects empty persisted material catalog records, preserves canonical catalog round-trips, includes focused regression coverage, and this claim is released as `COMPLETED`.
