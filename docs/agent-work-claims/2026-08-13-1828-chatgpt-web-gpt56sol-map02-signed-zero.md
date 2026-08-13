# Work claim — MAP-02 coverage signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map02-signed-zero-20260813-1828`
- Registered UTC: `2026-08-13T11:28:00Z`
- Baseline main SHA: `f3d742f91ec8145936931cede8b8019128391bf8`
- Priority: `MTR-05 / MAP-02 P0-P1 hardening` — public quantity coverage findings must not expose IEEE negative zero

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator.SnapshotQuantities()` rejects non-finite values from the public mutable `ProjectElement.Quantities` dictionary but copies every finite value unchanged into detached `MeasurementWorkItemCoverageFinding.QuantityValue`. Explicit IEEE `-0.0` therefore survives the coverage projection and remains observable by sign bit even though it is semantically zero.

The repository already treats signed-zero representation splits as quantity/unit/report canonicality defects: UnitScale and public Quantity Report output canonicalize exact zero to positive `0d`. MAP coverage is another public detached quantity projection, so it should preserve the same representation invariant without changing quantity math or mapping readiness.

## Reserved files

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`
- this claim file

## Scope

- In the existing quantity snapshot boundary, canonicalize every exact-zero finite quantity to positive `0d` after the current NaN/Infinity guard.
- Add a focused regression that injects explicit negative zero through the public mutable quantity dictionary and requires the public coverage finding value to have positive-zero bits.
- Preserve all mapped/unmapped/stale/missing semantics, ordering, mapping resolution, positive/negative finite quantity behavior and source dictionary contents.
- Do not change `ProjectElement.SetQuantity`, MAP-01 catalog, QSDB/persistence/schema, MeasurementTrace/active none-trace reconciliation, REV-03, reports/UI, rates/cost, geometry or BricsCAD/native surfaces.

## Initial overlap check

- The immediately prior MAP-02 control-character lane is `COMPLETED`; no MAP evaluator/test reservation remains active from it.
- Current `MTR-05 none trace reconciliation` is `ACTIVE` but is a MeasurementTrace contract lane; this claim does not touch MeasurementTrace files or semantics.
- REV-03A remains separately `ACTIVE`; no Revision/Measurement Snapshot file is reserved here.
- Recent V25 status/UI claims are host/test-preflight bounded and do not reserve either MAP file.
- Targeted history search found no MAP coverage signed-zero fix/claim.

## Validation plan

- Re-fetch `main` after claim publication and recheck overlap before source write.
- Keep production change inside `SnapshotQuantities()` and regression inside existing MAP coverage smoke.
- Assert both numeric zero and `BitConverter.DoubleToInt64Bits(...) == 0L` through public `MeasurementWorkItemCoverageFinding.QuantityValue`.
- Re-fetch exact source/test after push and reconcile any intervening commits.
- No GitHub Actions; no `.NET` or native PASS without actual execution.

## Completion condition

Public MAP coverage findings canonicalize negative zero to positive zero with focused committed regression, existing coverage/business semantics remain unchanged, concurrent work is reconciled without overwrite/force-push, remote readback confirms the landed result, and this claim is closed `COMPLETED` with truthful validation status.
