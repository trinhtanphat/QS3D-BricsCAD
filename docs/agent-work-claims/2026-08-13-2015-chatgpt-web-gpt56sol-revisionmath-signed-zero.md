# Work claim — RevisionMath signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revisionmath-signed-zero-20260813-2015`
- Registered: `2026-08-13T20:15:00+07:00`
- Baseline main SHA: `579f2cded8c18a5581509157ad5e4db88db72939`
- Priority: revision numeric canonicality hardening

## Confirmed defect

`RevisionMath.Finite(...)` rejects non-finite values but returns IEEE signed zero unchanged. `Add(...)`, `Subtract(...)`, and `Percent(...)` likewise return arithmetic zero without canonicalizing its sign bit. These shared helpers feed canonical revision capture/report paths: `RevisionService.Capture(...)` stores `Finite(quantity.Value)` directly in `RevisionElementSnapshot.Quantities`, while quantity revision rows/summaries use the same helpers. A business-equivalent numeric zero can therefore survive in revision state/output as `-0d`, unlike adjacent quantity/domain canonical boundaries already hardened to canonical positive zero.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionMath.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Intended bounded change

- preserve current non-finite and overflow exception semantics;
- canonicalize accepted zero input/output to `+0d` at the shared RevisionMath boundary;
- add focused public-path regression coverage proving revision capture/report/summary do not preserve a negative-zero sign bit;
- do not alter revision comparison tolerance, percentage denominator threshold, quantity keys, persisted XML token grammar, or business arithmetic.

## Excluded scope

- no edits to `RevisionService.cs`, `QuantityRevisionReport.cs`, `RevisionSnapshotStore.cs`, XML/schema validators, UI, CST, MAP, native BricsCAD, or cross-repo platform work;
- no GitHub Actions, force-push, or unexecuted managed/native PASS claim.

## Coordination

- the older revision snapshot numeric-canonicality claim is `COMPLETED` and reserved only persisted numeric token parsing in `RevisionSnapshotStore.cs`;
- the current Zone/Floor first-create revision claim reserves domain Zone/Floor services and their own smokes, not this source/test pair;
- targeted current-history searches for `RevisionMath signed-zero` and `RevisionRegressionSmoke` found no competing lane immediately before this claim.

## Completion condition

Publish this claim alone first; refresh and recheck overlap; update the shared helper plus focused existing smoke; reconcile current `main`; remote-readback exact source/test; close `COMPLETED` with only validation actually performed recorded.